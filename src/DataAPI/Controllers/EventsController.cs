using Common.Models;
using DataAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ISportEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(ISportEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        [HttpGet("match/{matchId}")]
        public async Task<ActionResult<IEnumerable<SportEvent>>> GetMatchEvents(int matchId)
        {
            try
            {
                var events = await _eventService.GetMatchEventsAsync(matchId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for match {MatchId}", matchId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<SportEvent>>> GetRecentEvents([FromQuery] int limit = 100)
        {
            try
            {
                var events = await _eventService.GetRecentEventsAsync(limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent events");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("type/{eventType}")]
        public async Task<ActionResult<IEnumerable<SportEvent>>> GetEventsByType(string eventType)
        {
            if (!Enum.TryParse<EventType>(eventType, true, out var type))
            {
                return BadRequest($"Invalid event type: {eventType}");
            }

            try
            {
                var events = await _eventService.GetEventsByTypeAsync(type);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events of type {EventType}", eventType);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
