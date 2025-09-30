using EnergyBackend.Data;
using EnergyBackend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for our API client (even in demo we still register it)
builder.Services.AddHttpClient<EnergyApiClient>(client =>
{
    // If not set, default to "demo" so everything works immediately
    var baseUrl = builder.Configuration["ExternalApi:BaseUrl"] ?? "demo";
    if (!string.IsNullOrWhiteSpace(baseUrl) && !baseUrl.Equals("demo", StringComparison.OrdinalIgnoreCase))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});

// Business service
builder.Services.AddScoped<IngestionService>();

var app = builder.Build();

// Auto-migrate DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
