using Microsoft.AspNetCore.Mvc;

namespace Aurora.Api.Controllers;

/// <summary>
/// Health check controller for API status monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Gets the health status of the API
    /// </summary>
    /// <returns>API health information</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        var response = new
        {
            Status = "Healthy",
            Message = "Aurora API is running successfully!",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets test data to verify API connectivity and data serialization
    /// </summary>
    /// <returns>Sample data for frontend testing</returns>
    [HttpGet("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetTest()
    {
        var response = new
        {
            Message = "Hello from Aurora backend!",
            Data = new[]
            {
                new { Id = 1, Name = "Test Event 1", Date = DateTime.UtcNow.AddDays(1) },
                new { Id = 2, Name = "Test Event 2", Date = DateTime.UtcNow.AddDays(2) },
                new { Id = 3, Name = "Test Event 3", Date = DateTime.UtcNow.AddDays(3) }
            },
            RequestInfo = new
            {
                Method = HttpContext.Request.Method,
                Path = HttpContext.Request.Path,
                Timestamp = DateTime.UtcNow
            }
        };

        return Ok(response);
    }
}