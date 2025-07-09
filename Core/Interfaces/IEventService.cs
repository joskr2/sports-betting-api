using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;

namespace SportsBetting.Api.Core.Interfaces
{
  
    public interface IEventService
    {

        Task<IEnumerable<EventResponseDto>> GetAvailableEventsAsync(int page = 1, int pageSize = 20);
        

        Task<EventDetailDto?> GetEventByIdAsync(int eventId);
        

        Task<bool> IsEventAvailableForBettingAsync(int eventId);
        

        Task<EventStatsDto> GetEventStatsAsync(int eventId);
        

        Task<EventResponseDto> CreateEventAsync(CreateEventDto eventDto);
        

        Task<bool> UpdateEventStatusAsync(int eventId, EventStatus newStatus);
    }
    

    public class EventStatsDto
    {
        public int TotalBets { get; set; }
        public decimal TotalAmountBet { get; set; }
        public decimal TeamAPercentage { get; set; }
        public decimal TeamBPercentage { get; set; }
        public DateTime LastBetDate { get; set; }
    }
}
