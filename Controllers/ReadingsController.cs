using EnergyBackend.Data;
using EnergyBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadingsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReadingsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EnergyReading>>> Get([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
        {
            var q = _db.Readings.AsQueryable();
            if (fromUtc.HasValue) q = q.Where(x => x.TimestampUtc >= fromUtc.Value);
            if (toUtc.HasValue)   q = q.Where(x => x.TimestampUtc <  toUtc.Value);
            return Ok(await q.OrderBy(x => x.TimestampUtc).ToListAsync());
        }
    }
}

