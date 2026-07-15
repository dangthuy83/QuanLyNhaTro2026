using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyNhaTro.Controllers;

[AllowAnonymous, ApiController]
public sealed class HealthController(IDbConnection db) : ControllerBase
{
    [HttpGet("/healthz")]
    public async Task<IActionResult> Get()
    {
        try
        {
            _ = await db.ExecuteScalarAsync<int>("SELECT 1");
            return Ok(new { status = "healthy" });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
        }
    }
}
