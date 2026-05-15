using Darb.Api.DTOs.AuthDtos;
using Darb.Api.DTOs.auth;
using Darb.Api.DTOs.Base;
using Darb.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Darb.Api.Services.Interfaces;

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


      
        [HttpPost("login")]
        [SwaggerOperation(Summary = "User Login", Description = "Authenticates users (Admin/Company/Passenger) and returns a JWT token.")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseDto.FailureResponse("Invalid input data.", ModelState));
            }

            var result = await _authService.Login(dto);

            if (!result.Success)
            {
                if (result.Message.Contains("Invalid")) return Unauthorized(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

  
        [HttpPost("send-registration-otp")]
        [SwaggerOperation(Summary = "Send Registration OTP", Description = "Sends Registration OTP to the user's email.")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("Validation failed.", ModelState));

            var result = await _authService.SendOtpAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("verify-registration-otp")]
        [SwaggerOperation(Summary = "Verify Registration OTP", Description = "Checks if the provided OTP matches the one sent to the email.")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("Validation failed.", ModelState));

            var result = await _authService.VerifyOtpAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

     
        [HttpPost("register/passengers")]
        [SwaggerOperation(
            Summary = "Register New Passenger",
            Description = "Creates a new passenger account. Checks for duplicate email and phone before saving."
        )]
        public async Task<IActionResult> RegisterPassenger([FromBody] RegisterPassengerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("Validation failed.", ModelState));

            if (await _authService.EmailExists(dto.Email!))
                return BadRequest(ResponseDto.FailureResponse("Email is already registered."));

            if (await _authService.PhoneExists(dto.Phone!))
                return BadRequest(ResponseDto.FailureResponse("Phone number is already registered."));

            var result = await _authService.RegisterPassenger(dto);

            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result);
        }


        [HttpPost("register/companies")]
        [SwaggerOperation(
            Summary = "Register New Company",
            Description = "Registers a new transport company. Requires uploading legal documents and licenses via form-data."
        )]
        public async Task<IActionResult> RegisterCompany([FromForm] RegisterCompanyDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("Validation failed.", ModelState));

            if (await _authService.EmailExists(request.Email!))
                return BadRequest(ResponseDto.FailureResponse("Email is already in use by another company."));

            var result = await _authService.RegisterCompanyAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("forget-password")]
        [SwaggerOperation(Summary = "Forget Password", Description = "Sends an OTP to the user's email to initiate password reset.")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("«·»Ì«‰«  «·„œŒ·… €Ì— ’ÕÌÕ….", ModelState));

            var result = await _authService.ForgetPasswordAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Reset Password", Description = "Verifies the OTP and updates the user's password.")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ResponseDto.FailureResponse("«·»Ì«‰«  «·„œŒ·… €Ì— ’ÕÌÕ….", ModelState));

            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

    }
}