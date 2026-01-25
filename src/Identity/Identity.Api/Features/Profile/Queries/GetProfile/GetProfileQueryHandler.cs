using Identity.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Features.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, GetProfileResponse?>
{
    private readonly IIdentityContext _context;

    public GetProfileQueryHandler(IIdentityContext context)
    {
        _context = context;
    }

    public async Task<GetProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        return new GetProfileResponse(
            UserId: user.UserId,
            Email: user.Email,
            IsEmailVerified: user.IsEmailVerified,
            BusinessName: profile?.BusinessName,
            StreetAddress: profile?.StreetAddress,
            City: profile?.City,
            Province: profile?.Province,
            PostalCode: profile?.PostalCode,
            Phone: profile?.Phone,
            HstNumber: profile?.HstNumber,
            LogoUrl: profile?.LogoUrl,
            CreatedAt: user.CreatedAt,
            UpdatedAt: profile?.UpdatedAt ?? user.CreatedAt);
    }
}
