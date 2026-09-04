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

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection"));
    });

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IAiDocumentService, AiDocumentService>();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "Angular",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:4200",
                        "https://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Angular");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
