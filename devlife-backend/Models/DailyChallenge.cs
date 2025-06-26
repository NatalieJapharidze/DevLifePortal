namespace DevLife.API.Models
{
    public class DailyChallenge
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int ChallengeId { get; set; }
        public int BonusMultiplier { get; set; } = 3;
        public bool IsActive { get; set; } = true;
        public CasinoChallenge? Challenge { get; set; }
    }
}
