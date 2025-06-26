namespace DevLife.API.Models
{
    public class UserStats
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TotalGamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int TotalPointsEarned { get; set; }
        public int TotalPointsLost { get; set; }
        public DateTime LastPlayedAt { get; set; }
        public bool PlayedToday { get; set; }
        public User? User { get; set; }
    }
}
