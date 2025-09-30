using EnergyBackend.Data;
using EnergyBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AveragesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AveragesController(AppDbContext db) => _db = db;

        // GET /api/averages?from=2025-09-20&to=2025-09-29
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyAverage>>> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var q = _db.DailyAverages.AsQueryable();
            if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
            if (to.HasValue)   q = q.Where(x => x.Date <= to.Value);
            return Ok(await q.OrderBy(x => x.Date).ToListAsync());
        }
    }
}
