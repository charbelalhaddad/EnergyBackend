# EnergyBackend – Evidence & Screenshots

This folder shows proof that the assignment was completed end-to-end:
- Auth against the **external StellarBlue API** (`/token`, `/MCP`)
- Our **local Web API** running with Swagger
- Build & run logs

---

## A) External API (StellarBlue)

**1. Login request to `/token`**  
![Token Request](ExternalAPI_Token_Request.jpg)

**2. Token response (access token)**  
![Token Response](ExternalAPI_Token_Response.jpg)

**3. Successful response & validation info**  
![Token Success / Validation](ExternalAPI_Token_SuccessValidation.jpg)

**4. MCP request with date range**  
![MCP Request](ExternalAPI_MCP_Request.jpg)

**5. MCP response with data (example date range)**  
![MCP Success](ExternalAPI_MCP_Success.jpg)

> Example request used:  
> `https://assignment.stellarblue.eu/MCP?date_from=2024-01-01&date_to=2024-01-10`

---

## B) Local Backend (EnergyBackend)

**1. Swagger – Ingestion endpoint visible**  
![Ingestion Endpoint](Backend_Swagger_Ingestion.jpg)

**2. Swagger – Averages (GetData) endpoint**  
![Averages Endpoint](Backend_Swagger_Averages.jpg)

**3. Swagger – WeatherForecast (default template)**  
![WeatherForecast](Backend_Swagger_WeatherForecast.jpg)

**4. Swagger – WeatherForecast response example**  
![WeatherForecast Response](Backend_Swagger_WeatherForecast_Response.jpg)

---

## C) Build & Run (PowerShell)

**Build succeeded**  
![Build](PowerShell_Build.jpg)
