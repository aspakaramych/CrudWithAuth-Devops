namespace CrudWithAuth.DTOs;

public class AuthResponse
{
    public string Token { get; set; }
    public UserResponse User { get; set; }
}
