using SportsBetting.Api.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Api.Core.DTOs
{
    public class EventResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TeamA { get; set; } = string.Empty;
        public string TeamB { get; set; } = string.Empty;
        public decimal TeamAOdds { get; set; }
        public decimal TeamBOdds { get; set; }
        public DateTime EventDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        public bool CanPlaceBets { get; set; }
        public string TimeUntilEvent { get; set; } = string.Empty;
        public decimal TotalBetsAmount { get; set; }
        public int TotalBetsCount { get; set; }
    }
    
    public class EventDetailDto : EventResponseDto
    {
        public DateTime CreatedAt { get; set; }
        public List<BetSummaryDto> RecentBets { get; set; } = new();
        public Dictionary<string, decimal> TeamStatistics { get; set; } = new();
    }
    
    public class CreateEventDto
    {
        [Required(ErrorMessage = "Event name is required")]
        [MinLength(5, ErrorMessage = "Event name must be at least 5 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Team A is required")]
        public string TeamA { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Team B is required")]
        public string TeamB { get; set; } = string.Empty;
        
        [Range(1.01, 50.0, ErrorMessage = "Team A odds must be between 1.01 and 50.0")]
        public decimal TeamAOdds { get; set; }
        
        [Range(1.01, 50.0, ErrorMessage = "Team B odds must be between 1.01 and 50.0")]
        public decimal TeamBOdds { get; set; }
        
        [Required(ErrorMessage = "Event date is required")]
        public DateTime EventDate { get; set; }
        
        public bool IsEventDateValid()
        {
            return EventDate > DateTime.UtcNow.AddHours(1);
        }
    }
}
