using EventosVivos.Application;
using EventosVivos.Infrastructure;
using EventosVivos.Infrastructure.Data;
using EventosVivos.Infrastructure.Data.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────

// Controllers + FluentValidation auto-validation
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/400",
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["errorCode"] = "Validation";

        return new BadRequestObjectResult(problem);
    };
});

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
        Contact = new OpenApiContact { Name = "EventosVivos Technical Assessment" }
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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred while processing the request.",
            Type = "https://httpstatuses.com/500",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["errorCode"] = "UnexpectedError";

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("Swagger:Enabled", false);

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EventosVivos API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AngularDev");

// Redirect root to Swagger for local/demo convenience when Swagger is enabled.
app.MapGet("/", () => swaggerEnabled
        ? Results.Redirect("/swagger")
        : Results.Ok(new { name = "EventosVivos API", status = "Healthy" }))
   .ExcludeFromDescription();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
   .ExcludeFromDescription();

app.MapGet("/health/ready", async (EventosVivosDbContext db, HttpContext httpContext, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "Healthy" })
        : Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Service Unavailable",
            detail: "Database connection is not available.",
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier,
                ["errorCode"] = "DatabaseUnavailable"
            }))
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
