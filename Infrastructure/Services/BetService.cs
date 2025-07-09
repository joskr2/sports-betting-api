using Microsoft.EntityFrameworkCore;
using SportsBetting.Api.Core.DTOs;
using SportsBetting.Api.Core.Entities;
using SportsBetting.Api.Core.Interfaces;
using SportsBetting.Api.Infrastructure.Data;

namespace SportsBetting.Api.Infrastructure.Services
{

    public class BetService : IBetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BetService> _logger;
        private readonly IConfiguration _configuration;
        
        public BetService(
            ApplicationDbContext context, 
            ILogger<BetService> logger,
            IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        

        public async Task<BetResponseDto?> CreateBetAsync(int userId, CreateBetDto betDto)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    _logger.LogInformation("Creating bet for user {UserId} on event {EventId}", userId, betDto.EventId);
                    
                    var validationResult = await ValidateBetAsync(userId, betDto);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Bet validation failed for user {UserId}: {Errors}", 
                            userId, string.Join(", ", validationResult.Errors));
                        return null;
                    }
                    
                    var user = await _context.Users
                        .Where(u => u.Id == userId)
                        .FirstOrDefaultAsync();
                    
                    var eventEntity = await _context.Events
                        .Where(e => e.Id == betDto.EventId)
                        .FirstOrDefaultAsync();
                
                    if (user != null)
                    {
                        _context.Entry(user).Reload();
                    }
                    if (eventEntity != null)
                    {
                        _context.Entry(eventEntity).Reload();
                    }
                    
                    if (user == null || eventEntity == null)
                    {
                        _logger.LogError("User or event not found during bet creation");
                        return null;
                    }
                    
                    if (!user.DeductBalance(betDto.Amount))
                    {
                        _logger.LogWarning("Insufficient balance for user {UserId}. Balance: {Balance}, Amount: {Amount}", 
                            userId, user.Balance, betDto.Amount);
                        return null;
                    }
                    
                    var currentOdds = eventEntity.GetOddsForTeam(betDto.SelectedTeam);
                    
                    var bet = new Bet
                    {
                        UserId = userId,
                        EventId = betDto.EventId,
                        SelectedTeam = betDto.SelectedTeam,
                        Amount = betDto.Amount,
                        Odds = currentOdds,
                        Status = BetStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.Bets.Add(bet);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Bet created successfully with ID: {BetId} for user {UserId}", bet.Id, userId);
                    
                    return new BetResponseDto
                    {
                        Id = bet.Id,
                        EventId = bet.EventId,
                        EventName = eventEntity.Name,
                        SelectedTeam = bet.SelectedTeam,
                        Amount = bet.Amount,
                        Odds = bet.Odds,
                        PotentialWin = bet.PotentialWin,
                        Status = bet.Status.ToString(),
                        CreatedAt = bet.CreatedAt,
                        EventStatus = eventEntity.Status.ToString(),
                        EventDate = eventEntity.EventDate,
                        CanBeCancelled = bet.CanBeCancelled(),
                        TimeUntilEvent = GetTimeUntilEvent(eventEntity.EventDate)
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating bet for user {UserId} on event {EventId}", userId, betDto.EventId);
                    throw;
                }
            });
        }
        

        public async Task<IEnumerable<BetResponseDto>> GetUserBetsAsync(int userId, BetFilterDto? filter = null)
        {
            try
            {
                _logger.LogInformation("Fetching bets for user {UserId}", userId);
                
                var query = _context.Bets
                    .Include(b => b.Event)
                    .Where(b => b.UserId == userId);
                
                if (filter != null)
                {
                    if (filter.Status.HasValue)
                    {
                        query = query.Where(b => b.Status == filter.Status.Value);
                    }
                    
                    if (filter.FromDate.HasValue)
                    {
                        query = query.Where(b => b.CreatedAt >= filter.FromDate.Value);
                    }
                    
                    if (filter.ToDate.HasValue)
                    {
                        query = query.Where(b => b.CreatedAt <= filter.ToDate.Value);
                    }
                    
                    if (filter.OnlyActive)
                    {
                        query = query.Where(b => b.Status == BetStatus.Active);
                    }
                }
                
                var bets = await query
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
                
                var betDtos = bets.Select(b => new BetResponseDto
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    EventName = b.Event.Name,
                    SelectedTeam = b.SelectedTeam,
                    Amount = b.Amount,
                    Odds = b.Odds,
                    PotentialWin = b.PotentialWin,
                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt,
                    EventStatus = b.Event.Status.ToString(),
                    EventDate = b.Event.EventDate,
                    CanBeCancelled = b.CanBeCancelled(),
                    TimeUntilEvent = GetTimeUntilEvent(b.Event.EventDate)
                });
                
                _logger.LogInformation("Retrieved {Count} bets for user {UserId}", betDtos.Count(), userId);
                
                return betDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bets for user {UserId}", userId);
                throw;
            }
        }
        

        public async Task<UserBetStatsDto> GetUserBetStatsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Calculating bet statistics for user {UserId}", userId);
                
                var userBets = await _context.Bets
                    .Where(b => b.UserId == userId)
                    .ToListAsync();
                
                if (!userBets.Any())
                {
                    return new UserBetStatsDto();
                }
                
                var stats = new UserBetStatsDto
                {
                    TotalBets = userBets.Count,
                    ActiveBets = userBets.Count(b => b.Status == BetStatus.Active),
                    WonBets = userBets.Count(b => b.Status == BetStatus.Won),
                    LostBets = userBets.Count(b => b.Status == BetStatus.Lost),
                    TotalAmountBet = userBets.Sum(b => b.Amount),
                    TotalWinnings = userBets.Where(b => b.Status == BetStatus.Won).Sum(b => b.PotentialWin),
                    CurrentPotentialWin = userBets.Where(b => b.Status == BetStatus.Active).Sum(b => b.PotentialWin),
                    AverageBetAmount = userBets.Average(b => b.Amount)
                };
                
                var completedBets = stats.WonBets + stats.LostBets;
                stats.WinRate = completedBets > 0 ? (double)stats.WonBets / completedBets * 100 : 0;
                
                _logger.LogInformation("Statistics calculated for user {UserId}: {TotalBets} bets, {WinRate}% win rate", 
                    userId, stats.TotalBets, stats.WinRate.ToString("F1"));
                
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating statistics for user {UserId}", userId);
                throw;
            }
        }
        

        public async Task<BetValidationResult> ValidateBetAsync(int userId, CreateBetDto betDto)
        {
            var result = new BetValidationResult();
            
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    result.Errors.Add("User not found");
                    return result;
                }
                
                result.UserBalance = user.Balance;
                
                var eventEntity = await _context.Events.FindAsync(betDto.EventId);
                if (eventEntity == null)
                {
                    result.Errors.Add("Event not found");
                    return result;
                }
                
                if (!eventEntity.IsAvailableForBetting())
                {
                    result.Errors.Add("Event is not available for betting");
                    return result;
                }
                
                if (!eventEntity.IsValidTeam(betDto.SelectedTeam))
                {
                    result.Errors.Add($"Invalid team selection: {betDto.SelectedTeam}");
                    return result;
                }
                
                if (!user.HasSufficientBalance(betDto.Amount))
                {
                    result.Errors.Add($"Insufficient balance. Available: {user.Balance:C}, Required: {betDto.Amount:C}");
                    return result;
                }
                
                var maxBetAmount = _configuration.GetValue<decimal>("ApiSettings:MaxBetAmount", 10000);
                var minBetAmount = _configuration.GetValue<decimal>("ApiSettings:MinBetAmount", 1);
                
                if (betDto.Amount > maxBetAmount)
                {
                    result.Errors.Add($"Bet amount exceeds maximum allowed: {maxBetAmount:C}");
                    return result;
                }
                
                if (betDto.Amount < minBetAmount)
                {
                    result.Errors.Add($"Bet amount is below minimum required: {minBetAmount:C}");
                    return result;
                }
                
                result.IsValid = true;
                result.CurrentOdds = eventEntity.GetOddsForTeam(betDto.SelectedTeam);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating bet for user {UserId}", userId);
                result.Errors.Add("Validation error occurred");
                return result;
            }
        }
        

        public async Task<bool> CancelBetAsync(int userId, int betId)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    _logger.LogInformation("Attempting to cancel bet {BetId} for user {UserId}", betId, userId);
                    
                    var bet = await _context.Bets
                        .Include(b => b.Event)
                        .Include(b => b.User)
                        .FirstOrDefaultAsync(b => b.Id == betId && b.UserId == userId);
                    
                    if (bet == null)
                    {
                        _logger.LogWarning("Bet not found or user mismatch: {BetId}, {UserId}", betId, userId);
                        return false;
                    }
                    
                    if (!bet.CanBeCancelled())
                    {
                        _logger.LogWarning("Bet cannot be cancelled: {BetId}", betId);
                        return false;
                    }

                    var refundAmount = bet.ProcessRefund();
                    bet.User.AddToBalance(refundAmount);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Bet {BetId} cancelled successfully, refunded {Amount:C}", betId, refundAmount);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error cancelling bet {BetId} for user {UserId}", betId, userId);
                    throw;
                }
            });
        }
        

        public async Task<BetSettlementResultDto> SettleEventBetsAsync(int eventId, string winnerTeam)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                _logger.LogInformation("Settling bets for event {EventId}, winner: {WinnerTeam}", eventId, winnerTeam);
                
                var eventEntity = await _context.Events
                    .Include(e => e.Bets.Where(b => b.Status == BetStatus.Active))
                        .ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(e => e.Id == eventId);
                
                if (eventEntity == null)
                {
                    throw new ArgumentException($"Event {eventId} not found");
                }
                
                if (!eventEntity.IsValidTeam(winnerTeam))
                {
                    throw new ArgumentException($"Invalid winner team: {winnerTeam}");
                }
                
                var result = new BetSettlementResultDto
                {
                    EventId = eventId,
                    WinnerTeam = winnerTeam,
                    SettledAt = DateTime.UtcNow
                };
                
                foreach (var bet in eventEntity.Bets)
                {
                    if (bet.SelectedTeam.Equals(winnerTeam, StringComparison.OrdinalIgnoreCase))
                    {
                        var winnings = bet.MarkAsWon();
                        bet.User.AddToBalance(winnings);
                        result.WinningBets++;
                        result.TotalPayouts += winnings;
                    }
                    else
                    {
                        bet.MarkAsLost();
                        result.LosingBets++;
                    }
                }
                
                    result.TotalBetsProcessed = result.WinningBets + result.LosingBets;
                    
                    eventEntity.FinishEvent();
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Event {EventId} settled: {TotalBets} bets processed, {Payouts:C} in payouts", 
                        eventId, result.TotalBetsProcessed, result.TotalPayouts);
                    
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error settling bets for event {EventId}", eventId);
                    throw;
                }
            });
        }
        
        private string GetTimeUntilEvent(DateTime eventDate)
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
