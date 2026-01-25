using MessagePack;
using Shared.Contracts;
using Shared.Contracts.Versioning;

namespace Identity.Core.Models.UserAggregate.Events;

/// <summary>
/// Event raised when a user logs in (optional, for analytics).
/// </summary>
[MessagePackObject]
[ContractVersion(1, 0)]
[MessageChannel("identity", "user", "logged-in", 1)]
public sealed class UserLoggedInEvent : IDomainEvent
{
    [Key(0)]
    public required string UserId { get; init; }

    [Key(1)]
    public required long LoggedInAtUnixMs { get; init; }

    [IgnoreMember]
    public string AggregateId => UserId;

    [IgnoreMember]
    public string AggregateType => "User";
}
