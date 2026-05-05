using BES_Scontrini.Models;
using BES_Scontrini.Utils;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BES_Scontrini.Service
{

    public sealed class RecoveryService
    {
        private readonly string _as400ConnectionString;
        private readonly string _generaleConnectionString;
        private readonly string _praticheBesRoot;
        private readonly string _employeesOldRoot;
        private readonly string _employeesNaRoot;
        private readonly string _supportFolder;
        private readonly DateTime _minDataIns;
        private readonly FileLogger _logger;

        public RecoveryService(
            string as400ConnectionString,
            string generaleConnectionString,
            string praticheBesRoot,
            string employeesOldRoot,
            string employeesNaRoot,
            string supportFolder,
            DateTime minDataIns,
            FileLogger logger)
        {
            _as400ConnectionString = as400ConnectionString;
            _generaleConnectionString = generaleConnectionString;
            _praticheBesRoot = praticheBesRoot;
            _employeesOldRoot = employeesOldRoot;
            _employeesNaRoot = employeesNaRoot;
            _supportFolder = supportFolder;
            _minDataIns = minDataIns;
            _logger = logger;
        }

        public async Task RunAsync(string besAnno, string bsrpbes)
        {
            _logger.Info($"Avvio elaborazione per BESANNO={besAnno}, BSRPBES={bsrpbes}");

            var bsrRows = await GetBsrRowsAsync(besAnno, bsrpbes);

            if (bsrRows.Count == 0)
            {
                _logger.Warn("Nessun record trovato in BSR00LF.");
                return;
            }

            _logger.Info($"Trovati {bsrRows.Count} record in BSR00LF.");

            foreach (var row in bsrRows)
            {
                try
                {
                    await ProcessRowAsync(row);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        $"Errore su BSRUCRE={row.BSRUCRE}, BSRTBES={row.BSRTBES}, BSRABES={row.BSRABES}, BSRPBES={row.BSRPBES}. Dettaglio: {ex.Message}");
                }
            }

            _logger.Info("Elaborazione terminata.");
        }

        private async Task ProcessRowAsync(BsrRow row)
        {
            var idRec = ExtractLast6(row.BSRUCRE);
            var practiceCode = BuildPracticeCode(row.BSRTBES, row.BSRABES, row.BSRPBES);

            if (string.IsNullOrWhiteSpace(idRec))
            {
                _logger.Warn($"BSRUCRE non valido. BSRUCRE={row.BSRUCRE}, practiceCode={practiceCode}");
                return;
            }

            _logger.Info($"Inizio record: BSRUCRE={row.BSRUCRE}, IDREC={idRec}, practiceCode={practiceCode}");

            var scontrino = await GetLatestBesScontrinoAsync(idRec);

            if (scontrino is null)
            {
                _logger.Warn(
                    $"Nessun record in BES_Scontrini per IDREC={idRec}, BSRUCRE={row.BSRUCRE}, practiceCode={practiceCode}");
                return;
            }

            if (string.IsNullOrWhiteSpace(scontrino.NomeFile))
            {
                _logger.Warn(
                    $"Record BES_Scontrini senza NomeFile. IDREC={idRec}, BSRUCRE={row.BSRUCRE}, practiceCode={practiceCode}");
                return;
            }

            var besFolder = Path.Combine(_praticheBesRoot, practiceCode);
            var besTargetFile = Path.Combine(besFolder, scontrino.NomeFile);

            if (File.Exists(besTargetFile))
            {
                _logger.Info(
                    $"File già presente nella cartella BES. File={scontrino.NomeFile}, path={besTargetFile}");
                return;
            }

            var oldPath = Path.Combine(_employeesOldRoot, scontrino.NomeFile);
            if (File.Exists(oldPath))
            {
                var dest = CopyToSupport(oldPath, practiceCode, scontrino.NomeFile);
                _logger.Info(
                    $"File copiato da _OLD. NomeFile={scontrino.NomeFile}, sorgente={oldPath}, destinazione={dest}");
                return;
            }

            var naPath = Path.Combine(_employeesNaRoot, scontrino.NomeFile);
            if (File.Exists(naPath))
            {
                var dest = CopyToSupport(naPath, practiceCode, scontrino.NomeFile);
                _logger.Info(
                    $"File copiato da _NA. NomeFile={scontrino.NomeFile}, sorgente={naPath}, destinazione={dest}");
                return;
            }

            _logger.Warn(
                $"File non trovato né in BES né in _OLD né in _NA. NomeFile={scontrino.NomeFile}, IDREC={idRec}, BSRUCRE={row.BSRUCRE}, practiceCode={practiceCode}");
        }

        private async Task<List<BsrRow>> GetBsrRowsAsync(string besAnno, string bsrpbes)
        {
            const string sql = """
            SELECT BSRTBES, BSRABES, BSRPBES, BSRRBES, BSRUCRE
            FROM Tabelle_AS400.dbo.BSR00LF
            WHERE BSRPBES = @Bsrpbes
              AND BSRABES = @BesAnno
            """;

            await using var connection = new SqlConnection(_as400ConnectionString);

            var result = await connection.QueryAsync<BsrRow>(sql, new
            {
                Bsrpbes = bsrpbes,
                BesAnno = besAnno
            });

            return result.ToList();
        }

        private async Task<BesScontrinoRow?> GetLatestBesScontrinoAsync(string idRec)
        {
            const string sql = """
            SELECT TOP (1)
                   FonteDati,
                   IDREC,
                   Percorso,
                   ID,
                   NomeFile,
                   DataIns,
                   Stato
            FROM Generale.dbo.BES_Scontrini
            WHERE IDREC = @IdRec
              AND DataIns > @MinDataIns
            ORDER BY DataIns DESC
            """;

            await using var connection = new SqlConnection(_generaleConnectionString);

            return await connection.QueryFirstOrDefaultAsync<BesScontrinoRow>(sql, new
            {
                IdRec = idRec,
                MinDataIns = _minDataIns
            });
        }

        private string CopyToSupport(string sourcePath, string practiceCode, string originalFileName)
        {
            Directory.CreateDirectory(_supportFolder);

            var newName = $"9_{practiceCode}_SYSTEM_{originalFileName}";
            var destinationPath = Path.Combine(_supportFolder, newName);

            File.Copy(sourcePath, destinationPath, overwrite: true);

            return destinationPath;
        }

        private static string BuildPracticeCode(string bsrttbes, string bsrabes, string bsrpbes)
        {
            var anno = (bsrabes ?? "").Trim().PadLeft(2, '0');
            var progressivo = (bsrpbes ?? "").Trim().PadLeft(6, '0');
            return $"{(bsrttbes ?? "").Trim()}{anno}{progressivo}";
        }

        private static string? ExtractLast6(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Length < 6)
                return null;

            return trimmed[^6..];
        }
    }
}
