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
            .Where(e => e.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
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
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sports Betting API v1");
        options.RoutePrefix = "docs";
    });
}

app.UseCors("DevelopmentPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => new
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
    // Prioridad: Variables de entorno > appsettings.json
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? 
                 configuration["ConnectionStrings:Host"] ?? 
                 "localhost";
    
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? 
                 configuration["ConnectionStrings:Database"] ?? 
                 "sportsbetting";
    
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? 
                 configuration["ConnectionStrings:Username"] ?? 
                 "postgres";
    
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? 
                     configuration["ConnectionStrings:Password"] ?? 
                     "password";
    
    // Si existe una cadena de conexión completa, usarla como base
    var baseConnectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(baseConnectionString))
    {
        // Reemplazar componentes individuales si hay variables de entorno
        var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnectionString);
        
        if (Environment.GetEnvironmentVariable("DB_HOST") != null)
            connectionStringBuilder.Host = dbHost;
        if (Environment.GetEnvironmentVariable("DB_NAME") != null)
            connectionStringBuilder.Database = dbName;
        if (Environment.GetEnvironmentVariable("DB_USER") != null)
            connectionStringBuilder.Username = dbUser;
        if (Environment.GetEnvironmentVariable("DB_PASSWORD") != null)
            connectionStringBuilder.Password = dbPassword;
            
        return connectionStringBuilder.ConnectionString;
    }
    
    // Si no hay cadena base, construir desde cero
    return $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true";
}
