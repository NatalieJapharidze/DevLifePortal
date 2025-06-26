using Microsoft.EntityFrameworkCore;
using DevLife.API.Data;
using DevLife.API.Models;

namespace DevLife.API.Services;

public class AuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> RegisterAsync(string username, string firstName, string lastName,
        DateTime birthDate, string techStack, ExperienceLevel experienceLevel)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return null;
        }

        var user = new User
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            BirthDate = birthDate,
            TechStack = techStack,
            ExperienceLevel = experienceLevel,
            ZodiacSign = ZodiacService.CalculateZodiacSign(birthDate)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<string?> LoginAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        var sessionToken = Guid.NewGuid().ToString();
        var session = new Session
        {
            UserId = user.Id,
            SessionToken = sessionToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return sessionToken;
    }

    public async Task<User?> GetUserByTokenAsync(string token)
    {
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == token && s.ExpiresAt > DateTime.UtcNow);

        return session?.User;
    }
}