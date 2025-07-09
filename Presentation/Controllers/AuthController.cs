using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Interfaces;
using SportsBetting.Api.Presentation.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SportsBetting.Api.Presentation.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        

        public AuthController(IAuthService authService, ILogger<AuthController> logger) 
            : base(logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }
        

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);
            try
            {
                var emailExists = await _authService.EmailExistsAsync(request.Email);
                if (emailExists)
                {
                    _logger.LogWarning("Registration failed: Email already exists - {Email}", request.Email);
                    return ErrorResponse("Email already registered", 409);
                }
                var result = await _authService.RegisterAsync(request);
                
                if (result == null)
                {
                    _logger.LogError("Registration failed for unknown reason: {Email}", request.Email);
                    return ErrorResponse("Registration failed", 400);
                }
                
                _logger.LogInformation("User registered successfully: {Email}", request.Email);
                
                return Created($"/api/auth/user/{result.Email}", SuccessResponse(result, "User registered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration: {Email}", request.Email);
                return ErrorResponse("Registration failed due to server error", 500);
            }
        }
        

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);
            
            try
            {
                var result = await _authService.LoginAsync(request);
                
                if (result == null)
                {
                    _logger.LogWarning("Login failed for email: {Email}", request.Email);
                    return ErrorResponse("Invalid email or password", 401);
                }
                
                _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                return SuccessResponse(result, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login: {Email}", request.Email);
                return ErrorResponse("Login failed due to server error", 500);
            }
        }
        

        [HttpPost("validate-token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return ErrorResponse("Token is required", 400);
                }
                
                var user = await _authService.ValidateTokenAsync(request.Token);
                
                if (user == null)
                {
                    return ErrorResponse("Invalid or expired token", 401);
                }
                
                var userInfo = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Balance,
                    TokenValid = true
                };
                
                return SuccessResponse(userInfo, "Token is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return ErrorResponse("Token validation failed", 500);
            }
        }
        

        [HttpGet("profile")]
        [Authorize] // Requiere token JWT válido
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            return await HandleActionAsync<UserProfileDto>(async () =>
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Fetching profile for user: {UserId}", userId);
                
                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }
                
                var profile = new UserProfileDto
                {
                    Id = userId,
                    Email = user.Email,
                    FullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown",
                    Balance = decimal.Parse(User.FindFirst("balance")?.Value ?? "0"),
                    CreatedAt = user.CreatedAt,
                    TotalBets = 0,
                    TotalBetAmount = 0
                };
                
                return profile;
            }, "GetProfile");
        }
        

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            try
            {
                var userEmail = GetCurrentUserEmail();
                _logger.LogInformation("User logged out: {Email}", userEmail);
                
                return SuccessResponse(new { message = "Logged out successfully" }, "Logout successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return ErrorResponse("Logout failed", 500);
            }
        }
    }

    public class TokenValidationRequest
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }
}
