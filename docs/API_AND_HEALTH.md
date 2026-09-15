# AWS ALB Health Checks & API Reference

## 1. AWS ALB Health Probe Design

AWS Application Load Balancers (ALB) require fast, zero-overhead HTTP responses to verify target health. Bolcko implements specialized lightweight health endpoints:

### Endpoints
- **`GET /api/health`** (Recommended Target Group Path):
  - Returns `{"status":"Healthy"}` with `200 OK`.
  - Zero database queries, zero Razor view rendering.
  - Bypassed by security/traffic tracking middlewares to prevent DB log saturation.

- **`GET /health/ready`** (Readiness Probe):
  - Verifies PostgreSQL database connectivity using `DbContext.Database.CanConnectAsync()`.
  - Verifies memory consumption (< 1.5 GB).
  - Returns `{"status":"Ready","database":"Connected"}`.

- **`GET /health/detail`** (Diagnostic Probe):
  - Returns application uptime, allocated memory, active product count, and database latency.

---

## 2. AWS ALB Target Group Configuration
In the AWS Console:
- **Protocol:** HTTP
- **Path:** `/api/health`
- **Port:** Traffic port (80 or 8080)
- **Healthy threshold:** 2
- **Unhealthy threshold:** 2
- **Timeout:** 5 seconds
- **Interval:** 15 seconds
- **Success codes:** 200\n