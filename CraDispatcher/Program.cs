using System.Text.Json;

internal sealed class Settings
{
    public string DispatchUrl { get; set; } = "";
    public string HeaderName { get; set; } = "X-Cra-Key";
    public string Secret { get; set; } = "";
    public int Steps { get; set; } = 2;
    public int DelaySeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 120;
    public string LogFile { get; set; } = "";
}

internal static class Program
{
    private static async Task<int> Main()
    {
        try
        {
            Settings settings = LoadSettings();

            Directory.CreateDirectory(
                Path.GetDirectoryName(settings.LogFile)!
            );

            using var handler = new HttpClientHandler
            {
                // Come nel VBS, vogliamo vedere eventuali redirect.
                AllowAutoRedirect = false
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
            };

            bool hasErrors = false;

            for (int i = 1; i <= settings.Steps; i++)
            {
                bool success = await CallDispatcher(client, settings, i);

                if (!success)
                {
                    hasErrors = true;
                }

                if (i < settings.Steps)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(settings.DelaySeconds)
                    );
                }
            }

            return hasErrors ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static Settings LoadSettings()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "appsettings.json"
        );

        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<Settings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? throw new Exception(
            "Impossibile leggere appsettings.json."
        );
    }

    private static async Task<bool> CallDispatcher(
        HttpClient client,
        Settings settings,
        int step)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                settings.DispatchUrl
            );

            request.Headers.Add(
                settings.HeaderName,
                settings.Secret
            );

            using HttpResponseMessage response =
                await client.SendAsync(request);

            string body = await response.Content.ReadAsStringAsync();

            string result;

            if (response.IsSuccessStatusCode)
            {
                result =
                    $"PASSO {step}/{settings.Steps} - {body.Trim()}";
            }
            else if ((int)response.StatusCode is 301 or 302 or 307 or 308)
            {
                result =
                    $"PASSO {step}/{settings.Steps} - " +
                    $"ERRORE HTTP {(int)response.StatusCode}: " +
                    "la richiesta viene rediretta";
            }
            else
            {
                result =
                    $"PASSO {step}/{settings.Steps} - " +
                    $"ERRORE HTTP {(int)response.StatusCode}: " +
                    body.Trim();
            }

            WriteLog(settings.LogFile, result);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            WriteLog(
                settings.LogFile,
                $"PASSO {step}/{settings.Steps} - " +
                $"ERRORE TRASPORTO: {ex.Message}"
            );

            return false;
        }
    }

    private static void WriteLog(string path, string text)
    {
        string line =
            $"{DateTime.Now:dd/MM/yyyy HH:mm:ss}\t{text}" +
            Environment.NewLine;

        File.AppendAllText(path, line);
        Console.Write(line);
    }
}