using DevLife.API.Models;

namespace DevLife.API.Services;

public class ZodiacService
{
    public static ZodiacSign CalculateZodiacSign(DateTime birthDate)
    {
        var month = birthDate.Month;
        var day = birthDate.Day;

        return (month, day) switch
        {
            (3, >= 21) or (4, <= 19) => ZodiacSign.Aries,
            (4, >= 20) or (5, <= 20) => ZodiacSign.Taurus,
            (5, >= 21) or (6, <= 20) => ZodiacSign.Gemini,
            (6, >= 21) or (7, <= 22) => ZodiacSign.Cancer,
            (7, >= 23) or (8, <= 22) => ZodiacSign.Leo,
            (8, >= 23) or (9, <= 22) => ZodiacSign.Virgo,
            (9, >= 23) or (10, <= 22) => ZodiacSign.Libra,
            (10, >= 23) or (11, <= 21) => ZodiacSign.Scorpio,
            (11, >= 22) or (12, <= 23) => ZodiacSign.Sagittarius,
            (12, >= 22) or (1, <= 19) => ZodiacSign.Capricorn,
            (1, >= 20) or (2, <= 18) => ZodiacSign.Aquarius,
            _ => ZodiacSign.Pisces
        };
    }

    public static string GetDailyHoroscope(ZodiacSign sign)
    {
        var horoscopes = new Dictionary<ZodiacSign, string[]>
        {
            [ZodiacSign.Aries] = [
                "დღეს შენი კოდი იქნება ისეთივე ცეცხლოვანი, როგორც შენი ხასიათი! 🔥",
                "ავარიული deployment გელოდება, მაგრამ ყველაფერი კარგად დასრულდება 🚀"
            ],
            [ZodiacSign.Taurus] = [
                "შენი კოდი დღეს იქნება მდგრადი, როგორც კლდე. Bug-ები ვერ შეძლებენ მოძვრას! 🗿",
                "ნელა, მაგრამ მტკიცედ - ასე დაწერე დღეს კოდი 🐌"
            ],
            [ZodiacSign.Gemini] = [
                "დღეს ორ ენაზე დაწერ კოდს - JavaScript-ზე და Bug-ების ენაზე 😅",
                "შენი კოდი იქნება ისეთივე მრავალფეროვანი, როგორც შენი აზრები 🎭"
            ],
            [ZodiacSign.Cancer] = [
                "დღეს შენი კოდი იქნება protective, როგორც კიბოს ჯავშანი 🦀",
                "Null reference exceptions-ებისგან დაიცავი შენი ალგორითმები 🛡️"
            ],
            [ZodiacSign.Leo] = [
                "შენი კოდი დღეს იქნება Show Stopper! ყველა რევიუზე ყურადღებას მიიპყრობს 🦁",
                "Senior-ებიც შენზე იტყვიან 'კარგი კოდია!' 👑"
            ],
            [ZodiacSign.Virgo] = [
                "შენი კოდი იქნება სრულყოფილი - არც ერთი semicolon არ დაგავიწყდება 🔍",
                "Code review-ზე კომენტარები არ იქნება, ყველაფერი Perfect! ✨"
            ]
        };

        var messages = horoscopes.GetValueOrDefault(sign, ["დღეს კარგი დღეა კოდინგისთვის! 💻"]);
        return messages[Random.Shared.Next(messages.Length)];
    }

    public static string GetLuckyTechnology()
    {
        var technologies = new[]
        {
            "TypeScript", "React", "Vue.js", "Angular", "Node.js",
            "Python", "C#", ".NET", "Go", "Rust", "Kotlin", "Swift",
            "PostgreSQL", "MongoDB", "Redis", "Docker", "Kubernetes"
        };

        return technologies[Random.Shared.Next(technologies.Length)];
    }
}