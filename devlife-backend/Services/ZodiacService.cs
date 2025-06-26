using DevLife.API.Models;

namespace DevLife.API.Services
{
    public static class ZodiacService
    {
        private static readonly Dictionary<ZodiacSign, List<string>> HoroscopeTexts = new()
        {
            [ZodiacSign.Aries] = new()
            {
                "დღეს შენს კოდს სპეციალური ენერგია ექნება",
                "Bug-ების ნადირობა დღეს წარმატებული იქნება",
                "ახალი ფრეიმვორკის შესწავლის იდეალური დღეა"
            },
            [ZodiacSign.Taurus] = new()
            {
                "სტაბილური კოდი = სტაბილური წარმატება",
                "დღეს refactoring-ზე ფოკუსირდი",
                "თბილი ყავით დაწერე ულამაზესი ფუნქცია"
            },
            [ZodiacSign.Gemini] = new()
            {
                "2 ენის ერთდროულად ცოდნა დღეს გამოგადგება",
                "კომუნიკაცია გუნდთან ყველაზე მნიშვნელოვანია",
                "ღია კოდი = ღია გულით"
            },
            [ZodiacSign.Cancer] = new()
            {
                "სახლიდან remote work დღეს შენი ღია სფეროა",
                "Secure coding პრაქტიკები დაიცავი",
                "შენი კოდი დღეს სხვებს სიბეჯერს მისცემს"
            },
            [ZodiacSign.Leo] = new()
            {
                "შენი კოდი დღეს ყველას აღაქცევს",
                "Leadership skills გამოიყენე PR review-ში",
                "სიმაჩვე = ლეგაცი კოდის მართვა"
            },
            [ZodiacSign.Virgo] = new()
            {
                "კოდის ხარისხზე ყურადღება დღეს განსაკუთრებული იყოს",
                "Test coverage 100%-მდე მივიდეთ!",
                "Detail-oriented approach შენი ძლიერი მხარეა"
            },
            [ZodiacSign.Libra] = new()
            {
                "წონასწორობა work-life balance-სა და კოდინგ სკილებს შორის",
                "UI/UX დღეს შენი გამარჯვების სფეროა",
                "ჰარმონიული API design შენი მიზანია"
            },
            [ZodiacSign.Scorpio] = new()
            {
                "Deep diving in code - შენი სუპერ ძალა",
                "Debugging mysterious issues შენი პროფესიაა",
                "დამალული ბაგები შენ გამოაშკარავებ"
            },
            [ZodiacSign.Sagittarius] = new()
            {
                "ახალი ტექნოლოგიების exploration დღეს შენი adventure-ია",
                "Long-term project vision შენი ღია მხარეა",
                "კოდინგის მოგზაურობაში შენ pioneer ხარ"
            },
            [ZodiacSign.Capricorn] = new()
            {
                "სიმტკიცე და არქიტექტურაზე ფოკუსი შენი ძლიერი მხარეა",
                "Enterprise-level solutions შენი სპეციალობაა",
                "Goals-oriented programming approach შენი მეთოდია"
            },
            [ZodiacSign.Aquarius] = new()
            {
                "ინოვაციური მიდგომები კოდში შენი super power-ია",
                "Open-source contribution დღეს განსაკუთრებით ღირებული",
                "Out-of-the-box thinking შენი კონკურენტული უპირატესობა"
            },
            [ZodiacSign.Pisces] = new()
            {
                "Creative solutions და intuitive debugging შენი ღია სფერო",
                "User empathy-ზე დაფუძნებული development შენი ძლიერი მხარე",
                "Flow state-ში კოდინგი დღეს შენი მიზანია"
            }
        };

        private static readonly List<string> LuckyTechnologies = new()
        {
            "TypeScript", "React", "Vue.js", "Angular", "Node.js", "Python",
            ".NET", "Java", "Go", "Rust", "Docker", "Kubernetes", "AWS",
            "Azure", "PostgreSQL", "MongoDB", "Redis", "GraphQL", "REST API"
        };

        public static string GetDailyHoroscope(ZodiacSign zodiacSign)
        {
            var messages = HoroscopeTexts[zodiacSign];
            var random = new Random();
            return messages[random.Next(messages.Count)];
        }

        public static string GetLuckyTechnology()
        {
            var random = new Random();
            return LuckyTechnologies[random.Next(LuckyTechnologies.Count)];
        }
    }
}