var builder = WebApplication.CreateBuilder(args);

// ─── Serilog setup (isto kao i prije) ─────────────────────────────────────────
var apiRoot = builder.Environment.ContentRootPath;
var logsRoot = Path.Combine(apiRoot, "..", "Logs");
Directory.CreateDirectory(logsRoot);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: Path.Combine(logsRoot, "apilogs-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
builder.Host.UseSerilog();
Log.Information("=== Serilog initialized. Logs folder: {LogsPath} ===", logsRoot);

// ─── DbContext registration ────────────────────────────────────────────────────
// Registriraj SQL Server *samo* u realnom (dev/prod), a *preskoči* u IntegrationTests
if (!builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.AddSqlServerDb();         // tvoj extension za UseSqlServer(...)
    builder.AddJwtAuthentication();   // JWT auth + policies
    builder.AddSwaggerWithJwt();      // Swagger + bearer support
}

// ─── Ostali servisi (uvijek) ────────────────────────────────────────────────────
builder.AddBookstoreServices();  // IBookstoreService, IThirdPartyBookClient
builder.AddQuartzService();      // Quartz job + trigger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ─── Apply migrations ili EnsureCreated ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookstoreDigaconDbContext>();
    if (app.Environment.IsEnvironment("IntegrationTests"))
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

// ─── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("IntegrationTests"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// **VAŽNO**: Authentication + Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

// Da WebApplicationFactory pronađe entry point
public partial class Program { }
