using System.Text.Json;

try
{
    string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    if (!File.Exists(configPath))
        throw new FileNotFoundException("File appsettings.json non trovato.", configPath);

    string json = File.ReadAllText(configPath);

    // parso il json in un oggetto AppSettings
    AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    if (settings == null || string.IsNullOrWhiteSpace(settings.FileToClean))
        throw new Exception("Il percorso FileToClean non è configurato correttamente.");

    string filePath = settings.FileToClean;

    if (!File.Exists(filePath))
        throw new FileNotFoundException("Il file da pulire non esiste.", filePath);

    // Svuota completamente il contenuto senza cancellare il file
    File.WriteAllText(filePath, string.Empty);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);

    return 1;
}

public class AppSettings
{
    public string FileToClean { get; set; } = string.Empty;
}