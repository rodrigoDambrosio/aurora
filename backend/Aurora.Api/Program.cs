using System.IO;
using System.Text;
using Aurora.Application.Interfaces;
using Aurora.Application.Options;
using Aurora.Application.Services;
using Aurora.Application.Validators;
using Aurora.Infrastructure.Data;
using Aurora.Infrastructure.Repositories;
using Aurora.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICE CONFIGURATION =====

// Entity Framework Configuration
builder.Services.AddDbContext<AuroraDbContext>(options =>
{
    var defaultDbPath = Path.Combine(builder.Environment.ContentRootPath, "aurora.db");

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = $"Data Source={defaultDbPath}";
    }
    else
    {
        const string relativeDataSource = "Data Source=aurora.db";
        if (connectionString.Contains(relativeDataSource, StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.Replace(relativeDataSource, $"Data Source={defaultDbPath}", StringComparison.OrdinalIgnoreCase);
        }
    }

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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IDailyMoodRepository, DailyMoodRepository>();
builder.Services.AddScoped<IRecommendationFeedbackRepository, RecommendationFeedbackRepository>();
builder.Services.AddScoped<IScheduleSuggestionRepository, ScheduleSuggestionRepository>();

// Application Services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDailyMoodService, DailyMoodService>();
builder.Services.AddScoped<IWellnessInsightsService, WellnessInsightsService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IScheduleSuggestionService, ScheduleSuggestionService>();

// AI Validation Service with HttpClient
// Timeout extendido a 120 segundos para generación de planes multi-día (PLAN-138)
builder.Services.AddHttpClient<IAIValidationService, GeminiAIValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserRequestDtoValidator>();

var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("La configuración de JWT es obligatoria.");

if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
{
    throw new InvalidOperationException("La clave secreta de JWT debe tener al menos 32 caracteres.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSettings.Issuer),
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtSettings.Audience),
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// API Controllers
builder.Services.AddControllers();

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// CORS Configuration for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174"
            ) // Vite dev ports
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

        // Update database index to support soft delete with unique constraint
        var connectionString = context.Database.GetConnectionString();
        if (!string.IsNullOrEmpty(connectionString))
        {
            Aurora.Api.UpdateDatabaseIndex.Execute(connectionString);

            // Clean up duplicate categories
            Aurora.Api.CleanupDuplicates.Execute(connectionString);
        }
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

app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health Check Endpoint
app.MapHealthChecks("/health");

// Start the application
app.Run();
