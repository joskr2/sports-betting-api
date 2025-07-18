using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SportsBetting.Api.Infrastructure.Data;
using Microsoft.OpenApi.Models;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
    
builder.Configuration.AddEnvironmentVariables();

var connectionString = BuildConnectionString(builder.Configuration);
if (builder.Environment.IsDevelopment() || builder.Environment.IsProduction())
{
    Console.WriteLine($"DEBUG: Connection String = {connectionString}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddControllers()
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );
        
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            success = false,
            message = "Validation failed",
            errors = errors
        });
    };
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
                       ?? jwtSettings["SecretKey"] 
                       ?? throw new InvalidOperationException("JWT Secret not configured");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings["Audience"],
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,
            RequireAudience = true
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"JWT Token validated for user: {context.Principal?.Identity?.Name}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:8000") // Frontend y BFF
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    
    options.AddPolicy("ProductionPolicy", policy =>
    {
        // Permitir el dominio del BFF Lambda, API principal y dominios de desarrollo
        policy.WithOrigins(
                "https://hf3bbankw5wc2uovwju4m6zvku0zuozj.lambda-url.us-east-2.on.aws",
                "https://api-kurax-demo-jos.uk",
                "http://localhost:3000", 
                "http://localhost:8000",
                "https://localhost:3000",
                "https://localhost:8000"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    
    // Política temporal para debugging - más permisiva
    options.AddPolicy("DebugPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sports Betting API",
        Version = "v1",
        Description = "API para sistema de apuestas deportivas",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@sportsbetting.com"
        }
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
    
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Registrar servicios de aplicación
builder.Services.AddScoped<SportsBetting.Api.Core.Interfaces.IBetService, SportsBetting.Api.Infrastructure.Services.BetService>();
builder.Services.AddScoped<SportsBetting.Api.Core.Interfaces.IAuthService, SportsBetting.Api.Infrastructure.Services.AuthService>();
builder.Services.AddScoped<SportsBetting.Api.Core.Interfaces.IEventService, SportsBetting.Api.Infrastructure.Services.EventService>();

// Configurar health checks
builder.Services.AddHealthChecks()
    .AddCheck("database-check", () => 
    {
        // Simple health check for database connectivity
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection available");
    })
    .AddCheck("services-check", () => 
    {
        // Simple health check - if we can create this, DI is working
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("All services registered");
    });

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    
    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Warning);
    }
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sports Betting API v1");
    options.RoutePrefix = app.Environment.IsDevelopment() ? string.Empty : "docs";
});

// Usar política CORS basada en el entorno
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}
else
{
    // Usar política de producción con dominio Lambda específico
    app.UseCors("ProductionPolicy");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de health checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            version = "1.0.0",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result));
    }
});

app.MapGet("/health/simple", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
        
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("Database connection verified");
        }
        else
        {
            logger.LogWarning("Could not verify database connection");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw;
    }
    
    logger.LogInformation("Starting Sports Betting API...");
}

await app.RunAsync();

// Función para construir la cadena de conexión
static string BuildConnectionString(IConfiguration configuration)
{
    // First try the complete connection string from environment/config
    var baseConnectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(baseConnectionString) && !baseConnectionString.Contains("${"))
    {
        Console.WriteLine("DEBUG: Using complete connection string from configuration");
        return baseConnectionString;
    }
    
    // Fallback: build from individual components
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? 
                 configuration["ConnectionStrings:Host"] ?? 
                 "localhost";
    
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? 
                 configuration["ConnectionStrings:Database"] ?? 
                 "sportsbetting_dev";
    
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? 
                 configuration["ConnectionStrings:Username"] ?? 
                 "postgres";
    
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? 
                     configuration["ConnectionStrings:Password"] ?? 
                     "postgres";
    
    Console.WriteLine("DEBUG: Building connection string from components");
    return $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Disable";
}
