namespace BookstoreApi.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = default!;
    public DateTime Expires { get; set; }
}
