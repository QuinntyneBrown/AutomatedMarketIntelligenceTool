using MessagePack;
using Shared.Contracts;
using Shared.Contracts.Versioning;

namespace Identity.Core.Models.UserAggregate.Events;

/// <summary>
/// Event raised when a new user registers.
/// </summary>
[MessagePackObject]
[ContractVersion(1, 0)]
[MessageChannel("identity", "user", "registered", 1)]
public sealed class UserRegisteredEvent : IDomainEvent
{
    [Key(0)]
    public required string UserId { get; init; }

    [Key(1)]
    public required string Email { get; init; }

    [Key(2)]
    public required long RegisteredAtUnixMs { get; init; }

    [IgnoreMember]
    public string AggregateId => UserId;

    [IgnoreMember]
    public string AggregateType => "User";
}
