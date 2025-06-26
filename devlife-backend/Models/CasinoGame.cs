namespace DevLife.API.Models
{
    public class CasinoGame
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ChallengeId { get; set; }
        public int UserAnswer { get; set; }
        public int BetPoints { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsWon { get; set; }
        public DateTime PlayedAt { get; set; }

        public User? User { get; set; }
        public CasinoChallenge? Challenge { get; set; }
    }
}
