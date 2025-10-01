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

        /// <summary>
        /// Pull external readings for [fromUtc, toUtc), store raw readings, (re)compute daily averages.
        /// Returns (#newReadingsInserted, #daysWithAverageUpdated).
        /// </summary>
        public async Task<(int inserted, int daysUpdated)> IngestAsync(
            DateTime fromUtc,
            DateTime toUtc,
            string source = "External",
            CancellationToken ct = default)
        {
            // ---- guard rails -------------------------------------------------
            if (fromUtc.Kind == DateTimeKind.Unspecified) fromUtc = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
            if (toUtc.Kind == DateTimeKind.Unspecified)   toUtc   = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);

            if (fromUtc >= toUtc)
                throw new ArgumentException("fromUtc must be earlier than toUtc.");

            // (Optional) cap range to protect the external API and your DB
            var maxDays = 60;
            if ((toUtc - fromUtc).TotalDays > maxDays)
                throw new ArgumentException($"Requested range is too large. Max {maxDays} days.");

            // ---- fetch from external API (translate HTTP failures) ----------
            // NEW
            IReadOnlyList<ExternalReading> readings;
            try
            {
            readings = await _client.GetReadingsAsync(fromUtc, toUtc, ct);
            }

            catch (HttpRequestException httpEx)
            {
                // Wrap to a more meaningful exception for the controller
                throw new ExternalApiException(
                    "Failed to fetch readings from the external API.",
                    httpEx);
            }

            // ---- dedupe + insert raw readings efficiently -------------------
            // Pull existing timestamps in the window once; avoid N+1 AnyAsync.
            var existingTimestamps = await _db.Readings
                .Where(r => r.Source == source && r.TimestampUtc >= fromUtc && r.TimestampUtc < toUtc)
                .Select(r => r.TimestampUtc)
                .ToListAsync(ct);

            var seen = new HashSet<DateTime>(existingTimestamps);

            var toInsert = new List<EnergyReading>();
            foreach (var r in readings)
            {
                var tsUtc = r.timestamp.Kind == DateTimeKind.Utc
                    ? r.timestamp
                    : DateTime.SpecifyKind(r.timestamp, DateTimeKind.Utc);

                if (!seen.Contains(tsUtc))
                {
                    toInsert.Add(new EnergyReading
                    {
                        Source       = source,
                        TimestampUtc = tsUtc,
                        
                    });
                    seen.Add(tsUtc);
                }
            }

            if (toInsert.Count > 0)
                await _db.Readings.AddRangeAsync(toInsert, ct);

            var inserted = await _db.SaveChangesAsync(ct);

            // ---- recompute daily averages for the affected days -------------
            // Build the set of dates (UTC) impacted by the request.
            var datesInRange = await _db.Readings
                .Where(r => r.Source == source && r.TimestampUtc >= fromUtc && r.TimestampUtc < toUtc)
                .Select(r => DateOnly.FromDateTime(r.TimestampUtc))
                .Distinct()
                .ToListAsync(ct);

            // Compute fresh averages directly from the raw readings
            var fresh = await _db.Readings
                .Where(r => r.Source == source && r.TimestampUtc >= fromUtc && r.TimestampUtc < toUtc)
                .GroupBy(r => DateOnly.FromDateTime(r.TimestampUtc))
                .Select(g => new { Date = g.Key, Avg = (decimal)g.Average(x => (double)x.Price) })
                .ToListAsync(ct);

            // Upsert: fetch existing rows for those dates once,
            // then update or add.
            var existingAverages = await _db.DailyAverages
                .Where(a => a.Source == source && datesInRange.Contains(a.Date))
                .ToListAsync(ct);

            var map = existingAverages.ToDictionary(a => a.Date);

            foreach (var f in fresh)
            {
                if (map.TryGetValue(f.Date, out var row))
                {
                    row.AveragePrice = Math.Round(f.Avg, 4);
                }
                else
                {
                    _db.DailyAverages.Add(new DailyAverage
                    {
                        Date         = f.Date,
                        Source       = source,
                        AveragePrice = Math.Round(f.Avg, 4)
                    });
                }
            }

            var daysUpdated = await _db.SaveChangesAsync(ct);
            return (inserted, daysUpdated);
        }
    }

    /// <summary>
    /// Used to signal external API failures distinctly from other errors.
    /// </summary>
    public sealed class ExternalApiException : Exception
    {
        public ExternalApiException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
