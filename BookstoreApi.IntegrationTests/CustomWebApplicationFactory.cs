namespace BookstoreApi.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        // 1) Otvori i drži SQLite in‑memory vezu
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // 2) Name the environment so Program.cs koristi EnsureCreated()
        builder.UseEnvironment("IntegrationTests");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        // 3) Content root -> bin folder API‑ja
        var apiAsm = typeof(Program).GetTypeInfo().Assembly;
        builder.UseContentRoot(Path.GetDirectoryName(apiAsm.Location)!);

        builder.ConfigureServices(services =>
        {
            //
            // ————— Override DB na SQLite in‑memory —————
            //

            // remove the SQL Server registrations
            services.RemoveAll(typeof(DbContextOptions<BookstoreDigaconDbContext>));
            services.RemoveAll(typeof(BookstoreDigaconDbContext));

            // add our test context
            services.AddDbContext<BookstoreDigaconDbContext>(opts =>
                opts.UseSqlite(_connection));

            // ensure schema
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                scope.ServiceProvider
                     .GetRequiredService<BookstoreDigaconDbContext>()
                     .Database
                     .EnsureCreated();
            }

            //
            // ————— Override Auth na “Test” —————
            //

            services
              .AddAuthentication("Test")
              .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                 "Test", opts => { });

            //
            // ————— Ponovo registriraj točno iste politike —————
            //

            services.AddAuthorization(o =>
            {
                o.AddPolicy("RequireReadRole", p =>
                    p.RequireRole("Read", "ReadWrite"));
                o.AddPolicy("RequireReadWriteRole", p =>
                    p.RequireRole("ReadWrite"));
            });
        });
    }

    /// <summary>
    /// Testi zovu ovo umjesto CreateClient() da bi imali JSON zaglavlja i BaseAddress
    /// </summary>
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
