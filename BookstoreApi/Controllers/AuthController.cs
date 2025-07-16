namespace BookstoreApi.Controllers;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, DateTime Expires);

[ApiController]
[Route("api/[controller]")]
public class AuthController(IOptions<JwtSettings> jwtOptions) : ControllerBase
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    private static readonly Dictionary<string, (string Password, string Role)> _users = new()
    {
        ["reader"] = (Password: "reader123", Role: "Read"),
        ["writer"] = (Password: "writer123", Role: "ReadWrite"),
        ["superuser"] = (Password: "super123", Role: "ReadWrite")
    };

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (!_users.TryGetValue(req.Username, out var info)
            || info.Password != req.Password)
        {
            return Unauthorized(new { Message = "Invalid credentials" });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, req.Username),
            new Claim(ClaimTypes.Role, info.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new LoginResponse(tokenString, expires));
    }
}
