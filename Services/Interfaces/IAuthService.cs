using Darb.Api.DTOs.AuthDtos;
using Darb.Api.DTOs.auth;
using Darb.Api.DTOs.Base;
using Darb.Api.Models; // Ensure this points to where ResponseDto is defined

namespace Darb.Api.Services.Interfaces
{
    public interface IAuthService
    {
    
        // Validates if an email is already registered in the system.
        Task<bool> EmailExists(string email);

        // Validates if a phone number is already registered for any passenger.
        Task<bool> PhoneExists(string phone);

        /// Handles the registration logic for a new passenger and returns a unified response.
        Task<ResponseDto> RegisterPassenger(RegisterPassengerDto dto);

        /// Handles the registration logic for a company, including document uploads and subscriptions.
        Task<ResponseDto> RegisterCompanyAsync(RegisterCompanyDto request);

        /// Authenticates the Account and returns a response containing the JWT token or error details.
        Task<ResponseDto> Login(LoginDto dto);

        /// Sends an OTP to the specified email for verification.
        Task<ResponseDto> SendOtpAsync(SendOtpDto dto);

        /// Verifies the provided OTP for the specified email.
        Task<ResponseDto> VerifyOtpAsync(VerifyOtpDto dto);

        Task<ResponseDto> ForgetPasswordAsync(ForgetPasswordDto dto);
        Task<ResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
    }
}