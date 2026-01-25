namespace Identity.Core.Models.UserAggregate.Entities;

/// <summary>
/// User profile containing business information for the consultant.
/// </summary>
public sealed class UserProfile
{
    public Guid UserProfileId { get; private set; }
    public Guid UserId { get; private set; }
    public string? BusinessName { get; private set; }
    public string? StreetAddress { get; private set; }
    public string? City { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Phone { get; private set; }
    public string? HstNumber { get; private set; }
    public string? LogoUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserProfile() { } // For EF Core

    /// <summary>
    /// Creates a new user profile.
    /// </summary>
    public static UserProfile Create(Guid userId)
    {
        return new UserProfile
        {
            UserProfileId = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates the profile information.
    /// </summary>
    public void Update(
        string? businessName,
        string? streetAddress,
        string? city,
        string? province,
        string? postalCode,
        string? phone,
        string? hstNumber,
        string? logoUrl)
    {
        BusinessName = businessName;
        StreetAddress = streetAddress;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Phone = phone;
        HstNumber = hstNumber;
        LogoUrl = logoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
