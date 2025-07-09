using AdminPortal.Server.Models;
using AdminPortal.Server.Services;
using AdminPortal.Server.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Xml.Linq;

namespace AdminPortal.Server.Controllers
{
    [ApiController]
    [Route("v1/companies")]
    public class CompanyController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ICompanyService _companyService;
        private readonly ICookieService _cookieService;
        private readonly IMappingService _mappingService;
        private readonly ILogger<CompanyController> _logger;
        private readonly string _connString;

        public CompanyController(
            IConfiguration config,
            ICompanyService companyService,
            ICookieService cookieService,
            IMappingService mappingService,
            ILogger<CompanyController> logger)
        {
            _config = config;
            _companyService = companyService;
            _cookieService = cookieService;
            _mappingService = mappingService;
            _logger = logger;
            _connString = _config.GetConnectionString("TCS")!;
        }

        // update full company name in records, does NOT change DB key...
        [HttpPut]
        [Route("{newName}")]
        [Authorize]
        public async Task<IActionResult> UpdateCompany(string newName)
        {
            // ensure valid proposed name...
            // may be redundant given null/empty username return 405 (ie: doesnt reach this endpoint)...
            if (string.IsNullOrEmpty(newName))
            {
                _logger.LogWarning("New company name must be valid to update.");
                return BadRequest(new { message = "Invalid company name provided, update failed." });
            }

            // ensure current company key is valid in cookies...
            string? companyKey = Request.Cookies["company"];
            if (string.IsNullOrEmpty(companyKey))
            {
                _logger.LogWarning("Company name was not found in cookies.");
                return BadRequest(new { message = "Current company name was not found in cookies." });
            }

            // update company records...
            (bool success, string message) = await _companyService.UpdateCompanyAsync(companyKey, newName);
            if (success)
            {
                // fetch company and module mappings...
                IDictionary<string, string> companies = await _mappingService.GetCompaniesAsync();
                Response.Cookies.Append("company_mapping", JsonSerializer.Serialize(companies), _cookieService.AccessOptions());

                return Ok(new { message = message });
            }
            else
            {
                // handle failed update...
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"Attempt to update non-existent company '{companyKey}'.");
                    return NotFound(new { message = $"Company with name '{companyKey}' not found." }); // 404
                }
                else if (message.Contains("Company name already exists", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Conflict: New company '{newName}' already exists.");
                    return Conflict(new { message = message }); // 409
                }
                else
                {
                    _logger.LogError($"Error updating company '{companyKey}': {message}");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = message }); // 500
                }
            }
        }
    }
}
