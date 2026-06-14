using EventosVivos.Application;
using EventosVivos.Infrastructure;
using EventosVivos.Infrastructure.Data;
using EventosVivos.Infrastructure.Data.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────

// Controllers + FluentValidation auto-validation
builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssembly>();

// Swagger / OpenAPI (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventosVivos API",
        Version = "v1",
        Description = "Professional REST API for EventosVivos event management, reservation lifecycle, and occupancy reporting. Built as part of the Ceiba Software Fullstack .NET + Angular technical assessment.",
        Contact = new OpenApiContact
        {
            Name = "Christian Alexander Portillo (Chris.Port)",
            Email = "wesker980@gmail.com"
        }
    });

    // Enable [SwaggerOperation] attributes for custom operationId/summary
    options.EnableAnnotations();

    // Include XML comments from API, Application, and Domain assemblies
    var apiXmlFile = Path.Combine(AppContext.BaseDirectory,
        $"{typeof(Program).Assembly.GetName().Name}.xml");
    var appXmlFile = Path.Combine(AppContext.BaseDirectory,
        $"{typeof(ApplicationAssembly).Assembly.GetName().Name}.xml");
    // Domain XML is loaded via the Application assembly reference
    var domainXmlFile = Path.Combine(AppContext.BaseDirectory,
        "EventosVivos.Domain.xml");

    if (File.Exists(apiXmlFile)) options.IncludeXmlComments(apiXmlFile);
    if (File.Exists(appXmlFile)) options.IncludeXmlComments(appXmlFile);
    if (File.Exists(domainXmlFile)) options.IncludeXmlComments(domainXmlFile);

    // (Root redirect is excluded via .ExcludeFromDescription() on the route itself)
});

// Application layer (handlers, validators)
builder.Services.AddApplication();

// Infrastructure layer (DbContext, repositories, clock)
var connectionString = builder.Configuration.GetConnectionString("EventosVivosDb")
    ?? throw new InvalidOperationException("Connection string 'EventosVivosDb' not found.");
builder.Services.AddInfrastructure(connectionString);

// CORS for Angular development origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Middleware Pipeline ────────────────────────────────────────────────────

var app = builder.Build();

// Swagger UI in all environments (useful for development and test)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EventosVivos API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("AngularDev");

// Redirect root to Swagger for convenience (excluded from OpenAPI spec)
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

app.MapControllers();

// ── Auto-migration (Development / Docker only) ──────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EventosVivosDbContext>();
    await db.Database.MigrateAsync();

    var demoDataOptions = app.Configuration
        .GetSection(DemoDataOptions.SectionName)
        .Get<DemoDataOptions>() ?? new DemoDataOptions();

    if (demoDataOptions.SeedOnStartup)
    {
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DemoDataSeeder));
        var clock = scope.ServiceProvider.GetRequiredService<EventosVivos.Domain.Services.IClock>();

        await DemoDataSeeder.SeedAsync(db, clock, demoDataOptions, logger);
    }
}

app.Run();
