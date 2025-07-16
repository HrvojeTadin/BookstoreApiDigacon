namespace BookstoreApi.UnitTests;

public class AuthControllerTests
{
    private static AuthController CreateController()
    {
        var jwt = new JwtSettings
        {
            Key = "DugaLozinkaKakoBiSeOsjecaliStoJeMoguceViseSigurnije!",
            Issuer = "BookstoreApi",
            Audience = "BookstoreClient",
            DurationInMinutes = 60
        };

        var options = Options.Create(jwt);
        return new AuthController(options);
    }

    [Fact]
    public void Login_WithValidCredentials_ReturnsToken()
    {
        var ctrl = CreateController();
        var req = new LoginRequest("reader", "reader123");

        var result = ctrl.Login(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("Token", json);
        Assert.Contains("Expires", json);
    }

    [Theory]
    [InlineData("reader", "wrong")]
    [InlineData("unknown", "reader123")]
    public void Login_WithInvalidCredentials_ReturnsUnauthorized(string user, string pwd)
    {
        var ctrl = CreateController();
        var req = new LoginRequest(user, pwd);

        var result = ctrl.Login(req);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
