using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;
using SportsBetting.Api.Core.Interfaces;
using SportsBetting.Api.Presentation.Controllers;
using System.ComponentModel.DataAnnotations;

namespace SportsBetting.Api.Presentation.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventsController : BaseController
    {
        private readonly IEventService _eventService;
        
        public EventsController(IEventService eventService, ILogger<EventsController> logger) 
            : base(logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return await HandleActionAsync(async () =>
            {
                _logger.LogInformation("Fetching available events - Page: {Page}, PageSize: {PageSize}", page, pageSize);
                
                var events = await _eventService.GetAvailableEventsAsync(page, pageSize);
                
                _logger.LogInformation("Retrieved {Count} available events", events.Count());
                return events;
            }, "GetEvents");
        }
        

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEvent([FromRoute] int id)
        {
            if (id <= 0)
            {
                return ErrorResponse("Event ID must be a positive number", 400);
            }
            
            try
            {
                _logger.LogInformation("Fetching event details for ID: {EventId}", id);
                
                var eventDetail = await _eventService.GetEventByIdAsync(id);
                
                if (eventDetail == null)
                {
                    _logger.LogWarning("Event not found: {EventId}", id);
                    return ErrorResponse("Event not found", 404);
                }
                
                _logger.LogInformation("Event details retrieved: {EventId} - {EventName}", id, eventDetail.Name);
                return SuccessResponse(eventDetail, "Event details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching event: {EventId}", id);
                return ErrorResponse("Failed to retrieve event details", 500);
            }
        }
        

        [HttpGet("{id:int}/stats")]
        [ProducesResponseType(typeof(EventStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventStats([FromRoute] int id)
        {
            return await HandleActionAsync(async () =>
            {
                _logger.LogInformation("Fetching statistics for event: {EventId}", id);
                
                var stats = await _eventService.GetEventStatsAsync(id);
                
                _logger.LogInformation("Statistics retrieved for event: {EventId}", id);
                return stats;
            }, "GetEventStats");
        }
        

        [HttpGet("{id:int}/availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CheckEventAvailability([FromRoute] int id)
        {
            return await HandleActionAsync(async () =>
            {
                _logger.LogInformation("Checking availability for event: {EventId}", id);
                
                var isAvailable = await _eventService.IsEventAvailableForBettingAsync(id);
                
                var result = new
                {
                    EventId = id,
                    IsAvailable = isAvailable,
                    Message = isAvailable ? "Event is available for betting" : "Event is not available for betting",
                    CheckedAt = DateTime.UtcNow
                };
                
                return result;
            }, "CheckEventAvailability");
        }
        

        [HttpPost]
        [Authorize] // Requiere autenticación
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto eventDto)
        {
            return await HandleActionAsync(async () =>
            {
                var userEmail = GetCurrentUserEmail();
                _logger.LogInformation("Creating new event: {EventName} by user: {UserEmail}", eventDto.Name, userEmail);
                
                if (eventDto.TeamA.Equals(eventDto.TeamB, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Team A and Team B cannot be the same");
                }
                
                if (!eventDto.IsEventDateValid())
                {
                    throw new ArgumentException("Event date must be at least 1 hour in the future");
                }
                
                var result = await _eventService.CreateEventAsync(eventDto);
                
                _logger.LogInformation("Event created successfully: {EventId} - {EventName}", result.Id, result.Name);
                
                return Created($"/api/events/{result.Id}", result);
            }, "CreateEvent");
        }
        

        [HttpPatch("{id:int}/status")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEventStatus([FromRoute] int id, [FromBody] UpdateEventStatusRequest request)
        {
            return await HandleActionAsync(async () =>
            {
                var userEmail = GetCurrentUserEmail();
                _logger.LogInformation("Updating status for event {EventId} to {Status} by user {UserEmail}", 
                    id, request.Status, userEmail);
                
                if (!Enum.IsDefined(typeof(EventStatus), request.Status))
                {
                    throw new ArgumentException($"Invalid event status: {request.Status}");
                }
                
                var success = await _eventService.UpdateEventStatusAsync(id, request.Status);
                
                if (!success)
                {
                    throw new ArgumentException($"Event with ID {id} not found");
                }
                
                var result = new
                {
                    EventId = id,
                    NewStatus = request.Status.ToString(),
                    UpdatedBy = userEmail,
                    UpdatedAt = DateTime.UtcNow
                };
                
                return result;
            }, "UpdateEventStatus");
        }
        

        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchEvents(
            [FromQuery] string? team = null,
            [FromQuery] DateTime? date = null,
            [FromQuery] EventStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await HandleActionAsync(async () =>
            {
                _logger.LogInformation("Searching events with filters - Team: {Team}, Date: {Date}, Status: {Status}, Page: {Page}, PageSize: {PageSize}", 
                    team, date, status, page, pageSize);
                
                var allEvents = await _eventService.GetAvailableEventsAsync(1, 1000);
                
                var filteredEvents = allEvents.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(team))
                {
                    filteredEvents = filteredEvents.Where(e => 
                        e.TeamA.Contains(team, StringComparison.OrdinalIgnoreCase) ||
                        e.TeamB.Contains(team, StringComparison.OrdinalIgnoreCase));
                }
                
                if (date.HasValue)
                {
                    var targetDate = date.Value.Date;
                    filteredEvents = filteredEvents.Where(e => e.EventDate.Date == targetDate);
                }
                
                if (status.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.Status == status.Value.ToString());
                }
                
                var totalItems = filteredEvents.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                var pagedResults = filteredEvents
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                _logger.LogInformation("Search completed. Found {TotalCount} events matching criteria, returning page {Page} with {PageCount} items", 
                    totalItems, page, pagedResults.Count);
                
                var result = new
                {
                    Data = pagedResults,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                        HasPreviousPage = page > 1,
                        HasNextPage = page < totalPages
                    },
                    Filters = new
                    {
                        Team = team,
                        Date = date,
                        Status = status
                    }
                };
                
                return result;
            }, "SearchEvents");
        }
    }
    

    public class UpdateEventStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public EventStatus Status { get; set; }
        
        public string? Reason { get; set; }
    }
}
