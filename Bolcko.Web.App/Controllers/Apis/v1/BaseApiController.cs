using Blocko.Services.DTOs.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BaseApiController : ControllerBase
    {
        protected IActionResult OkResponse<T>(T data, string message = "Success")
        {
            return Ok(ApiResponse<T>.Ok(data, message));
        }

        protected IActionResult OkResponse(string message = "Success")
        {
            return Ok(ApiResponse.Ok(message));
        }

        protected IActionResult ErrorResponse(string message, System.Collections.Generic.List<string> errors = null, int statusCode = 400)
        {
            return StatusCode(statusCode, ApiResponse.Error(message, errors));
        }
    }
}
