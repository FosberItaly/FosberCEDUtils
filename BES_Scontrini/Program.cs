using BES_Scontrini.Utils;
using BES_Scontrini.Service;

Console.Write("Inserisci besAnno: ");
var besAnno = Console.ReadLine()?.Trim();

Console.Write("Inserisci bsrpbes: ");
var bsrpbes = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(besAnno) || string.IsNullOrWhiteSpace(bsrpbes))
{
    Console.WriteLine("Input non validi.");
    return;
}

// TODO: metti qui le tue connection string reali
var as400ConnectionString = "Server=...;Database=Tabelle_AS400;Trusted_Connection=True;TrustServerCertificate=True;";
var generaleConnectionString = "Server=...;Database=Generale;Trusted_Connection=True;TrustServerCertificate=True;";

// TODO: metti qui i path reali
var praticheBesRoot = @"\\server1\Pratiche_Bes";
var employeesOldRoot = @"\\server1\employees\_OLD";
var employeesNaRoot = @"\\server1\employees\_NA";
var supportFolder = @"\\server1\Supporto_Bes";
var logFolder = @"C:\Logs\BesScontrini";

// TODO: se vuoi renderla parametrica, la leggi da config o da input
var minDataIns = new DateTime(2025, 1, 1);

using var logger = new FileLogger(logFolder, besAnno, bsrpbes);

try
{
    var service = new RecoveryService(
        as400ConnectionString,
        generaleConnectionString,
        praticheBesRoot,
        employeesOldRoot,
        employeesNaRoot,
        supportFolder,
        minDataIns,
        logger);

    await service.RunAsync(besAnno, bsrpbes);

    logger.Info($"Log scritto in: {logger.LogPath}");
}
catch (Exception ex)
{
    logger.Error($"Errore fatale: {ex}");
}