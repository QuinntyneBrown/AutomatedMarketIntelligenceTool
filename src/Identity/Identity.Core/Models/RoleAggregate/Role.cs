namespace Identity.Core.Models.RoleAggregate;

/// <summary>
/// Represents a role for authorization.
/// </summary>
public sealed class Role
{
    public Guid RoleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<Guid> _userIds = new();
    public IReadOnlyCollection<Guid> UserIds => _userIds.AsReadOnly();

    private Role() { } // For EF Core

    public static Role Create(string name, string? description = null, bool isSystem = false)
    {
        return new Role
        {
            RoleId = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystem = isSystem,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void AddUser(Guid userId)
    {
        if (!_userIds.Contains(userId))
        {
            _userIds.Add(userId);
        }
    }

    public void RemoveUser(Guid userId)
    {
        _userIds.Remove(userId);
    }

    // Predefined system roles
    public static class SystemRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string ReadOnly = "ReadOnly";
    }
}
