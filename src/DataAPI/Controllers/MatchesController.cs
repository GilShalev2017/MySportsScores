using Common.DTOs;
using DataAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly ILogger<MatchesController> _logger;

        public MatchesController(IMatchService matchService, ILogger<MatchesController> logger)
        {
            _matchService = matchService;
            _logger = logger;
        }

        [HttpGet("live")]
        public async Task<ActionResult<IEnumerable<LiveMatchDto>>> GetLiveMatches()
        {
            try
            {
                var matches = await _matchService.GetLiveMatchesAsync();
                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live matches");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LiveMatchDto>> GetMatch(int id)
        {
            try
            {
                var match = await _matchService.GetMatchByIdAsync(id);
                if (match == null)
                    return NotFound();

                return Ok(match);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting match {MatchId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("league/{leagueId}")]
        public async Task<ActionResult<IEnumerable<LiveMatchDto>>> GetMatchesByLeague(int leagueId)
        {
            try
            {
                var matches = await _matchService.GetMatchesByLeagueAsync(leagueId);
                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matches for league {LeagueId}", leagueId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("team/{teamId}")]
        public async Task<ActionResult<IEnumerable<LiveMatchDto>>> GetMatchesByTeam(int teamId)
        {
            try
            {
                var matches = await _matchService.GetMatchesByTeamAsync(teamId);
                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matches for team {TeamId}", teamId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{matchId}/events")]
        public async Task<ActionResult<object>> GetMatchEvents(int matchId)
        {
            try
            {
                var events = await _matchService.GetMatchEventsAsync(matchId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for match {MatchId}", matchId);
                return StatusCode(500, "Internal server error");
            }
        }
    }

}
