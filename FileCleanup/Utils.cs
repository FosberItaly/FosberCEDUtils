using System;
using System.Collections.Generic;
using System.Text;

namespace FileCleanup
{

    public class Utils
    {
        public string? Path { get; set; }
        public int Months { get; set; } = 1;
        public bool Execute { get; set; }
        public bool Recursive { get; set; }

        public static void PrintOptionsHelp()
        {
            Console.WriteLine("Uso:");
            Console.WriteLine(@"  LogCleaner.exe --path ""C:\Log"" --months 1 [--execute] [--recursive]");
            Console.WriteLine();
            Console.WriteLine("Opzioni:");
            Console.WriteLine(@"  --path       Cartella da controllare (default: C:\Log)");
            Console.WriteLine("  --months     Numero di mesi da mantenere (default: 1)");
            Console.WriteLine("  --execute    Cancella davvero i file. Se assente, fa solo simulazione.");
            Console.WriteLine("  --recursive  Include anche le sottocartelle.");
        }

    }

}
