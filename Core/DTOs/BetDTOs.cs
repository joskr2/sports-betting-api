using SportsBetting.Api.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Api.Core.DTOs
{
    public class CreateBetDto
    {
        [Required(ErrorMessage = "Event ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Event ID must be a positive number")]
        public int EventId { get; set; }
        
        [Required(ErrorMessage = "Selected team is required")]
        [MinLength(1, ErrorMessage = "Selected team cannot be empty")]
        public string SelectedTeam { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 10000, ErrorMessage = "Amount must be between 1 and 10,000")]
        public decimal Amount { get; set; }
        
        public bool IsValidAmount()
        {
            return Amount > 0 && Amount <= 10000 && 
                   Amount == Math.Round(Amount, 2);
        }
    }
    
    public class BetResponseDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string SelectedTeam { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Odds { get; set; }
        public decimal PotentialWin { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        public string EventStatus { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public bool CanBeCancelled { get; set; }
        public string TimeUntilEvent { get; set; } = string.Empty;
    }
    
    public class BetSummaryDto
    {
        public int Id { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string SelectedTeam { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    
    public class UserBetStatsDto
    {
        public int TotalBets { get; set; }
        public int ActiveBets { get; set; }
        public int WonBets { get; set; }
        public int LostBets { get; set; }
        public decimal TotalAmountBet { get; set; }
        public decimal TotalWinnings { get; set; }
        public decimal CurrentPotentialWin { get; set; }
        public double WinRate { get; set; }
        public decimal AverageBetAmount { get; set; }
    }
}
