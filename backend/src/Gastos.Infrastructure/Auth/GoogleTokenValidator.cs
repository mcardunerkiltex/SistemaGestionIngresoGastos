using Gastos.Application.Abstractions;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gastos.Infrastructure.Auth;

/// <summary>Valida el ID token que entrega Google Identity Services en el navegador.</summary>
internal sealed class GoogleTokenValidator(IOptions<GoogleAuthOptions> options, ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    private readonly string _clientId = options.Value.ClientId;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId);

    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId] });
            return new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.Name ?? "");
        }
        catch (InvalidJwtException ex)
        {
            logger.LogInformation(ex, "ID token de Google rechazado");
            return null;
        }
    }
}
