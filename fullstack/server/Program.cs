
using System.Text;
using fullstackapplication.Server.Data;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using fullstackapplication.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace fullstackapplication.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before starting the API.");
        }

        var signingKey = builder.Configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("Configure Jwt:SigningKey using a secret store or environment variable.");
            }

            signingKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        }

        var signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        }

        var issuer = builder.Configuration["Jwt:Issuer"] ?? "fullstackapplication";
        var audience = builder.Configuration["Jwt:Audience"] ?? "fullstackapplication";
        var lifetimeMinutes = builder.Configuration.GetValue("Jwt:LifetimeMinutes", 60);
        if (lifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:LifetimeMinutes must be greater than zero.");
        }

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0 || allowedOrigins.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Configure at least one valid origin in Cors:AllowedOrigins.");
        }

        builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddScoped<IPasswordHasher<EmployeeAccount>, PasswordHasher<EmployeeAccount>>();
        builder.Services.AddScoped<IAdminCandidateRepository, AdminCandidateRepository>();
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<ICandidateSelfRepository, CandidateSelfRepository>();
        builder.Services.AddScoped<IEmployeeDirectoryRepository, EmployeeDirectoryRepository>();
        builder.Services.AddScoped<IEmployeeManagementRepository, EmployeeManagementRepository>();
        builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
        builder.Services.AddScoped<IWorkTrackingRepository, WorkTrackingRepository>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IWorkTrackingService, WorkTrackingService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAdminCandidateService, AdminCandidateService>();
        builder.Services.AddScoped<ICandidateSelfService, CandidateSelfService>();
        builder.Services.AddScoped<IEmployeeDirectoryService, EmployeeDirectoryService>();
        builder.Services.AddScoped<IEmployeeManagementService, EmployeeManagementService>();
        builder.Services.AddSingleton(new JwtTokenService(signingKeyBytes, issuer, audience, lifetimeMinutes));
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fullstack Application API",
                Version = "v1"
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a JWT access token."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = "role",
                    NameClaimType = "unique_name"
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (dbContext.Database.IsRelational())
            {
                await dbContext.Database.MigrateAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                await DevelopmentDataSeeder.SeedAsync(
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<IPasswordHasher<EmployeeAccount>>(),
                    app.Configuration);
            }
        }

        app.UseDefaultFiles();
        app.MapStaticAssets();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger(options => options.SerializeAsV2 = true);
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fullstack Application API v1"));
        }

        app.UseHttpsRedirection();
        app.UseCors("Frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapFallbackToFile("/index.html");
        app.Run();
    }
}
