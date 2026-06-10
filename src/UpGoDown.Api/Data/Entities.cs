namespace UpGoDown.Api.Data;

public sealed class AppUser
{
    public Guid Id { get; set; }
    public string Login { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = UserRoles.Student;
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<LevelAttempt> Attempts { get; set; } = new List<LevelAttempt>();
}

public static class UserRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
}

public sealed class LevelAttempt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int LevelId { get; set; }
    public bool Success { get; set; }
    public int StepsCount { get; set; }
    public string PointsHistoryJson { get; set; } = "[]";
    public string SceneJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
