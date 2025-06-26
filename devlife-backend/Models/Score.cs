namespace DevLife.API.Models
{
    public class Score
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GameType { get; set; } = string.Empty;
        public int Points { get; set; }
        public DateTime CreatedAt { get; set; }
        public User? User { get; set; }
    }
}
