using Dapper;
using Microsoft.Extensions.Configuration;
using BES_Scontrini.Service;
using BES_Scontrini.Utils;

Console.WriteLine("=== BES Scontrini Recovery ===");

// --------------------------------------------------
// RECUPERO CONFIGURAZIONE
// --------------------------------------------------

var configuration = new ConfigurationBuilder() // creazione di un contenitore per la configurazione, che legge da appsettings.json
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build(); // con Build otteniamo l'oggetto IConfiguration che ci permette di leggere i valori

string as400ConnectionString =
    configuration.GetConnectionString("As400")
    ?? throw new Exception("ConnectionStrings:As400 mancante");

string generaleConnectionString =
    configuration.GetConnectionString("Generale")
    ?? throw new Exception("ConnectionStrings:Generale mancante");

string praticheBesRoot =
    configuration["Paths:PraticheBesRoot"]
    ?? throw new Exception("Paths:PraticheBesRoot mancante");

string employeesOldRoot =
    configuration["Paths:EmployeesOldRoot"]
    ?? throw new Exception("Paths:EmployeesOldRoot mancante");

string supportFolder =
    configuration["Paths:SupportFolder"]
    ?? throw new Exception("Paths:SupportFolder mancante");

string logFolder =
    configuration["Paths:LogFolder"]
    ?? throw new Exception("Paths:LogFolder mancante");

DateTime minDataIns =
    DateTime.Parse(configuration["Settings:MinDataIns"]
                   ?? throw new Exception("Settings:MinDataIns mancante"));

// --------------------------------------------------
// INPUT UTENTE
// --------------------------------------------------

Console.Write("Inserisci besAnno: ");
string? besAnno = Console.ReadLine()?.Trim();

Console.Write("Inserisci bsrpbes: ");
string? bsrpbes = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(besAnno) || string.IsNullOrWhiteSpace(bsrpbes))
{
    Console.WriteLine("Input non validi. Uscita.");
    return;
}

// --------------------------------------------------
// LOGGER
// --------------------------------------------------

using var logger = new FileLogger(logFolder, besAnno, bsrpbes);

// --------------------------------------------------
// RICHIAMO IL SERVIZIO
// --------------------------------------------------

try
{
    var service = new RecoveryService(
        as400ConnectionString,
        generaleConnectionString,
        praticheBesRoot,
        employeesOldRoot,
        supportFolder,
        minDataIns,
        logger
    );

    await service.RunAsync(besAnno, bsrpbes);

    logger.Info($"Elaborazione completata. Log: {logger.LogPath}");
}
catch (Exception ex)
{
    logger.Error($"Errore fatale: {ex}");
}

Console.WriteLine("Premi un tasto per uscire...");
Console.ReadKey();