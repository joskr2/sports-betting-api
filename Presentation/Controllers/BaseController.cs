using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SportsBetting.Api.Presentation.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger _logger;

        protected BaseController(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError("Unable to extract user ID from JWT token");
                throw new UnauthorizedAccessException("Invalid user token");
            }
            
            return userId;
        }
        

        protected string GetCurrentUserEmail()
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            return emailClaim ?? "Unknown";
        }
        

        protected bool IsUserAuthenticated()
        {
            return User.Identity?.IsAuthenticated == true;
        }
        

        protected IActionResult SuccessResponse<T>(T data, string message = "Operation successful")
        {
            var response = new
            {
                success = true,
                message = message,
                data = data,
                timestamp = DateTime.UtcNow
            };
            
            return Ok(response);
        }
        

        protected IActionResult ErrorResponse(string message, int statusCode = 400, object? details = null)
        {
            var response = new
            {
                success = false,
                error = GetErrorTypeFromStatusCode(statusCode),
                message = message,
                details = details,
                timestamp = DateTime.UtcNow
            };
            
            _logger.LogWarning("API Error Response: {StatusCode} - {Message}", statusCode, message);
            
            return StatusCode(statusCode, response);
        }
        

        protected async Task<IActionResult> HandleActionAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                _logger.LogInformation("Starting operation: {OperationName} for user: {UserEmail}", 
                    operationName, GetCurrentUserEmail());
                
                var result = await action();
                
                if (result == null)
                {
                    return ErrorResponse($"{operationName} failed", 400);
                }
                
                _logger.LogInformation("Operation completed successfully: {OperationName}", operationName);
                return SuccessResponse(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during {OperationName}", operationName);
                return ErrorResponse("Unauthorized access", 401);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument during {OperationName}", operationName);
                return ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {OperationName}", operationName);
                return ErrorResponse("An unexpected error occurred", 500);
            }
        }
        

        private string GetErrorTypeFromStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "ValidationError",
                401 => "AuthenticationError", 
                403 => "AuthorizationError",
                404 => "NotFoundError",
                409 => "ConflictError",
                500 => "InternalServerError",
                _ => "UnknownError"
            };
        }
    }
}
