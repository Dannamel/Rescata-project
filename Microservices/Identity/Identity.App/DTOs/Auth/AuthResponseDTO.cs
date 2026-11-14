namespace Identity.App.DTOs.Auth;

public class AuthResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
