using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BES_Scontrini.Models
{

    public sealed class BesScontrinoRow
    {
        public string? FonteDati { get; init; }
        public string IDREC { get; init; } = "";
        public string? Percorso { get; init; }
        public int ID { get; init; }
        public string NomeFile { get; init; } = "";
        public DateTime DataIns { get; init; }
        public string? Stato { get; init; }
    }

}
