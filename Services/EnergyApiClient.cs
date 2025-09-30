using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EnergyBackend.Services
{
    public record ExternalReading(DateTime timestamp, decimal price);

    public class EnergyApiClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private string? _cachedToken;

        public EnergyApiClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;

            var baseUrl = _config["ExternalApi:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                _http.BaseAddress = new Uri(baseUrl!);
            }
        }

        public async Task<List<ExternalReading>> GetReadingsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            // Ensure bearer token
            var token = await GetTokenAsync(ct);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Build MCP request (assignment says date_from/date_to)
            var fromStr = Uri.EscapeDataString(fromUtc.ToString("O"));
            var toStr   = Uri.EscapeDataString(toUtc.ToString("O"));
            var path = $"/MCP?date_from={fromStr}&date_to={toStr}";

            using var res = await _http.GetAsync(path, ct);
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var list = new List<ExternalReading>();
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in root.EnumerateArray())
                    TryAddRow(row, list);
            }
            else
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var row in prop.Value.EnumerateArray())
                            TryAddRow(row, list);
                    }
                }
            }

            return list;
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(_cachedToken))
                return _cachedToken!;

            var user = _config["ExternalApi:Username"];
            var pass = _config["ExternalApi:Password"];
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                throw new InvalidOperationException("Missing ExternalApi:Username/Password in appsettings.json");

            // Form-encoded login (this is what the API expects)
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("username", user!),
                new KeyValuePair<string,string>("password", pass!)
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, "/token") { Content = form };
            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try common token field names
            string? token =
                root.TryGetProperty("access_token", out var a) ? a.GetString() :
                root.TryGetProperty("token", out var b) ? b.GetString() :
                null;

            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Could not parse token from /token response.");

            _cachedToken = token!;
            return _cachedToken!;
        }

        private static void TryAddRow(JsonElement row, List<ExternalReading> list)
        {
            // Accept a few possible field names
            DateTime? t = TryGetDate(row, "timestamp") ?? TryGetDate(row, "time") ?? TryGetDate(row, "date");
            decimal? v = TryGetDecimal(row, "mcp") ?? TryGetDecimal(row, "value") ?? TryGetDecimal(row, "price");

            if (t is not null && v is not null)
                list.Add(new ExternalReading(DateTime.SpecifyKind(t.Value, DateTimeKind.Utc), v.Value));
        }

        private static DateTime? TryGetDate(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p)) return null;

            if (p.ValueKind == JsonValueKind.String && DateTime.TryParse(p.GetString(), out var dt))
                return dt;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var unix))
                return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

            return null;
        }

        private static decimal? TryGetDecimal(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p)) return null;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d)) return d;
            if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var s)) return s;

            return null;
        }
    }
}
