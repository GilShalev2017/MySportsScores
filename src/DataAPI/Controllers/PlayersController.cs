using Common.DTOs;
using DataAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;
        private readonly ILogger<PlayersController> _logger;

        public PlayersController(IPlayerService playerService, ILogger<PlayersController> logger)
        {
            _playerService = playerService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPlayer(int id)
        {
            try
            {
                var player = await _playerService.GetPlayerByIdAsync(id);
                if (player == null)
                    return NotFound();

                return Ok(player);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting player {PlayerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/stats")]
        public async Task<ActionResult<PlayerStatsDto>> GetPlayerStats(int id)
        {
            try
            {
                var stats = await _playerService.GetPlayerStatsAsync(id);
                if (stats == null)
                    return NotFound();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for player {PlayerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("team/{teamId}")]
        public async Task<ActionResult<object>> GetPlayersByTeam(int teamId)
        {
            try
            {
                var players = await _playerService.GetPlayersByTeamAsync(teamId);
                return Ok(players);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting players for team {TeamId}", teamId);
                return StatusCode(500, "Internal server error");
            }
        }
    }

}
