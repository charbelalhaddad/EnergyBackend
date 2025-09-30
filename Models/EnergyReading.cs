using System;

namespace EnergyBackend.Models
{
    public class EnergyReading
    {
        public int Id { get; set; }                   // Primary Key (auto-increment ID)
        public string Source { get; set; } = "External"; // Source of data (default = External API)
        public DateTime TimestampUtc { get; set; }    // When the reading was taken (UTC time)
        public decimal Price { get; set; }            // Energy price value
    }
}
