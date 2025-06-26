namespace DevLife.API.Models
{
    public class CasinoChallenge
    {
        public int Id { get; set; }
        public string TechStack { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CodeSnippet1 { get; set; } = string.Empty;
        public string CodeSnippet2 { get; set; } = string.Empty;
        public int CorrectAnswer { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public ExperienceLevel Difficulty { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
