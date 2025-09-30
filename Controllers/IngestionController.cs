using EnergyBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnergyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngestionController : ControllerBase
    {
        private readonly IngestionService _service;
        public IngestionController(IngestionService service) => _service = service;

        // POST /api/ingestion?fromUtc=2025-09-20T00:00:00Z&toUtc=2025-09-29T00:00:00Z
        [HttpPost]
        public async Task<IActionResult> Ingest([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
        {
            var to = toUtc ?? DateTime.UtcNow;
            var from = fromUtc ?? to.AddDays(-7);
            var inserted = await _service.IngestAsync(from, to);
            return Ok(new { inserted, from, to });
        }
    }
}
