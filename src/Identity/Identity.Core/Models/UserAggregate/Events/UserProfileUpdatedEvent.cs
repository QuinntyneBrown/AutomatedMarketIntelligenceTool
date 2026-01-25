using MessagePack;
using Shared.Contracts;
using Shared.Contracts.Versioning;

namespace Identity.Core.Models.UserAggregate.Events;

/// <summary>
/// Event raised when a user's profile is updated.
/// </summary>
[MessagePackObject]
[ContractVersion(1, 0)]
[MessageChannel("identity", "user", "profile-updated", 1)]
public sealed class UserProfileUpdatedEvent : IDomainEvent
{
    [Key(0)]
    public required string UserId { get; init; }

    [Key(1)]
    public required string? BusinessName { get; init; }

    [Key(2)]
    public required long UpdatedAtUnixMs { get; init; }

    [IgnoreMember]
    public string AggregateId => UserId;

    [IgnoreMember]
    public string AggregateType => "User";
}
