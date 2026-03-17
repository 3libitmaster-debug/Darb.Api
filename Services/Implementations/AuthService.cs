using Darb.Api.DTOs.AuthDtos;
using Darb.Api.DTOs.Base;
using Darb.Api.Helpers;
using Darb.Api.Interfaces;
using Darb.Api.Models;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Darb.Api.Models.Enums;

namespace Darb.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _ImageService;
        private readonly ITokenService _TokenService;

        public AuthService(ApplicationDbContext context, IImageService ImageService, ITokenService TokenService)
        {
            _context = context;
            _ImageService = ImageService;
            _TokenService = TokenService;
        }

        #region Login Endpoint Logic
        /// <summary>
        /// Authenticates users and checks company subscription validity.
        /// </summary>
        public async Task<ResponseDto> Login(LoginDto dto)
        {
            var base64Password = SecurityHelper.ConvertToBase64(dto.Password);

            // Fetch user with related Passenger or Company profiles
            var user = await _context.Users
                .Include(u => u.Passenger)
                .Include(u => u.Company).ThenInclude(c => c.Subscription)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Password == base64Password);

            if (user == null)
                return ResponseDto.FailureResponse("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            // Logic for Company: Validate the latest subscription
            if (user.Role == UserRoles.Company && user.Company != null)
            {
                var latestSub = user.Company.Subscription?
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefault();

                if (latestSub == null || latestSub.ExpiryDate <= DateHelper.GetYemenTime()) 
                {
                    if (user.IsActive)
                    {
                        user.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                    return ResponseDto.FailureResponse("انتهى الاشتراك يرجى التجديد .");
                }
            }

            // Check account activation status
            if (!user.IsActive)
            {
                string statusMessage = user.Role == UserRoles.Company ? "حسابك لا يزال قيد المراجعة." : "هذا الحساب غير نشط.";
                return ResponseDto.FailureResponse(statusMessage);
            }

            // Success: Generate and return the Token
            var token = _TokenService.GenerateJwtToken(user);
            return ResponseDto.SuccessResponse("تم تسجيل الدخول بنجاح.", new { Token = token });
        }
        #endregion

        #region Register Passenger Endpoint Logic
        /// <summary>
        /// Registers a new passenger using a resilient transaction strategy.
        /// </summary>
        public async Task<ResponseDto> RegisterPassenger(RegisterPassengerDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Create the base User
                    var newUser = new User
                    {
                        Email = dto.Email,
                        Password = SecurityHelper.ConvertToBase64(dto.Password),
                        Role = UserRoles.Passenger,
                        IsActive = true,
                        JoinDate = DateHelper.GetYemenTime(),
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 2. Create the linked Passenger profile
                    var newPassenger = new Passenger
                    {
                        UserId = newUser.UserId,
                        FullName = dto.FullName,
                        Phone = dto.Phone,
                        Address = dto.Address,
                        NationalId = dto.NationalId,
                        DateOfBirth = dto.DateOfBirth
                    };

                    _context.Passengers.Add(newPassenger);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return ResponseDto.SuccessResponse("تم تسجيل حسابك بنجاح.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"RegisterPassenger Error: {ex.Message}");
                    return ResponseDto.FailureResponse("حدث خطأ أثناء تسجيل الحساب .");
                }
            });
        }
        #endregion

        #region Register Company Endpoint Logic
        /// <summary>
        /// Registers a new company, handles multiple file uploads, and initiates a subscription.
        /// </summary>
        public async Task<ResponseDto> RegisterCompanyAsync(RegisterCompanyDto request)
        {
            // Using ExecutionStrategy for high availability and resilient database connections
            // Using ExecutionStrategy for high availability and resilient database connections
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // --- STEP 1: Process and optimize image uploads (Converted to WebP internally) ---
                    var logoPath = await _ImageService.SaveImageAsync(request.Logo, "Transport company logos");
                    var licensePath = await _ImageService.SaveImageAsync(request.License, "Transport company licenses");
                    var paymentPath = await _ImageService.SaveImageAsync(request.PaymentSlip, "Subscription payment receipts");

                    // Validate that all required documents are successfully uploaded
                    if (logoPath == null || licensePath == null || paymentPath == null)
                        return ResponseDto.FailureResponse("فشل في رفع الوثائق المطلوبة.");

                    // Define Yemen Time (UTC+3) to ensure consistency across all date fields
                    var yemenNow = DateTime.UtcNow.AddHours(3);

                    // --- STEP 2: Create User Identity Account ---
                    var user = new User
                    {
                        Email = request.Email,
                        Password = SecurityHelper.ConvertToBase64(request.Password),
                        Role = UserRoles.Company,
                        IsActive = false, // Companies remain inactive until admin approval
                        JoinDate = yemenNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // --- STEP 3: Create Detailed Company Profile ---
                    var company = new Company
                    {
                        UserId = user.UserId, // Linking the profile to the created user
                        Name = request.Name,
                        Address = request.Address,
                        Logo = logoPath,
                        License = licensePath
                    };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    // --- STEP 4: Calculate Subscription Expiry Date ---
                    // Adding 3 hours to ensure expiry logic also follows Yemen time
                    DateTime expiryDate = request.PlanType == SubscriptionPlans.Monthly
                        ? yemenNow.AddDays(30)
                        : yemenNow.AddYears(1);

                    // --- STEP 5: Initialize Subscription Record ---
                    var subscription = new Subscription
                    {
                        CompanyId = company.CompanyId,
                        PlanType = request.PlanType,
                        SubscriptionDate = yemenNow,
                        ExpiryDate = expiryDate,
                        PaymentSlip = paymentPath
                    };

                    _context.Subscriptions.Add(subscription);
                    await _context.SaveChangesAsync();

                    // Commit transaction if all steps succeeded
                    await transaction.CommitAsync();

                    return ResponseDto.SuccessResponse("تم تقديم طلب تسجيل الشركة بنجاح.");
                }
                catch (Exception ex)
                {
                    // Rollback all database changes if any step fails
                    await transaction.RollbackAsync();
                    Console.WriteLine($"RegisterCompany Error: {ex.Message}");
                    return ResponseDto.FailureResponse("حدث خطأ أثناء تسجيل بيانات الشركة.");
                }
            });
        }
        #endregion

        #region Validation Helpers Logic
        public async Task<bool> EmailExists(string email)
            => await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<bool> PhoneExists(string phone)
            => await _context.Passengers.AnyAsync(p => p.Phone == phone);
        #endregion
    }
}