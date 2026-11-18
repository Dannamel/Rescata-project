using Pickups.Api.Core;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Pickups.Api.Services.Donations;

public sealed class DonationsClient : IDonationsClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DonationsClient> _logger;

    public DonationsClient(HttpClient httpClient,
                           IHttpContextAccessor httpContextAccessor,
                           ILogger<DonationsClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<DonationSummaryDTO>> GetDonationAsync(Guid donationId,
                                                                   CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"api/donations/{donationId}");

        ForwardAccessToken(request);

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<DonationSummaryDTO>(
                    Error.NotFound("Donations.NotFound", "La donación indicada no existe."));
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Donations rechazó el token al consultar la donación {DonationId}: {StatusCode}.",
                    donationId, response.StatusCode);

                return Result.Failure<DonationSummaryDTO>(
                    Error.Failure("Donations.Unauthorized", "No fue posible autenticarse contra el servicio de donaciones."));
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Donations respondió {StatusCode} al consultar la donación {DonationId}.",
                    response.StatusCode, donationId);

                return Result.Failure<DonationSummaryDTO>(
                    Error.Failure("Donations.Unavailable", "El servicio de donaciones no respondió correctamente."));
            }

            DonationSummaryDTO? donation =
                await response.Content.ReadFromJsonAsync<DonationSummaryDTO>(cancellationToken);

            return donation is null
                ? Result.Failure<DonationSummaryDTO>(
                    Error.Failure("Donations.InvalidResponse", "El servicio de donaciones devolvió una respuesta vacía."))
                : Result.Success(donation);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Donations apagado, sin red o fuera de tiempo: no es culpa de quien llamó a Pickups.
            _logger.LogError(exception, "No fue posible contactar a Donations para la donación {DonationId}.", donationId);

            return Result.Failure<DonationSummaryDTO>(
                Error.Failure("Donations.Unavailable", "El servicio de donaciones no está disponible."));
        }
    }

       private void ForwardAccessToken(HttpRequestMessage request)
    {
        string? authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authorizationHeader)
            && AuthenticationHeaderValue.TryParse(authorizationHeader, out AuthenticationHeaderValue? header))
        {
            request.Headers.Authorization = header;
        }
    }
}
