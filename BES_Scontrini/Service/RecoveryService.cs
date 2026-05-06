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
        private readonly string _supportFolder;
        private readonly DateTime _minDataIns;
        private readonly FileLogger _logger;

        // costruttore
        public RecoveryService(
            string as400ConnectionString,
            string generaleConnectionString,
            string praticheBesRoot,
            string employeesOldRoot,
            string supportFolder,
            DateTime minDataIns,
            FileLogger logger)
        {
            _as400ConnectionString = as400ConnectionString;
            _generaleConnectionString = generaleConnectionString;
            _praticheBesRoot = praticheBesRoot;
            _employeesOldRoot = employeesOldRoot;
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
            // ============================
            // 1. Estrazione IDREC
            // ============================
            var idRec = ExtractLast6(row.BSRUCRE);

            if (string.IsNullOrWhiteSpace(idRec))
            {
                _logger.Warn(
                    $"BSRUCRE non valido. BSRUCRE={row.BSRUCRE}, PracticeFolder={row.PracticeFolder}");
                return;
            }

            _logger.Info(
                $"Inizio record: BSRUCRE={row.BSRUCRE}, IDREC={idRec}, PracticeFolder={row.PracticeFolder}");

            // ============================
            // 2. Query BES_Scontrini
            // ============================
            var scontrino = await GetLatestBesScontrinoAsync(idRec);

            if (scontrino is null)
            {
                _logger.Warn(
                    $"Nessun record in BES_Scontrini. IDREC={idRec}, BSRUCRE={row.BSRUCRE}, PracticeFolder={row.PracticeFolder}");
                return;
            }

            if (string.IsNullOrWhiteSpace(scontrino.NomeFile))
            {
                _logger.Warn(
                    $"Record BES_Scontrini senza NomeFile. IDREC={idRec}, BSRUCRE={row.BSRUCRE}");
                return;
            }

            // ============================
            // 3. Costruzione percorsi BES
            // ============================
            string besFolder = Path.Combine(_praticheBesRoot, row.PracticeFolder);

            // Nome file reale nel BES (prefisso + nome file DB)
            string besFileName = row.Chiave_BES + scontrino.NomeFile;

            string besTargetFile = Path.Combine(besFolder, besFileName);

            // ============================
            // 4. Check presenza in Pratiche_BES
            // ============================
            if (File.Exists(besTargetFile))
            {
                _logger.Info(
                    $"File già presente in Pratiche_BES. File={besFileName}, Path={besTargetFile}");
                return;
            }

            // ============================
            // 5. Fallback _OLD
            // ============================
            string oldPath = Path.Combine(_employeesOldRoot, scontrino.NomeFile);

            if (File.Exists(oldPath))
            {
                var dest = CopyToSupport(oldPath, row.Chiave_BES, scontrino.NomeFile);
                _logger.Info(
                    $"File copiato da _OLD. Sorgente={oldPath}, Destinazione={dest}");
                return;
            }


            // ============================
            // 7. Non trovato
            // ============================
            _logger.Warn(
                $"File non trovato in BES, _OLD, _NA. " +
                $"NomeFile={scontrino.NomeFile}, IDREC={idRec}, PracticeFolder={row.PracticeFolder}");
        }



        private async Task<List<BsrRow>> GetBsrRowsAsync(string besAnno, string bsrpbes)
        {
            const string sql = """
            
            SELECT
                BSRTBES,
                BSRABES,
                BSRPBES,
                BSRRBES,
                BSRUCRE,

                BSRTBES
                  + Documentazione_Ottica.dbo.fn_riempi(BSRABES, 2, 'S', '0')
                  + Documentazione_Ottica.dbo.fn_riempi(BSRPBES, 6, 'S', '0')
                  AS PracticeFolder,

                '9_' + BSRTBES
                  + Documentazione_Ottica.dbo.fn_riempi(BSRABES, 2, 'S', '0')
                  + Documentazione_Ottica.dbo.fn_riempi(BSRPBES, 6, 'S', '0')
                  + '_SYSTEM_' AS Chiave_BES

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

        private string CopyToSupport(string sourcePath, string besPrefix, string originalFileName)
        {
            Directory.CreateDirectory(_supportFolder);

            string newFileName = besPrefix + originalFileName;
            string destinationPath = Path.Combine(_supportFolder, newFileName);

            if (!File.Exists(destinationPath))
            {
                File.Copy(sourcePath, destinationPath);
            }
            else
            {
                _logger.Warn($"File già presente in supporto: {destinationPath}");
            }

            return destinationPath;
        }

        private static string? ExtractLast6(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed.Length < 6)
                return null;

            // devo recuperare gli ultimi 6 caratteri, non i primi 6
            return trimmed[^6..];
        }
    }
}
