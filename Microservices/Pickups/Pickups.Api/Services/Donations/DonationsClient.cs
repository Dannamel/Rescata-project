using Pickups.Api.Core;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

    public Task<Result> ClaimAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(donationId, "claim", cancellationToken);

    public Task<Result> ReleaseAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(donationId, "release", cancellationToken);

    public Task<Result> MarkPickedUpAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(donationId, "picked-up", cancellationToken);

    /// <summary>
    /// Llama a PATCH api/donations/{id}/{action} reenviando el JWT del usuario.
    /// Las respuestas de Donations se traducen a errores de Pickups con el mismo significado:
    /// 400 (regla de negocio) sigue siendo 400 y 404 sigue siendo 404.
    /// </summary>
    private async Task<Result> ChangeStatusAsync(Guid donationId, string action, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Patch, $"api/donations/{donationId}/{action}");

        ForwardAccessToken(request);

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure(
                    Error.NotFound("Donations.NotFound", "La donación indicada no existe."));
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Donations rechazó el cambio por una regla de negocio (por ejemplo, la donación venció)
                string message = await ReadErrorMessageAsync(response, cancellationToken)
                                 ?? "La donación no permite este cambio de estado.";

                return Result.Failure(Error.Validation("Donations.BusinessRule", message));
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Result.Failure(
                    Error.Forbidden("Donations.Forbidden", "El usuario no tiene permiso para cambiar el estado de la donación."));
            }

            _logger.LogError("Donations respondió {StatusCode} al hacer {Action} sobre la donación {DonationId}.",
                response.StatusCode, action, donationId);

            return Result.Failure(
                Error.Failure("Donations.Unavailable", "El servicio de donaciones no respondió correctamente."));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "No fue posible contactar a Donations para hacer {Action} sobre la donación {DonationId}.",
                action, donationId);

            return Result.Failure(
                Error.Failure("Donations.Unavailable", "El servicio de donaciones no está disponible."));
        }
    }

    /// <summary>Lee el campo "message" de la respuesta de error de Donations ({ status, error, message, errors }).</summary>
    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            DonationsErrorResponse? body =
                await response.Content.ReadFromJsonAsync<DonationsErrorResponse>(cancellationToken);

            string? message = body?.Message;

            return string.IsNullOrWhiteSpace(message) ? null : message;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed record DonationsErrorResponse(string? Message);

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
