using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BES_Scontrini.Utils
{

    public sealed class FileLogger : IDisposable
    {
        private readonly StreamWriter _writer;

        public string LogPath { get; }

        public FileLogger(string logFolder, string besAnno, string bsrpbes)
        {
            Directory.CreateDirectory(logFolder);

            var fileName = $"{besAnno}_{bsrpbes}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            LogPath = Path.Combine(logFolder, fileName);

            _writer = new StreamWriter(LogPath, append: false, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public void Info(string message) =>
            Write("INFO", message);

        public void Warn(string message) =>
            Write("WARN", message);

        public void Error(string message) =>
            Write("ERROR", message);

        private void Write(string level, string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            Console.WriteLine(line);
            _writer.WriteLine(line);
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }

}
