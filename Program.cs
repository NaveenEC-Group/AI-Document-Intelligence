using AI_Document_Intelligence.Data;
using AI_Document_Intelligence.Filters;
using AI_Document_Intelligence.Services;
using AI_Document_Intelligence.Services.Interfaces;
using AI_Document_Intelligence.Validation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = DocumentFileValidator.MaxFileBytes;
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = DocumentFileValidator.MaxFileBytes;
    options.ValueLengthLimit = int.MaxValue;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string sqliteDefault = "Data Source=/app/data/aidocs.db";

string? configuredConnection =
    NormalizeConnectionString(
        builder.Configuration.GetConnectionString("DefaultConnection"));

string databaseProvider =
    builder.Configuration["Database:Provider"]
    ?? (builder.Environment.IsProduction() ? "Sqlite" : "SqlServer");

bool useSqlite =
    databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
    || IsSqliteConnection(configuredConnection);

string connectionString = useSqlite
    ? (IsSqliteConnection(configuredConnection)
        ? configuredConnection!
        : sqliteDefault)
    : (configuredConnection
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured."));

if (useSqlite)
{
    string? dataSource = GetSqliteDataSourcePath(connectionString);
    if (!string.IsNullOrWhiteSpace(dataSource))
    {
        string? directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

Console.WriteLine(
    $"Database provider: {(useSqlite ? "Sqlite" : "SqlServer")}; connection configured.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        if (useSqlite)
        {
            options.UseSqlite(connectionString);
        }
        else
        {
            options.UseSqlServer(connectionString);
        }
    });

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IAiDocumentService, AiDocumentService>();

string[] configuredOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ??
    [
        "http://localhost:4200",
        "https://localhost:4200",
        "https://ai-document-intelligence-kappa.vercel.app"
    ];

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "Angular",
            policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        if (configuredOrigins.Contains(
                                origin,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
                        {
                            return false;
                        }

                        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                            || uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });

WebApplication app = builder.Build();

// Register health before DB init so we can still diagnose boot issues in logs.
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    database = useSqlite ? "sqlite" : "sqlserver"
}));

try
{
    using IServiceScope scope = app.Services.CreateScope();
    ApplicationDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (useSqlite)
    {
        dbContext.Database.EnsureCreated();
        Console.WriteLine("SQLite database ready.");
    }
    else
    {
        dbContext.Database.Migrate();
        Console.WriteLine("SQL Server migrations applied.");
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Database initialization failed: {exception}");
    throw;
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Angular");

app.UseAuthorization();

app.MapControllers();

app.Run();

static string? NormalizeConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return null;
    }

    return connectionString.Trim().Trim('"');
}

static bool IsSqliteConnection(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    return connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase);
}

static string? GetSqliteDataSourcePath(string connectionString)
{
    const string marker = "Data Source=";
    int index = connectionString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
    if (index < 0)
    {
        return null;
    }

    string value = connectionString[(index + marker.Length)..].Trim().Trim('"');
    int separator = value.IndexOf(';');
    if (separator >= 0)
    {
        value = value[..separator];
    }

    return value.Trim().Trim('"');
}
