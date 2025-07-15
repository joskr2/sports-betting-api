using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Api.Core.Entities
{
    public class Bet
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required] 
        public int EventId { get; set; }
        
        [Required(ErrorMessage = "Selected team is required")]
        public string SelectedTeam { get; set; } = string.Empty;
        
        [Range(1, 10000, ErrorMessage = "Amount must be between 1 and 10,000")]
        public decimal Amount { get; set; }
        
        public decimal Odds { get; set; }
        
        public BetStatus Status { get; set; } = BetStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual User User { get; set; } = null!;
        public virtual Event Event { get; set; } = null!;
        
        
        public decimal PotentialWin => Amount * Odds;
        
        public decimal PotentialProfit => PotentialWin - Amount;
        
        public decimal MarkAsWon()
        {
            if (Status != BetStatus.Active)
                throw new InvalidOperationException("Only active bets can be marked as won");
                
            Status = BetStatus.Won;
            UpdatedAt = DateTime.UtcNow;
            
            return PotentialWin;
        }
        
        public void MarkAsLost()
        {
            if (Status != BetStatus.Active)
                throw new InvalidOperationException("Only active bets can be marked as lost");
                
            Status = BetStatus.Lost;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public decimal ProcessRefund()
        {
            if (Status == BetStatus.Refunded)
                throw new InvalidOperationException("Bet is already refunded");
                
            Status = BetStatus.Refunded;
            UpdatedAt = DateTime.UtcNow;

            return Amount;
        }
        
        public bool CanBeCancelled()
        {
            return Status == BetStatus.Active && 
                   Event?.Status == EventStatus.Upcoming &&
                   Event?.EventDate > DateTime.UtcNow;
        }
        
        public bool IsValidForEvent()
        {
            if (Event is null) return false;
            
            return Event.IsValidTeam(SelectedTeam) && 
                   Event.IsAvailableForBetting() &&
                   Odds == Event.GetOddsForTeam(SelectedTeam);
        }
    }
    
    public enum BetStatus
    {
        Active = 0,
        Won = 1,
        Lost = 2,
        Refunded = 3
    }
}
