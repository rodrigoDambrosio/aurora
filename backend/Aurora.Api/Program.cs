using Aurora.Application.Interfaces;
using Aurora.Application.Services;
using Aurora.Infrastructure.Data;
using Aurora.Infrastructure.Repositories;
using Aurora.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICE CONFIGURATION =====

// Entity Framework Configuration
builder.Services.AddDbContext<AuroraDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                          ?? "Data Source=aurora.db";
    options.UseSqlite(connectionString);

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Repository Pattern - Generic Repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Repository Pattern - Specific Repositories
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventCategoryRepository, EventCategoryRepository>();

// Application Services
builder.Services.AddScoped<IEventService, EventService>();

// AI Validation Service with HttpClient
builder.Services.AddHttpClient<IAIValidationService, GeminiAIValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// API Controllers
builder.Services.AddControllers();

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// CORS Configuration for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite default port
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// ===== APPLICATION PIPELINE =====

var app = builder.Build();

// Initialize Database in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AuroraDbContext>();
        await DbInitializer.InitializeAsync(context);
    }
}

// Development Environment Configuration
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
}
else
{
    // Only redirect to HTTPS in production
    app.UseHttpsRedirection();
}

// Routing
app.UseRouting();

// Controllers
app.MapControllers();

// Health Check Endpoint
app.MapHealthChecks("/health");

// Start the application
app.Run();
