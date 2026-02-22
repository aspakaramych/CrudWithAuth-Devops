namespace CrudWithAuth.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;

    /// <summary>
    /// Backwards-compatible alias for AccessToken (используется в тестах)
    /// </summary>
    public string Token => AccessToken;
}
