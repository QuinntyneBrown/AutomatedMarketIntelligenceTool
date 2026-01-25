using MessagePack;
using Shared.Contracts;
using Shared.Contracts.Versioning;

namespace Identity.Core.Models.UserAggregate.Events;

/// <summary>
/// Event raised when a user verifies their email address.
/// </summary>
[MessagePackObject]
[ContractVersion(1, 0)]
[MessageChannel("identity", "user", "email-verified", 1)]
public sealed class UserEmailVerifiedEvent : IDomainEvent
{
    [Key(0)]
    public required string UserId { get; init; }

    [Key(1)]
    public required string Email { get; init; }

    [Key(2)]
    public required long VerifiedAtUnixMs { get; init; }

    [IgnoreMember]
    public string AggregateId => UserId;

    [IgnoreMember]
    public string AggregateType => "User";
}
