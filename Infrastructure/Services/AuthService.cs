using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;
using SportsBetting.Api.Core.Interfaces;
using SportsBetting.Api.Infrastructure.Data;

namespace SportsBetting.Api.Infrastructure.Services
{

    public class AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger) : IAuthService
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private readonly ILogger<AuthService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        

        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                _logger.LogInformation("Attempting to register user with email: {Email}", request.Email);
                
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));
                
                if (existingUser is not null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                    return null;
                }
                
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
                
                  var user = new User
                {
                    Email = request.Email.ToLower().Trim(),
                    PasswordHash = passwordHash,
                    FullName = request.FullName.Trim(),
                    Balance = _configuration.GetValue<decimal>("UserSettings:InitialBalance", 1000.00m),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);
                
                var token = GenerateJwtToken(user);
                
                return new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    FullName = user.FullName,
                    Balance = user.Balance,
                    ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JwtSettings:TokenExpirationDays", 7)), // Token válido por días configurables
                    Message = "Registration successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                throw;
            }
        }
        

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);
                
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));
                
                if (user is null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                    return null;
                }
                
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);
                    return null;
                }
                
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                
                var token = GenerateJwtToken(user);
                
                return new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    FullName = user.FullName,
                    Balance = user.Balance,
                    ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JwtSettings:TokenExpirationDays", 7)),
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                throw;
            }
        }
        

        public string GenerateJwtToken(User user)
        {
            try
            { 
                var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
                    ?? _configuration["JwtSettings:SecretKey"] 
                    ?? throw new InvalidOperationException("JWT secret key not configured");
                
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim("balance", user.Balance.ToString("F2")),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, 
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                        ClaimValueTypes.Integer64)
                };
                
                var token = new JwtSecurityToken(
                    issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["JwtSettings:Issuer"],
                    audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _configuration["JwtSettings:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JwtSettings:TokenExpirationDays", 7)), // Token válido por días configurables
                    signingCredentials: credentials
                );
                
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                
                _logger.LogDebug("JWT token generated for user: {UserId}", user.Id);
                
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);
                throw;
            }
        }
        

        public async Task<User?> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
                    ?? _configuration["JwtSettings:SecretKey"] 
                    ?? throw new InvalidOperationException("JWT secret key not configured");
                
                var key = Encoding.UTF8.GetBytes(secretKey);
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Sin tolerancia para expiración
                };
                
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
                {
                    return null;
                }
                
                var user = await _context.Users.FindAsync(userId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }
        

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _context.Users
                    .AnyAsync(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }
        

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.FindAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                throw;
            }
        }
    }
}
