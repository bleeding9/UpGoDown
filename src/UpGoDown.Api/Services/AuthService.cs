using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UpGoDown.Api.Data;

namespace UpGoDown.Api.Services;

public sealed class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<(bool Ok, string? Error, AuthResponse? Data)> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Login и password обязательны", null);

        var login = request.Login.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Login == login))
            return (false, "Пользователь уже существует", null);

        var role = string.IsNullOrWhiteSpace(request.Role) ? UserRoles.Student : request.Role.Trim();
        if (role is not (UserRoles.Student or UserRoles.Teacher))
            return (false, "Role: Student или Teacher", null);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Login = login,
            Name = string.IsNullOrWhiteSpace(request.Name) ? login : request.Name.Trim(),
            Role = role,
            PasswordHash = HashPassword(request.Password),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (true, null, BuildAuthResponse(user));
    }

    public async Task<(bool Ok, string? Error, AuthResponse? Data)> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Login и password обязательны", null);

        var login = request.Login.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user is null || user.PasswordHash != HashPassword(request.Password))
            return (false, "Неверный login или password", null);

        return (true, null, BuildAuthResponse(user));
    }

    public Guid? GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    public string? GetUserRole(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role);

    private AuthResponse BuildAuthResponse(AppUser user) => new()
    {
        Token = CreateToken(user),
        Name = user.Name,
        Login = user.Login,
        Role = user.Role,
    };

    private string CreateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("display_name", user.Name),
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

public sealed class RegisterRequest
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = UserRoles.Student;
}

public sealed class LoginRequest
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class AuthResponse
{
    public string Token { get; set; } = "";
    public string Name { get; set; } = "";
    public string Login { get; set; } = "";
    public string Role { get; set; } = "";
}
