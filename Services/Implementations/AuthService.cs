using Darb.Api.DTOs.AuthDtos;
using Darb.Api.DTOs.Base;
using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Darb.Api.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using Darb.Api.DTOs.auth;

namespace Darb.Api.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _ImageService;
        private readonly ITokenService _TokenService;
        private readonly IEmailService _EmailService;
        private readonly IMemoryCache _Cache;

        public AuthService(ApplicationDbContext context, IImageService ImageService, ITokenService TokenService, 
            IEmailService EmailService, IMemoryCache Cache)
        {
            _context = context;
            _ImageService = ImageService;
            _TokenService = TokenService;
            _EmailService = EmailService;
            _Cache = Cache;
        }

        #region Login Endpoint Logic
        /// <summary>
        /// Authenticates Users and checks company subscription validity.
        /// </summary>
        public async Task<ResponseDto> Login(LoginDto dto)
        {
            var base64Password = SecurityHelper.ConvertToBase64(dto.Password!);

            // Fetch User with related Customer or Company profiles
            var User = await _context.Users
                .Include(u => u.Customer)
                .Include(u => u.Company).ThenInclude(c => c!.CompanySubscription)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Password == base64Password);

            if (User == null)
                return ResponseDto.FailureResponse("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            // Logic for Company: Validate the latest subscription
            if (User.Role == AccountRoles.Company && User.Company != null)
            {
                var latestSub = User.Company.CompanySubscription?
                    .OrderByDescending(s => s.CompanySubscriptionId)
                    .FirstOrDefault();

                if (latestSub == null || latestSub.ExpiryDate <= DateHelper.GetYemenTime()) 
                {
                    if (User.IsActive)
                    {
                        User.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                    return ResponseDto.FailureResponse("انتهى الاشتراك يرجى التجديد .");
                }
            }

            // Check user activation status
            if (!User.IsActive)
            {
                string statusMessage = User.Role == AccountRoles.Company ? "حسابك لا يزال قيد المراجعة." : "هذا الحساب غير نشط.";
                return ResponseDto.FailureResponse(statusMessage);
            }

            // Success: Generate and return the Token
            var token = _TokenService.GenerateJwtToken(User);
            return ResponseDto.SuccessResponse("تم تسجيل الدخول بنجاح.", new { Token = token });
        }
        #endregion

        #region Register Customer Endpoint Logic
        /// <summary>
        /// Registers a new customer using a resilient transaction strategy.
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
                    var newAccount = new User
                    {
                        Email = dto.Email,
                        Password = SecurityHelper.ConvertToBase64(dto.Password!),
                        Role = AccountRoles.Customer,
                        IsActive = true,
                        JoinDate = DateHelper.GetYemenTime(),
                    };

                    _context.Users.Add(newAccount);
                    await _context.SaveChangesAsync();

                    // 2. Create the linked Customer profile
                    var newPassenger = new Customer
                    {
                        UserId = newAccount.UserId,
                        FullName = dto.FullName,
                        Phone = dto.Phone,
                        Address = dto.Address,
                        NationalId = dto.NationalId,
                        DateOfBirth = dto.DateOfBirth
                    };

                    _context.Customers.Add(newPassenger);
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
                    var logoPath = await _ImageService.SaveImageAsync(request.Logo!, "Transport company logos");
                    var licensePath = await _ImageService.SaveImageAsync(request.License!, "Transport company licenses");
                    var paymentPath = await _ImageService.SaveImageAsync(request.PaymentSlip!, "Subscription payment receipts");

                    // Validate that all required documents are successfully uploaded
                    if (logoPath == null || licensePath == null || paymentPath == null)
                        return ResponseDto.FailureResponse("فشل في رفع الوثائق المطلوبة.");

                    // Define Yemen Time (UTC+3) to ensure consistency across all date fields
                    var yemenNow = DateTime.UtcNow.AddHours(3);

                    // --- STEP 2: Create User Identity User ---
                    var User = new User
                    {
                        Email = request.Email,
                        Password = SecurityHelper.ConvertToBase64(request.Password),
                        Role = AccountRoles.Company,
                        IsActive = false, // Companies remain inactive until admin approval
                        JoinDate = yemenNow
                    };
                    _context.Users.Add(User);
                    await _context.SaveChangesAsync();

                    // --- STEP 3: Create Detailed Company Profile ---
                    var company = new Company
                    {
                        UserId = User.UserId, // Linking the profile to the created User
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
                    var subscription = new CompanySubscription
                    {
                        CompanyId = company.CompanyId,
                        PlanType = request.PlanType,
                        SubscriptionDate = yemenNow,
                        ExpiryDate = expiryDate,
                        PaymentSlip = paymentPath
                    };

                    _context.CompanySubscription.Add(subscription);
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
            => await _context.Customers.AnyAsync(p => p.Phone == phone);

        #region OTP Logic
        public async Task<ResponseDto> SendOtpAsync(SendOtpDto dto)
        {
            // Check if email already exists
            if (await EmailExists(dto.Email))
                return ResponseDto.FailureResponse("هذا البريد الإلكتروني مسجل مسبقاً.");

            var otp = await _EmailService.SendOtpEmailAsync(dto.Email);
            if (string.IsNullOrEmpty(otp))
                return ResponseDto.FailureResponse("فشل إرسال البريد الإلكتروني. يرجى المحاولة لاحقاً.");

            // Store OTP in cache for 5 minutes
            _Cache.Set($"OTP_{dto.Email}", otp, TimeSpan.FromMinutes(5));

            return ResponseDto.SuccessResponse("تم إرسال رمز التحقق إلى بريدك الإلكتروني.");
        }

        public async Task<ResponseDto> VerifyOtpAsync(VerifyOtpDto dto)
        {
            if (_Cache.TryGetValue($"OTP_{dto.Email}", out string? storedOtp))
            {
                if (storedOtp == dto.OtpCode)
                {
                    _Cache.Remove($"OTP_{dto.Email}"); // Remove after successful verification
                    return await Task.FromResult(ResponseDto.SuccessResponse("تم التحقق من الرمز بنجاح."));
                }
            }

            return await Task.FromResult(ResponseDto.FailureResponse("الرمز غير صحيح أو انتهت صلاحيته."));
        }
        #endregion
        #endregion

        #region Reset Password Logic
        
        public async Task<ResponseDto> ForgetPasswordAsync(ForgetPasswordDto dto)
        {
            // 1. التحقق من وجود الحساب (عكس التسجيل: هنا يجب أن يكون الحساب موجوداً)
            if (!await EmailExists(dto.Email))
                return ResponseDto.FailureResponse("هذا البريد الإلكتروني غير مسجل لدينا.");

            // 2. استدعاء خدمة البريد الإلكتروني بنفس الطريقة المعتمدة في كود التسجيل لديك
            var otp = await _EmailService.SendOtpEmailAsync(dto.Email);

            // 3. التحقق من نجاح إرسال الإيميل وتوليد الرمز
            if (string.IsNullOrEmpty(otp))
                return ResponseDto.FailureResponse("فشل إرسال البريد الإلكتروني. يرجى المحاولة لاحقاً.");

            // 4. تخزين الرمز في الكاش لمدة 5 دقائق ببادئة خاصة لعملية إعادة التعيين
            _Cache.Set($"RESET_OTP_{dto.Email}", otp, TimeSpan.FromMinutes(5));

            return ResponseDto.SuccessResponse("تم إرسال رمز التحقق إلى بريدك الإلكتروني.");
        }

    
        public async Task<ResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            // 1. التحقق من وجود الرمز وصحته داخل الكاش
            if (!_Cache.TryGetValue($"RESET_OTP_{dto.Email}", out string? storedOtp) || storedOtp != dto.OtpCode)
            {
                return ResponseDto.FailureResponse("الرمز غير صحيح أو انتهت صلاحيته.");
            }

            // 2. جلب الحساب لتعديله
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return ResponseDto.FailureResponse("حدث خطأ، الحساب لم يعد متاحاً.");

            // 3. تشفير كلمة المرور الجديدة بنفس أسلوب مشروعك (Base64)
            user.Password = SecurityHelper.ConvertToBase64(dto.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // 4. مسح الرمز من الكاش بعد نجاح العملية لضمان الأمان
            _Cache.Remove($"RESET_OTP_{dto.Email}");

            return ResponseDto.SuccessResponse("تمت إعادة تعيين كلمة المرور بنجاح.");
        }
        #endregion
    }
}