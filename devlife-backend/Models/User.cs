namespace DevLife.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string TechStack { get; set; } = string.Empty;
        public ExperienceLevel ExperienceLevel { get; set; }
        public ZodiacSign ZodiacSign { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ExperienceLevel
    {
        Junior,
        Middle,
        Senior
    }

    public enum ZodiacSign
    {
        Aries, Taurus, Gemini, Cancer, Leo, Virgo,
        Libra, Scorpio, Sagittarius, Capricorn, Aquarius, Pisces
    }
}
