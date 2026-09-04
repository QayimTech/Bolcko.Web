using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Blocko.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bolcko.Web.App.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("health")]
    [Route("api/health")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class HealthController : ControllerBase
    {
        private static readonly DateTime AppStartTimeUtc = DateTime.UtcNow;
        private readonly BlockoDbContext _dbContext;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            BlockoDbContext dbContext,
            IHostEnvironment environment,
            ILogger<HealthController> logger)
        {
            _dbContext = dbContext;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// AWS ALB Primary Liveness Probe (Fast, lightweight, < 5ms)
        /// Accessible via: /health, /healthz, /ping, /api/health
        /// </summary>
        [HttpGet("")]
        [HttpGet("/healthz")]
        [HttpGet("/ping")]
        public IActionResult GetLiveness()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "Bolcko.Web",
                timestampUtc = DateTime.UtcNow,
                environment = _environment.EnvironmentName,
                uptime = (DateTime.UtcNow - AppStartTimeUtc).ToString(@"d\.hh\:mm\:ss")
            });
        }

        /// <summary>
        /// Deep Readiness Probe for ECS, RDS PostgreSQL, and System Diagnostics
        /// Accessible via: /health/ready, /health/live
        /// </summary>
        [HttpGet("ready")]
        [HttpGet("live")]
        public async Task<IActionResult> GetReadiness()
        {
            var sw = Stopwatch.StartNew();
            bool isDbHealthy = false;
            string? dbError = null;

            try
            {
                // Lightweight database connectivity check (PostgreSQL)
                isDbHealthy = await _dbContext.Database.CanConnectAsync();
                sw.Stop();
            }
            catch (Exception ex)
            {
                sw.Stop();
                isDbHealthy = false;
                dbError = ex.Message;
                _logger.LogError(ex, "Health check database connectivity probe failed.");
            }

            var allocatedMemoryMb = Math.Round(GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0), 2);
            var uptime = (DateTime.UtcNow - AppStartTimeUtc).ToString(@"d\.hh\:mm\:ss");

            var result = new
            {
                status = isDbHealthy ? "Healthy" : "Degraded",
                service = "Bolcko.Web",
                environment = _environment.EnvironmentName,
                timestampUtc = DateTime.UtcNow,
                uptime,
                database = new
                {
                    status = isDbHealthy ? "Connected" : "Disconnected",
                    latencyMs = sw.ElapsedMilliseconds,
                    error = dbError
                },
                system = new
                {
                    allocatedMemoryMb,
                    processorCount = Environment.ProcessorCount
                }
            };

            if (!isDbHealthy)
            {
                return StatusCode(503, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Comprehensive Diagnostic Endpoint for DevOps and CloudVests
        /// Accessible via: /health/detail
        /// </summary>
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailedHealth()
        {
            var sw = Stopwatch.StartNew();
            bool isDbHealthy = false;
            int? productsCount = null;
            string? dbError = null;

            try
            {
                productsCount = await _dbContext.Products.AsNoTracking().CountAsync();
                isDbHealthy = true;
                sw.Stop();
            }
            catch (Exception ex)
            {
                sw.Stop();
                isDbHealthy = false;
                dbError = ex.Message;
            }

            var allocatedMemoryMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);

            return Ok(new
            {
                status = isDbHealthy ? "Healthy" : "Degraded",
                application = "Bolcko Construction & E-Commerce Platform",
                version = "1.0.0",
                environment = _environment.EnvironmentName,
                timestampUtc = DateTime.UtcNow,
                appStartTimeUtc = AppStartTimeUtc,
                uptime = (DateTime.UtcNow - AppStartTimeUtc).ToString(@"d\.hh\:mm\:ss"),
                cloudTarget = "AWS ECS + RDS PostgreSQL",
                components = new
                {
                    database = new
                    {
                        status = isDbHealthy ? "Connected" : "Failed",
                        type = "PostgreSQL",
                        queryLatencyMs = sw.ElapsedMilliseconds,
                        productsSampleCount = productsCount,
                        error = dbError
                    },
                    memory = new
                    {
                        allocatedMb = allocatedMemoryMb
                    },
                    runtime = new
                    {
                        framework = ".NET 8.0",
                        os = Environment.OSVersion.ToString(),
                        machineName = Environment.MachineName
                    }
                }
            });
        }
    }
}
