namespace BES_Scontrini.Models
{

    public sealed class BsrRow
    {
        public string BSRTBES { get; init; } = "";
        public decimal BSRABES { get; init; } = 0;
        public decimal BSRPBES { get; init; } = 0;
        public decimal BSRRBES { get; init; } = 0;
        public string BSRUCRE { get; init; } = "";
        public string Chiave_BES { get; init; } = "";

        public string PracticeFolder { get; init; } = "";
    }

}
