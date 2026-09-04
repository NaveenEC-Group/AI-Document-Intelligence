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

string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");
}

bool useSqlite = IsSqliteConnection(connectionString);

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
        "https://localhost:4200"
    ];

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "Angular",
            policy =>
            {
                policy
                    .WithOrigins(configuredOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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

        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Angular");

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static bool IsSqliteConnection(string connectionString)
{
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

    string value = connectionString[(index + marker.Length)..].Trim();
    int separator = value.IndexOf(';');
    if (separator >= 0)
    {
        value = value[..separator];
    }

    return value.Trim().Trim('"');
}
