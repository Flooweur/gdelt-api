using GdeltApi.Models;
using GdeltApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GdeltApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GdeltController : ControllerBase
{
    private readonly IGdeltService _gdeltService;
    private readonly ILogger<GdeltController> _logger;

    public GdeltController(IGdeltService gdeltService, ILogger<GdeltController> logger)
    {
        _gdeltService = gdeltService;
        _logger = logger;
    }

    /// <summary>
    /// Search for articles matching the provided filters
    /// </summary>
    [HttpPost("article_search")]
    public async Task<ActionResult<ArticleSearchResponse>> ArticleSearch([FromBody] FiltersDto filters)
    {
        try
        {
            var articles = await _gdeltService.ArticleSearchAsync(filters);
            return Ok(new ArticleSearchResponse { Articles = articles });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in article_search");
            return StatusCode(500, new { error = "An error occurred while processing the request" });
        }
    }

    /// <summary>
    /// Get a timeline of news coverage matching the filters
    /// </summary>
    /// <param name="mode">Timeline mode: timelinevol, timelinevolraw, timelinetone, timelinelang, timelinesourcecountry</param>
    /// <param name="filters">Filter parameters</param>
    [HttpPost("timeline_search")]
    public async Task<ActionResult<TimelineSearchResponse>> TimelineSearch(
        [FromQuery] string mode,
        [FromBody] FiltersDto filters)
    {
        try
        {
            var timeline = await _gdeltService.TimelineSearchAsync(mode, filters);
            return Ok(new TimelineSearchResponse { Data = timeline });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in timeline_search");
            return StatusCode(500, new { error = "An error occurred while processing the request" });
        }
    }

    /// <summary>
    /// Get articles from the last hour, optionally filtered
    /// </summary>
    [HttpPost("get_last_hour")]
    public async Task<ActionResult<ArticleSearchResponse>> GetLastHour([FromBody] FiltersDto? filters = null)
    {
        try
        {
            var articles = await _gdeltService.GetLastHourAsync(filters);
            return Ok(new ArticleSearchResponse { Articles = articles });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in get_last_hour");
            return StatusCode(500, new { error = "An error occurred while processing the request" });
        }
    }
}
