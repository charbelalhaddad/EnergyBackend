using EnergyBackend.Data;
using EnergyBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Services
{
    public class IngestionService
    {
        private readonly AppDbContext _db;
        private readonly EnergyApiClient _client;

        public IngestionService(AppDbContext db, EnergyApiClient client)
        {
            _db = db;
            _client = client;
        }

        public async Task<int> IngestAsync(DateTime fromUtc, DateTime toUtc, string source = "External", CancellationToken ct = default)
        {
            var readings = await _client.GetReadingsAsync(fromUtc, toUtc, ct);

            foreach (var r in readings)
            {
                var ts = DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc);
                bool exists = await _db.Readings.AnyAsync(x => x.TimestampUtc == ts && x.Source == source, ct);
                if (!exists)
                {
                    _db.Readings.Add(new EnergyReading
                    {
                        Source = source,
                        TimestampUtc = ts,
                        Price = r.price
                    });
                }
            }

            int inserted = await _db.SaveChangesAsync(ct);

            var grouped = await _db.Readings
                .Where(x => x.TimestampUtc >= fromUtc && x.TimestampUtc < toUtc && x.Source == source)
                .GroupBy(x => DateOnly.FromDateTime(x.TimestampUtc))
                .Select(g => new { Date = g.Key, Avg = (decimal)g.Average(x => (double)x.Price) })
                .ToListAsync(ct);

            foreach (var g in grouped)
            {
                var row = await _db.DailyAverages
                    .FirstOrDefaultAsync(a => a.Date == g.Date && a.Source == source, ct);

                if (row is null)
                {
                    _db.DailyAverages.Add(new DailyAverage
                    {
                        Date = g.Date,
                        Source = source,
                        AveragePrice = Math.Round(g.Avg, 4)
                    });
                }
                else
                {
                    row.AveragePrice = Math.Round(g.Avg, 4);
                }
            }

            await _db.SaveChangesAsync(ct);
            return inserted;
        }
    }
}
