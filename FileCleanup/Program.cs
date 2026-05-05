using FileCleanup;

internal class Program
{
    static int Main(string[] args)
    {
        try
        {
            var options = ParseArgs(args);

            string folderPath = options.Path ?? @"C:\Logs";
            int months = options.Months > 0 ? options.Months : 1;
            bool execute = options.Execute;
            bool recursive = options.Recursive;

            if (!Directory.Exists(folderPath))
            {
                Console.Error.WriteLine($"[ERRORE] La cartella non esiste: {folderPath}");
                return 2;
            }

            DateTime cutoffDate = DateTime.Now.AddMonths(-months);
            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Console.WriteLine("==============================================");
            Console.WriteLine(" LogCleaner");
            Console.WriteLine("==============================================");
            Console.WriteLine($"Cartella      : {folderPath}");
            Console.WriteLine($"Soglia        : file con LastWriteTime < {cutoffDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Modalità      : {(execute ? "ESECUZIONE (cancella i file)" : "DRY-RUN (simulazione)")}");
            Console.WriteLine($"Ricorsivo     : {(recursive ? "Sì" : "No")}");
            Console.WriteLine($"Avvio         : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            int scanned = 0;
            int matched = 0;
            int deleted = 0;
            int errors = 0;

            foreach (string file in Directory.EnumerateFiles(folderPath, "*", searchOption))
            {
                scanned++;

                try
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(file);

                    if (lastWriteTime < cutoffDate)
                    {
                        matched++;
                        Console.WriteLine($"[CANDIDATO] {file} | LastWriteTime={lastWriteTime:yyyy-MM-dd HH:mm:ss}");

                        if (execute)
                        {
                            File.Delete(file);
                            deleted++;
                            Console.WriteLine($"[ELIMINATO] {file}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    Console.Error.WriteLine($"[ERRORE FILE] {file} | {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("==============================================");
            Console.WriteLine(" RIEPILOGO");
            Console.WriteLine("==============================================");
            Console.WriteLine($"File analizzati : {scanned}");
            Console.WriteLine($"File trovati    : {matched}");
            Console.WriteLine($"File eliminati  : {deleted}");
            Console.WriteLine($"Errori          : {errors}");
            Console.WriteLine($"Fine            : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return errors > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERRORE FATALE] {ex}");
            return 99;
        }
    }

    private static Utils ParseArgs(string[] args)
    {
        var options = new Utils();

        // vado ad eseguire il parsing per le varie operazioni
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim();

            switch (arg.ToLowerInvariant())
            {
                // nel caso in cui volessi specificare un altro path (diverso da C:\Logs)
                case "--path":
                    if (i + 1 < args.Length)
                    {
                        options.Path = args[++i];
                    }
                    break;

                case "--months":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int months))
                    {
                        options.Months = months;
                    }
                    break;

                // per eseguirlo effettivamente
                case "--execute":
                    options.Execute = true;
                    break;

                case "--recursive":
                    options.Recursive = true;
                    break;

                // help aiuti da terminale
                case "--help":
                case "-h":
                    Utils.PrintOptionsHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }
}