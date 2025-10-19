using DataAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<object>> Search([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Query parameter is required");

                var results = await _searchService.SearchAsync(query);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for: {Query}", query);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("events")]
        public async Task<ActionResult<object>> SearchEvents([FromQuery] string eventType, [FromQuery] int? matchId)
        {
            try
            {
                var results = await _searchService.SearchEventsAsync(eventType, matchId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                return StatusCode(500, "Internal server error");
            }
        }
    }

}
