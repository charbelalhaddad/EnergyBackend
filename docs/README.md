#  EnergyBackend – External API Ingestion & Processing

This project demonstrates how to securely connect to an external energy provider's API (https://assignment.stellarblue.eu), retrieve hourly electricity price readings, and store and process them in a local database. It includes token-based authentication, deduplication logic, daily average computation, and Swagger-exposed endpoints for ingestion.



##  Project Structure

- **Controllers/**
  - `IngestionController.cs`: Exposes `/api/ingest` for ingesting data.
- **Services/**
  - `EnergyApiClient.cs`: Authenticates and fetches external data.
  - `IngestionService.cs`: Saves readings and calculates daily averages.
- **Models/**
  - `EnergyReading.cs`: Raw hourly data model.
  - `DailyAverage.cs`: Daily average data model.
- **Data/**
  - `AppDbContext.cs`: Entity Framework database context.



##  Features Implemented

###  Authentication (Token-Based)
- Sends a POST request to `/token` with form-urlencoded credentials (`username` + `password`).
- Caches the token for 30 minutes.
- Handles HTTP errors and invalid tokens gracefully.

 `docs/LogInStellarBlue.png`  
 `docs/LogInResponse1StellarBlue.png`  
 `docs/LogInResponse2StellarBlue.png`



###  MCP Data Fetching

- Retrieves hourly energy prices between selected `date_from` and `date_to` values using a `GET` request to `/MCP`.
- Authorization token is passed as a Bearer header.

 `docs/MCPRequestStellarBlue.png`  
 `docs/MCPResponse1.png`  
 `docs/MCPResponse2.png`  
 `docs/PowerShell1.png`  
 `docs/PowerShell2.png`



###  Data Ingestion and Storage

- Parses external readings and saves them to the local SQL database.
- Filters out duplicates based on timestamp and source.
- Automatically computes daily averages for all affected dates.

📷 `docs/Ingestion1.png`  
📷 `docs/Ingestion2.png`  
📷 `docs/Readings1.png`, `Readings2.png`, `Readings3.png`, `Readings4.png`  
📷 `docs/Averages1.png`, `Average2.png`, `Average3.png`



##  How to Run

### 1. **Setup Configuration (`appsettings.json`)**

```json
"ExternalApi": {
  "BaseUrl": "https://assignment.stellarblue.eu",
  "Username": "stellarblue",
  "Password": "st3!!@r_b1u3"
}
