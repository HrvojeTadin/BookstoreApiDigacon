var builder = WebApplication.CreateBuilder(args);

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
if (!builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.AddSqlServerDb();
    builder.AddJwtAuthentication();
    builder.AddSwaggerWithJwt();
}

builder.AddBookstoreServices();
builder.AddQuartzService();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

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

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("IntegrationTests"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

// So WebApplicationFactory can find entry point
public partial class Program { }
