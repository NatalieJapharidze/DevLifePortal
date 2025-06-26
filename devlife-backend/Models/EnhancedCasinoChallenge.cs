namespace DevLife.API.Models
{
    public class EnhancedCasinoChallenge : CasinoChallenge
    {
        public ChallengeType Type { get; set; }
        public int BasePoints { get; set; } = 10;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string Hint { get; set; } = string.Empty;
    }
    public enum ChallengeType
    {
        Syntax,
        Logic,
        Performance,
        Security,
        BestPractice
    }
}
