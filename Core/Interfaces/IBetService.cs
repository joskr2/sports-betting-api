using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;

namespace SportsBetting.Api.Core.Interfaces
{

    public interface IBetService
    {

        Task<BetResponseDto?> CreateBetAsync(int userId, CreateBetDto betDto);
        

        Task<IEnumerable<BetResponseDto>> GetUserBetsAsync(int userId, BetFilterDto? filter = null);
        

        Task<UserBetStatsDto> GetUserBetStatsAsync(int userId);
        

        Task<bool> CancelBetAsync(int userId, int betId);
        

        Task<BetSettlementResultDto> SettleEventBetsAsync(int eventId, string winnerTeam);
        

        Task<BetValidationResult> ValidateBetAsync(int userId, CreateBetDto betDto);
    }
    

    public class BetFilterDto
    {
        public BetStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool OnlyActive { get; set; } = false;
    }
    

    public class BetSettlementResultDto
    {
        public int EventId { get; set; }
        public string WinnerTeam { get; set; } = string.Empty;
        public int TotalBetsProcessed { get; set; }
        public int WinningBets { get; set; }
        public int LosingBets { get; set; }
        public decimal TotalPayouts { get; set; }
        public DateTime SettledAt { get; set; }
    }
    

    public class BetValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal CurrentOdds { get; set; }
        public decimal UserBalance { get; set; }
    }
}
