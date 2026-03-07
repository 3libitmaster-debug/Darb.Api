using Darb.Api.DTOs.AuthDtos;
using Darb.Api.DTOs.Base;
using Darb.Api.Models; // Ensure this points to where ResponseDto is defined

namespace Darb.Api.Interfaces
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

        /// Authenticates the user and returns a response containing the JWT token or error details.
        Task<ResponseDto> Login(LoginDto dto);
    }
}