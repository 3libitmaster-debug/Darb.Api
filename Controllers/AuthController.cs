using Darb.Api.DTOs.AuthDtos;
using Darb.Api.DTOs.Base;
using Darb.Api.Interfaces;
using Darb.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Darb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Handles user login for both Passengers and Companies.
        /// </summary>
        [HttpPost("login")]
        [SwaggerOperation(Summary = "User Login", Description = "Authenticates users (Admin/Company/Passenger) and returns a JWT token.")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Validate Model attributes
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto.FailureResponse("Invalid input data.", ModelState));
            }

            // Call the service which now returns a ResponseDto
            var result = await _authService.Login(dto);

            // If the login failed (Invalid, Inactive, Unsubscribed, etc.)
            if (!result.Success)
            {
                // We can differentiate status codes if needed
                if (result.Message.Contains("Invalid")) return Unauthorized(result);
                return BadRequest(result);
            }

            // Return 200 OK with the token inside the result object
            return Ok(result);
        }

        /// <summary>
        /// Registers a new passenger in the system.
        /// </summary>
        [HttpPost("register-passenger")]
        [SwaggerOperation(
            Summary = "Register New Passenger",
            Description = "Creates a new passenger account. Checks for duplicate email and phone before saving."
        )]
        public async Task<IActionResult> RegisterPassenger([FromBody] RegisterPassengerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("Validation failed.", ModelState));

            // Business Check: Duplicate Email
            if (await _authService.EmailExists(dto.Email!))
                return BadRequest(ResponseDto.FailureResponse("Email is already registered."));

            // Business Check: Duplicate Phone
            if (await _authService.PhoneExists(dto.Phone!))
                return BadRequest(ResponseDto.FailureResponse("Phone number is already registered."));

            // Call registration service
            var result = await _authService.RegisterPassenger(dto);

            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Registers a new company with document uploads and subscription details.
        /// </summary>
        [HttpPost("register-company")]
        [SwaggerOperation(
            Summary = "Register New Company",
            Description = "Registers a new transport company. Requires uploading legal documents and licenses via form-data."
        )]
        public async Task<IActionResult> RegisterCompany([FromForm] RegisterCompanyDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("Validation failed.", ModelState));

            // Business Check: Duplicate Email
            if (await _authService.EmailExists(request.Email!))
                return BadRequest(ResponseDto.FailureResponse("Email is already in use by another company."));

            // Call registration service (handles files and transactions)
            var result = await _authService.RegisterCompanyAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}