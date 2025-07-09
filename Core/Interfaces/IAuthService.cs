using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;

namespace SportsBetting.Api.Core.Interfaces
{

    public interface IAuthService
    {

        Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request);
        

        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
        

        string GenerateJwtToken(User user);
        

        Task<User?> ValidateTokenAsync(string token);
        

        Task<bool> EmailExistsAsync(string email);
        

        Task<User?> GetUserByIdAsync(int userId);
    }
}
