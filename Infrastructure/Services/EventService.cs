using Microsoft.EntityFrameworkCore;
using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;
using SportsBetting.Api.Core.Interfaces;
using SportsBetting.Api.Infrastructure.Data;

namespace SportsBetting.Api.Infrastructure.Services
{

    public class EventService(
        ApplicationDbContext context,
        ILogger<EventService> logger) : IEventService
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<EventService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


        public async Task<IEnumerable<EventResponseDto>> GetAvailableEventsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Fetching available events for betting - Page: {Page}, PageSize: {PageSize}", page, pageSize);

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var events = await _context.Events
                    .Where(e => e.Status == EventStatus.Upcoming &&
                               e.EventDate > DateTime.UtcNow.AddMinutes(15))
                    .Include(e => e.Bets)
                    .OrderBy(e => e.EventDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var eventDtos = events.Select(e => new EventResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    TeamA = e.TeamA,
                    TeamB = e.TeamB,
                    TeamAOdds = e.TeamAOdds,
                    TeamBOdds = e.TeamBOdds,
                    EventDate = e.EventDate,
                    Status = e.Status.ToString(),
                    CanPlaceBets = e.IsAvailableForBetting(),
                    TimeUntilEvent = GetTimeUntilEvent(e.EventDate),
                    TotalBetsAmount = e.Bets.Sum(b => b.Amount),
                    TotalBetsCount = e.Bets.Count
                }).ToList();

                _logger.LogInformation("Retrieved {Count} available events for page {Page}", eventDtos.Count, page);

                return eventDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available events");
                throw;
            }
        }


        public async Task<EventDetailDto?> GetEventByIdAsync(int eventId)
        {
            try
            {
                _logger.LogInformation("Fetching event details for ID: {EventId}", eventId);

                var eventEntity = await _context.Events
                    .Include(e => e.Bets)
                        .ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (eventEntity is null)
                {
                    _logger.LogWarning("Event not found: {EventId}", eventId);
                    return null;
                }

                var eventDetailDto = new EventDetailDto
                {
                    Id = eventEntity.Id,
                    Name = eventEntity.Name,
                    TeamA = eventEntity.TeamA,
                    TeamB = eventEntity.TeamB,
                    TeamAOdds = eventEntity.TeamAOdds,
                    TeamBOdds = eventEntity.TeamBOdds,
                    EventDate = eventEntity.EventDate,
                    Status = eventEntity.Status.ToString(),
                    CanPlaceBets = eventEntity.IsAvailableForBetting(),
                    TimeUntilEvent = GetTimeUntilEvent(eventEntity.EventDate),
                    TotalBetsAmount = eventEntity.Bets.Sum(b => b.Amount),
                    TotalBetsCount = eventEntity.Bets.Count,
                    CreatedAt = eventEntity.CreatedAt,

                    RecentBets = eventEntity.Bets
                        .OrderByDescending(b => b.CreatedAt)
                        .Take(10)
                        .Select(b => new BetSummaryDto
                        {
                            Id = b.Id,
                            EventName = eventEntity.Name,
                            SelectedTeam = b.SelectedTeam,
                            Amount = b.Amount,
                            Status = b.Status.ToString(),
                            CreatedAt = b.CreatedAt
                        }).ToList(),

                    TeamStatistics = new Dictionary<string, decimal>
                    {
                        ["TeamABets"] = eventEntity.Bets.Where(b => b.SelectedTeam == eventEntity.TeamA).Sum(b => b.Amount),
                        ["TeamBBets"] = eventEntity.Bets.Where(b => b.SelectedTeam == eventEntity.TeamB).Sum(b => b.Amount),
                        ["TeamACount"] = eventEntity.Bets.Count(b => b.SelectedTeam == eventEntity.TeamA),
                        ["TeamBCount"] = eventEntity.Bets.Count(b => b.SelectedTeam == eventEntity.TeamB)
                    }
                };

                _logger.LogInformation("Event details retrieved for ID: {EventId}", eventId);
                return eventDetailDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching event details for ID: {EventId}", eventId);
                throw;
            }
        }


        public async Task<bool> IsEventAvailableForBettingAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);

                if (eventEntity is null)
                {
                    return false;
                }

                return eventEntity.IsAvailableForBetting();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking event availability: {EventId}", eventId);
                return false;
            }
        }


        public async Task<EventStatsDto> GetEventStatsAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Bets)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (eventEntity is null)
                {
                    throw new ArgumentException($"Event with ID {eventId} not found");
                }

                var totalBets = eventEntity.Bets.Count;
                var totalAmount = eventEntity.Bets.Sum(b => b.Amount);
                var teamABets = eventEntity.Bets.Where(b => b.SelectedTeam == eventEntity.TeamA).Sum(b => b.Amount);
                var teamBBets = eventEntity.Bets.Where(b => b.SelectedTeam == eventEntity.TeamB).Sum(b => b.Amount);

                return new EventStatsDto
                {
                    TotalBets = totalBets,
                    TotalAmountBet = totalAmount,
                    TeamAPercentage = totalAmount > 0 ? (teamABets / totalAmount) * 100 : 0,
                    TeamBPercentage = totalAmount > 0 ? (teamBBets / totalAmount) * 100 : 0,
                    LastBetDate = eventEntity.Bets.Count == 0 ? eventEntity.Bets.Max(b => b.CreatedAt) : DateTime.MinValue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event stats for ID: {EventId}", eventId);
                throw;
            }
        }


        public async Task<EventResponseDto> CreateEventAsync(CreateEventDto eventDto)
        {
            try
            {
                _logger.LogInformation("Creating new event: {EventName}", eventDto.Name);

                if (!eventDto.IsEventDateValid())
                {
                    throw new ArgumentException("Event date must be at least 1 hour in the future");
                }

                var eventEntity = new Event
                {
                    Name = eventDto.Name.Trim(),
                    TeamA = eventDto.TeamA.Trim(),
                    TeamB = eventDto.TeamB.Trim(),
                    TeamAOdds = eventDto.TeamAOdds,
                    TeamBOdds = eventDto.TeamBOdds,
                    EventDate = eventDto.EventDate,
                    Status = EventStatus.Upcoming,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Events.Add(eventEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event created successfully with ID: {EventId}", eventEntity.Id);

                return new EventResponseDto
                {
                    Id = eventEntity.Id,
                    Name = eventEntity.Name,
                    TeamA = eventEntity.TeamA,
                    TeamB = eventEntity.TeamB,
                    TeamAOdds = eventEntity.TeamAOdds,
                    TeamBOdds = eventEntity.TeamBOdds,
                    EventDate = eventEntity.EventDate,
                    Status = eventEntity.Status.ToString(),
                    CanPlaceBets = eventEntity.IsAvailableForBetting(),
                    TimeUntilEvent = GetTimeUntilEvent(eventEntity.EventDate),
                    TotalBetsAmount = 0,
                    TotalBetsCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event: {EventName}", eventDto.Name);
                throw;
            }
        }


        public async Task<bool> UpdateEventStatusAsync(int eventId, EventStatus newStatus)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);

                if (eventEntity is null)
                {
                    return false;
                }

                eventEntity.Status = newStatus;
                eventEntity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Event {EventId} status updated to {Status}", eventId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event status: {EventId}", eventId);
                throw;
            }
        }


        private static string GetTimeUntilEvent(DateTime eventDate)
        {
            var timeSpan = eventDate - DateTime.UtcNow;

            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days} days";
            else if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours} hours";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minutes";
            else
                return "Starting soon";
        }
    }
}
