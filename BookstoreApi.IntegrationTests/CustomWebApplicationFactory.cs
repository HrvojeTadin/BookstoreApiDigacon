namespace BookstoreApi.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        var apiAsm = typeof(Program).GetTypeInfo().Assembly;
        builder.UseContentRoot(Path.GetDirectoryName(apiAsm.Location)!);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<BookstoreDigaconDbContext>));
            services.RemoveAll(typeof(BookstoreDigaconDbContext));

            services.AddDbContext<BookstoreDigaconDbContext>(opts =>
                opts.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                scope.ServiceProvider
                     .GetRequiredService<BookstoreDigaconDbContext>()
                     .Database
                     .EnsureCreated();
            }

            services
              .AddAuthentication("Test")
              .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                 "Test", opts => { });

            services.AddAuthorization(o =>
            {
                o.AddPolicy("RequireReadRole", p =>
                    p.RequireRole("Read", "ReadWrite"));
                o.AddPolicy("RequireReadWriteRole", p =>
                    p.RequireRole("ReadWrite"));
            });
        });
    }
    public HttpClient CreateClientWithDefaults()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders
              .Accept
              .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public new void Dispose()
    {
        base.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
