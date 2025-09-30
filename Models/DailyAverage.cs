using System;

namespace EnergyBackend.Models
{
    public class DailyAverage
    {
        public int Id { get; set; }                   // Primary Key
        public string Source { get; set; } = "External"; // Source of data
        public DateOnly Date { get; set; }            // The day (e.g., 2025-09-29)
        public decimal AveragePrice { get; set; }     // Average price for that day
    }
}
