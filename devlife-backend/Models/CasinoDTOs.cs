using DevLife.API.Models;


namespace DevLife.API.Models.DTOs
{
    public record PlayCasinoRequest(
    int ChallengeId,
    int UserAnswer,
    int BetPoints,
    ChallengeData? ChallengeData = null);

    public record ChallengeData(
           string Title,
           string Description,
           string CodeSnippet1,
           string CodeSnippet2,
           int CorrectAnswer,
           string Explanation,
           string TechStack,
           ExperienceLevel Difficulty
       );
}
