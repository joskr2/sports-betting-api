using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;
using SportsBetting.Api.Core.Interfaces;
using SportsBetting.Api.Presentation.Controllers;

namespace SportsBetting.Api.Presentation.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class BetsController : BaseController
    {
        private readonly IBetService _betService;
        private readonly IEventService _eventService;
        
        public BetsController(
            IBetService betService, 
            IEventService eventService,
            ILogger<BetsController> logger) 
            : base(logger)
        {
            _betService = betService ?? throw new ArgumentNullException(nameof(betService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }
        

        [HttpPost]
        [ProducesResponseType(typeof(BetResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBet([FromBody] CreateBetDto betDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userEmail = GetCurrentUserEmail();
                
                _logger.LogInformation("Creating bet for user {UserId} ({UserEmail}) - Event: {EventId}, Team: {Team}, Amount: {Amount:C}", 
                    userId, userEmail, betDto.EventId, betDto.SelectedTeam, betDto.Amount);
                
                var validationResult = await _betService.ValidateBetAsync(userId, betDto);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Bet validation failed for user {UserId}: {Errors}", 
                        userId, string.Join(", ", validationResult.Errors));
                    
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bet validation failed",
                        errors = validationResult.Errors,
                        userBalance = validationResult.UserBalance,
                        currentOdds = validationResult.CurrentOdds
                    });
                }
                
                var result = await _betService.CreateBetAsync(userId, betDto);
                
                if (result == null)
                {
                    _logger.LogError("Bet creation failed for user {UserId} despite validation passing", userId);
                    return ErrorResponse("Bet creation failed unexpectedly", 500);
                }
                
                _logger.LogInformation("Bet created successfully - ID: {BetId}, User: {UserId}, Amount: {Amount:C}", 
                    result.Id, userId, result.Amount);
                
                _logger.LogInformation("AUDIT: Bet created - BetId: {BetId}, UserId: {UserId}, EventId: {EventId}, " +
                    "Amount: {Amount:C}, Odds: {Odds}, PotentialWin: {PotentialWin:C}", 
                    result.Id, userId, result.EventId, result.Amount, result.Odds, result.PotentialWin);
                
                return SuccessResponse(result, "Bet created successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access during CreateBet");
                return ErrorResponse("Unauthorized access", 401);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument during CreateBet");
                return ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during CreateBet");
                return ErrorResponse("An unexpected error occurred", 500);
            }
        }
        

        [HttpGet("my-bets")]
        [ProducesResponseType(typeof(IEnumerable<BetResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyBets(
            [FromQuery] BetStatus? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool onlyActive = false)
        {
            return await HandleActionAsync(async () =>
            {
                var userId = GetCurrentUserId();
                var userEmail = GetCurrentUserEmail();
                
                _logger.LogInformation("Fetching bets for user {UserId} ({UserEmail}) with filters - Status: {Status}, " +
                    "FromDate: {FromDate}, ToDate: {ToDate}, OnlyActive: {OnlyActive}", 
                    userId, userEmail, status, fromDate, toDate, onlyActive);
                
                var filter = new BetFilterDto
                {
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    OnlyActive = onlyActive
                };
                
                var bets = await _betService.GetUserBetsAsync(userId, filter);
                
                _logger.LogInformation("Retrieved {Count} bets for user {UserId}", bets.Count(), userId);
                
                return bets;
            }, "GetMyBets");
        }
        

        [HttpGet("my-stats")]
        [ProducesResponseType(typeof(UserBetStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyBetStats()
        {
            return await HandleActionAsync(async () =>
            {
                var userId = GetCurrentUserId();
                var userEmail = GetCurrentUserEmail();
                
                _logger.LogInformation("Calculating bet statistics for user {UserId} ({UserEmail})", userId, userEmail);
                
                var stats = await _betService.GetUserBetStatsAsync(userId);
                
                _logger.LogInformation("Statistics calculated for user {UserId}: {TotalBets} bets, " +
                    "{WinRate:F1}% win rate, {TotalAmount:C} total bet", 
                    userId, stats.TotalBets, stats.WinRate, stats.TotalAmountBet);
                
                return stats;
            }, "GetMyBetStats");
        }
        

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BetResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBet([FromRoute] int id)
        {
            return await HandleActionAsync(async () =>
            {
                var userId = GetCurrentUserId();
                
                _logger.LogInformation("Fetching bet {BetId} for user {UserId}", id, userId);
                
                var userBets = await _betService.GetUserBetsAsync(userId);
                var bet = userBets.FirstOrDefault(b => b.Id == id);
                
                if (bet == null)
                {
                    _logger.LogWarning("Bet {BetId} not found for user {UserId}", id, userId);
                    throw new ArgumentException($"Bet with ID {id} not found");
                }
                
                return bet;
            }, "GetBet");
        }
        

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelBet([FromRoute] int id)
        {
            return await HandleActionAsync(async () =>
            {
                var userId = GetCurrentUserId();
                var userEmail = GetCurrentUserEmail();
                
                _logger.LogInformation("Attempting to cancel bet {BetId} for user {UserId} ({UserEmail})", 
                    id, userId, userEmail);
                
                var success = await _betService.CancelBetAsync(userId, id);
                
                if (!success)
                {
                    _logger.LogWarning("Bet cancellation failed for bet {BetId}, user {UserId}", id, userId);
                    throw new InvalidOperationException("Bet cannot be cancelled. It may have already started or been settled.");
                }
                
                _logger.LogInformation("AUDIT: Bet cancelled - BetId: {BetId}, UserId: {UserId}, CancelledBy: {UserEmail}", 
                    id, userId, userEmail);
                
                var result = new
                {
                    BetId = id,
                    Status = "Cancelled",
                    CancelledAt = DateTime.UtcNow,
                    Message = "Bet has been successfully cancelled and amount refunded"
                };
                
                return result;
            }, "CancelBet");
        }
        

        [HttpPost("preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PreviewBet([FromBody] CreateBetDto betDto)
        {
            return await HandleActionAsync(async () =>
            {
                var userId = GetCurrentUserId();
                
                _logger.LogInformation("Previewing bet for user {UserId} - Event: {EventId}, Team: {Team}, Amount: {Amount:C}", 
                    userId, betDto.EventId, betDto.SelectedTeam, betDto.Amount);
                
                var validationResult = await _betService.ValidateBetAsync(userId, betDto);
                
                var eventDetail = await _eventService.GetEventByIdAsync(betDto.EventId);
                
                var preview = new
                {
                    IsValid = validationResult.IsValid,
                    Errors = validationResult.Errors,
                    
                    Amount = betDto.Amount,
                    CurrentOdds = validationResult.CurrentOdds,
                    PotentialWin = betDto.Amount * validationResult.CurrentOdds,
                    PotentialProfit = (betDto.Amount * validationResult.CurrentOdds) - betDto.Amount,
                    
                    CurrentBalance = validationResult.UserBalance,
                    BalanceAfterBet = validationResult.UserBalance - betDto.Amount,
                    
                    EventName = eventDetail?.Name,
                    EventDate = eventDetail?.EventDate,
                    SelectedTeam = betDto.SelectedTeam,
                    
                      PreviewedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    
                    Message = validationResult.IsValid 
                        ? "Bet preview is valid. You can proceed to create this bet."
                        : "Bet preview shows validation errors. Please correct them before proceeding."
                };
                
                return preview;
            }, "PreviewBet");
        }
        

        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBetHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] BetStatus? status = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] bool sortDescending = true)
        {
            return await HandleActionAsync(async () =>
            {
                var userId = GetCurrentUserId();
                
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;
                
                _logger.LogInformation("Fetching bet history for user {UserId} - Page: {Page}, PageSize: {PageSize}, " +
                    "Status: {Status}, SortBy: {SortBy}, SortDesc: {SortDescending}", 
                    userId, page, pageSize, status, sortBy, sortDescending);
                
                var filter = new BetFilterDto { Status = status };
                var allBets = await _betService.GetUserBetsAsync(userId, filter);
                
                var orderedBets = sortBy?.ToLower() switch
                {
                    "amount" => sortDescending 
                        ? allBets.OrderByDescending(b => b.Amount)
                        : allBets.OrderBy(b => b.Amount),
                    "odds" => sortDescending 
                        ? allBets.OrderByDescending(b => b.Odds)
                        : allBets.OrderBy(b => b.Odds),
                    "potentialwin" => sortDescending 
                        ? allBets.OrderByDescending(b => b.PotentialWin)
                        : allBets.OrderBy(b => b.PotentialWin),
                    "eventdate" => sortDescending 
                        ? allBets.OrderByDescending(b => b.EventDate)
                        : allBets.OrderBy(b => b.EventDate),
                    _ => sortDescending 
                        ? allBets.OrderByDescending(b => b.CreatedAt)
                        : allBets.OrderBy(b => b.CreatedAt)
                };
                
                var totalItems = orderedBets.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagedBets = orderedBets
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                var result = new
                {
                    Data = pagedBets,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                        HasPreviousPage = page > 1,
                        HasNextPage = page < totalPages
                    },
                    Sorting = new
                    {
                        SortBy = sortBy,
                        SortDescending = sortDescending
                    }
                };
                
                return result;
            }, "GetBetHistory");
        }
    }
}
