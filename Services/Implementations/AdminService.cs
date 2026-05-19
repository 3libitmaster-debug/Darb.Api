using Darb.Api.DTOs.admin.Company;
using Darb.Api.DTOs.admin.Customers;
using Darb.Api.DTOs.adminDtos.Advertisement;
using Darb.Api.DTOs.adminDtos.Bank;
using Darb.Api.DTOs.adminDtos.City;
using Darb.Api.DTOs.adminDtos.Governorate;
using Darb.Api.DTOs.Base;
using Darb.Api.Enums;
using Darb.Api.Helpers;
using Darb.Api.Models;
using Darb.Api.Models.Enums;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Interfaces;
using darbWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace Darb.Api.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IRepository<Governorate> _govRepo;
        private readonly IRepository<City> _cityRepo;
        private readonly IRepository<Advertisement> _adRepo;
        private readonly IImageService _imageService;
        private readonly ApplicationDbContext _context;
        private readonly string _baseUrl;
        private readonly IEmailService _emailService;

        public AdminService(IRepository<Governorate> govRepo, IRepository<City> cityRepo, IRepository<Advertisement> adRepo, IImageService imageService, ApplicationDbContext context, IOptions<ApiSettings> apiOptions, IEmailService emailService)
        {
            _govRepo = govRepo;
            _cityRepo = cityRepo;
            _adRepo = adRepo;
            _imageService = imageService;
            _context = context;
            _baseUrl = apiOptions.Value.BaseUrl ?? string.Empty;
            _emailService = emailService;
        }

        #region Governorate Logic
        public async Task<ResponseDto> GetAllGovernoratesAsync()
        {
            // Fetching all governorates from the database
            var govs = await _govRepo.GetAllAsync();
            var dtos = govs.Select(g => new GovernorateReadDto { Id = g.GovernorateId, Name = g.Name ?? "N/A" }).ToList();
            return ResponseDto.SuccessResponse($"تم العثور على ({dtos.Count}) محافظة.", dtos);
        }

        public async Task<ResponseDto> GetGovernorateByIdAsync(int id)
        {
            // Fetching a single governorate by its unique identifier
            var gov = await _govRepo.GetByIdAsync(id);
            if (gov == null) return ResponseDto.FailureResponse("المحافظة غير موجودة.");

            return ResponseDto.SuccessResponse("تم استرجاع بيانات المحافظة.", new GovernorateReadDto { Id = gov.GovernorateId, Name = gov.Name ?? "" });
        }

        public async Task<ResponseDto> CreateGovernorateAsync(GovernorateCreateDto dto)
        {
            // Validating and persisting a new governorate record
            var gov = new Governorate { Name = dto.Name };
            await _govRepo.AddAsync(gov);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم إضافة المحافظة بنجاح.");
        }

        public async Task<ResponseDto> UpdateGovernorateAsync(int id, GovernorateCreateDto dto)
        {
            // Updating an existing governorate's information
            var gov = await _govRepo.GetByIdAsync(id);
            if (gov == null) return ResponseDto.FailureResponse("المحافظة غير موجودة لتحديثها.");

            gov.Name = dto.Name;
            _govRepo.Update(gov);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث بيانات المحافظة.");
        }

        public async Task<ResponseDto> DeleteGovernorateAsync(int id)
        {
            // Deleting a governorate record
            var gov = await _govRepo.GetByIdAsync(id);
            if (gov == null) return ResponseDto.FailureResponse("المحافظة غير موجودة لحذفها.");

            _govRepo.Delete(gov);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف المحافظة بنجاح.");
        }
        #endregion

        #region City Logic
        public async Task<ResponseDto> GetAllCitiesAsync()
        {
            // Fetching cities with their related governorates to display names correctly
            var cities = await _context.Cities.Include(c => c.Governorate).ToListAsync();
            var dtos = cities.Select(c => new CityReadDto
            {
                Id = c.CityId,
                Name = c.Name,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate?.Name ?? "N/A"
            }).ToList();
            return ResponseDto.SuccessResponse($"تم العثور على ({dtos.Count}) مدينة.", dtos);
        }

        public async Task<ResponseDto> GetCitiesByGovernorateIdAsync(int governorateId)
        {
            // Fetching cities for a specific governorate
            var cities = await _context.Cities.Where(c => c.GovernorateId == governorateId).Include(c => c.Governorate).ToListAsync();
            var dtos = cities.Select(c => new CityReadDto
            {
                Id = c.CityId,
                Name = c.Name,
                GovernorateId = c.GovernorateId,
                GovernorateName = c.Governorate?.Name ?? "N/A"
            }).ToList();
            return ResponseDto.SuccessResponse($"تم العثور على ({dtos.Count}) مدينة لهذه المحافظة.", dtos);
        }

        public async Task<ResponseDto> GetCityByIdAsync(int id)
        {
            // Fetching city details including its parent governorate
            var city = await _context.Cities.Include(c => c.Governorate).FirstOrDefaultAsync(c => c.CityId == id);
            if (city == null) return ResponseDto.FailureResponse("المدينة غير موجودة.");

            return ResponseDto.SuccessResponse("تم استرجاع بيانات المدينة.", new CityReadDto
            {
                Id = city.CityId,
                Name = city.Name,
                GovernorateId = city.GovernorateId,
                GovernorateName = city.Governorate?.Name ?? ""
            });
        }

        public async Task<ResponseDto> CreateCityAsync(CityCreateDto dto)
        {
            // Persisting a new city and linking it to a governorate via ID
            var city = new City { Name = dto.Name!, GovernorateId = dto.GovernorateId };
            await _cityRepo.AddAsync(city);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم إضافة المدينة بنجاح.");
        }

        public async Task<ResponseDto> UpdateCityAsync(int id, CityCreateDto dto)
        {
            // Modifying city details and ensuring the governorate link is updated
            var city = await _cityRepo.GetByIdAsync(id);
            if (city == null) return ResponseDto.FailureResponse("المدينة غير موجودة.");

            city.Name = dto.Name!;
            city.GovernorateId = dto.GovernorateId;
            _cityRepo.Update(city);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث المدينة بنجاح.");
        }

        public async Task<ResponseDto> DeleteCityAsync(int id)
        {
            // Removing city record from the database
            var city = await _cityRepo.GetByIdAsync(id);
            if (city == null) return ResponseDto.FailureResponse("المدينة غير موجودة.");

            _cityRepo.Delete(city);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف المدينة بنجاح.");
        }
        #endregion

        #region Advertisement Logic

        /// <summary>
        /// Retrieves all advertisements with related User information.
        /// </summary>
        public async Task<ResponseDto> GetAllAdvertisementsAsync()
        {
            // Fetch all advertisement records from the database using the repository
            var ads = await _adRepo.GetAllAsync();

            // Extract unique AccountIDs to fetch their emails in a single batch for better performance (Optimization)
            var AccountIds = ads.Select(a => a.UserId).Distinct().ToList();

            // Fetch User emails and store them in a dictionary for fast lookup
            var Users = await _context.Users
                .Where(u => AccountIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Email);

            // Map the database entities to Read-Only DTOs for the client side
            var dtos = ads.Select(a => new AdvertisementReadDto
            {
                AdvertisementID = a.AdvertisementID,
                AccountID = a.UserId,
                // Safely handle cases where a User might not exist in the dictionary
                Account_Email = Users.ContainsKey(a.UserId) ? Users[a.UserId] : "Unknown User",
                Title = a.AdsTitle,
                Description = a.Description,
                ImageUrl = !string.IsNullOrEmpty(a.Image) ? _baseUrl + a.Image : string.Empty,
                StartDateAds = a.StartDateAds,
                EndDateAds = a.EndDateAds,
                AdsStatus = a.AdsStatus,
                CreatedAt = a.AdsCreatedAt
            }).ToList();

            return ResponseDto.SuccessResponse($"Found ({dtos.Count}) advertisements successfully.", dtos);
        }

        /// <summary>
        /// Retrieves a specific advertisement by its unique identifier.
        /// </summary>
        public async Task<ResponseDto> GetAdvertisementByIdAsync(int id)
        {
            // Retrieve the advertisement by ID
            var ad = await _adRepo.GetByIdAsync(id);
            if (ad == null) return ResponseDto.FailureResponse("Advertisement not found.");

            // Fetch the owner/creator information
            var User = await _context.Users.FindAsync(ad.UserId);

            var dto = new AdvertisementReadDto
            {
                AdvertisementID = ad.AdvertisementID,
                AccountID = ad.UserId,
                Account_Email = User?.Email ?? "Unknown User",
                Title = ad.AdsTitle,
                Description = ad.Description,
                ImageUrl = !string.IsNullOrEmpty(ad.Image) ? _baseUrl + ad.Image : string.Empty,
                StartDateAds = ad.StartDateAds,
                EndDateAds = ad.EndDateAds,
                AdsStatus = ad.AdsStatus,
                CreatedAt = ad.AdsCreatedAt
            };

            return ResponseDto.SuccessResponse("Advertisement details retrieved successfully.", dto);
        }

        /// <summary>
        /// Creates a new advertisement and links it automatically to the authenticated Admin.
        /// </summary>
        public async Task<ResponseDto> CreateAdvertisementAsync(int adminId, AdvertisementCreateDto dto)
        {
            // Verify if the Admin ID from the token exists in the database to prevent Foreign Key constraints violation
            var adminExists = await _context.Users.AnyAsync(u => u.UserId == adminId);
            if (!adminExists) return ResponseDto.FailureResponse("Unauthorized: Admin User not found.");

            // Initialize the Advertisement entity with data from DTO and the Token
            var ad = new Advertisement
            {
                UserId = adminId,  // Automatic binding to the logged-in admin
                AdsTitle = dto.Title,
                Description = dto.Description,
                StartDateAds = dto.StartDateAds,
                EndDateAds = dto.EndDateAds,
                AdsStatus = dto.AdsStatus = AdsStatus.Active, // Default to active if status is null
                AdsCreatedAt = DateHelper.GetYemenTime() // Log the creation time in local timezone
            };

            // Handle image upload via the dedicated ImageService
            if (dto.ImageFile != null)
            {
                var imagePath = await _imageService.SaveImageAsync(dto.ImageFile, "AdvertisementsImages");
                if (!string.IsNullOrEmpty(imagePath)) ad.Image = imagePath;
            }

            // Save the new entity to the database
            await _adRepo.AddAsync(ad);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("Advertisement created successfully.");
        }

        /// <summary>
        /// Updates an existing advertisement's details selectively.
        /// </summary>
        public async Task<ResponseDto> UpdateAdvertisementAsync(int id, AdvertisementUpdateDto dto)
        {
            // Fetch the existing record
            var ad = await _adRepo.GetByIdAsync(id);
            if (ad == null) return ResponseDto.FailureResponse("Advertisement not found for update.");

            // Patch-style updates: only update fields that have provided values
            if (!string.IsNullOrEmpty(dto.Title)) ad.AdsTitle = dto.Title;
            if (!string.IsNullOrEmpty(dto.Description)) ad.Description = dto.Description;
            if (dto.StartDateAds.HasValue) ad.StartDateAds = dto.StartDateAds.Value;
            if (dto.EndDateAds.HasValue) ad.EndDateAds = dto.EndDateAds.Value;
            if (dto.AdsStatus.HasValue) ad.AdsStatus = dto.AdsStatus.Value;

            // Process image update via the centralized ImageService method
            if (dto.ImageFile != null)
            {
                var newImagePath = await _imageService.UpdateImageAsync(dto.ImageFile, ad.Image, "AdvertisementsImages");
                if (!string.IsNullOrEmpty(newImagePath)) ad.Image = newImagePath;
            }

            // Mark entity as modified and save changes
            _adRepo.Update(ad);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("Advertisement updated successfully.");
        }

        /// <summary>
        /// Deletes an advertisement permanently from the system.
        /// </summary>
        public async Task<ResponseDto> DeleteAdvertisementAsync(int id)
        {
            var ad = await _adRepo.GetByIdAsync(id);
            if (ad == null) return ResponseDto.FailureResponse("Advertisement not found or already deleted.");

            // Delete the image from disk before removing the record
            if (!string.IsNullOrEmpty(ad.Image))
            {
                _imageService.DeleteImage(ad.Image);
            }

            // Remove the record via repository
            _adRepo.Delete(ad);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("Advertisement deleted successfully.");
        }

        #endregion

        #region Bank Logic

        public async Task<ResponseDto> GetAllBanksAsync()
        {
            var banks = await _context.Banks.ToListAsync();
            var dtos = banks.Select(b => new BankReadDto
            {
                BankId = b.BankId,
                BankName = b.BankName,
                LogoUrl = !string.IsNullOrEmpty(b.LogoUrl) ? _baseUrl + b.LogoUrl : string.Empty
            }).ToList();
            return ResponseDto.SuccessResponse($"تم استرجاع ({dtos.Count}) بنك بنجاح.", dtos);
        }

        public async Task<ResponseDto> GetBankByIdAsync(int bankId)
        {
            var bank = await _context.Banks.FindAsync(bankId);
            if (bank == null) return ResponseDto.FailureResponse("البنك غير موجود.");

            var dto = new BankReadDto
            {
                BankId = bank.BankId,
                BankName = bank.BankName,
                LogoUrl = !string.IsNullOrEmpty(bank.LogoUrl) ? _baseUrl + bank.LogoUrl : string.Empty
            };
            return ResponseDto.SuccessResponse("تم استرجاع البنك بنجاح.", dto);
        }

        public async Task<ResponseDto> CreateBankAsync(BankCreateDto dto)
        {
            var bank = new Bank { BankName = dto.BankName, LogoUrl = string.Empty };

            if (dto.LogoFile != null)
            {
                var logoPath = await _imageService.SaveImageAsync(dto.LogoFile, "Banks logo");
                if (!string.IsNullOrEmpty(logoPath)) bank.LogoUrl = logoPath;
            }

            await _context.Banks.AddAsync(bank);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم اضافة البنك بنجاح.");
        }

        public async Task<ResponseDto> UpdateBankAsync(int bankId, BankUpdateDto dto)
        {
            var bank = await _context.Banks.FindAsync(bankId);
            if (bank == null) return ResponseDto.FailureResponse("البنك غير موجود.");

            if (!string.IsNullOrEmpty(dto.BankName)) bank.BankName = dto.BankName;

            if (dto.LogoFile != null)
            {
                var newLogoPath = await _imageService.UpdateImageAsync(dto.LogoFile, bank.LogoUrl, "Banks logo");
                if (!string.IsNullOrEmpty(newLogoPath)) bank.LogoUrl = newLogoPath;
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث البنك بنجاح.");
        }

        public async Task<ResponseDto> DeleteBankAsync(int bankId)
        {
            var bank = await _context.Banks.Include(b => b.BankAccounts).FirstOrDefaultAsync(b => b.BankId == bankId);
            if (bank == null) return ResponseDto.FailureResponse("البنك غير موجود.");
            if (bank.BankAccounts != null && bank.BankAccounts.Any())
                return ResponseDto.FailureResponse("لا يمكن حذف البنك لوجود حسابات مرتبطة به.");

            if (!string.IsNullOrEmpty(bank.LogoUrl))
            {
                _imageService.DeleteImage(bank.LogoUrl);
            }

            _context.Banks.Remove(bank);
            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف البنك بنجاح.");
        }

        #endregion

        #region Customer Management Logic

        public async Task<ResponseDto> GetAllCustomersAsync()
        {
            var customers = await _context.Customers
                .Include(c => c.User)
                .ToListAsync();

            var dtos = customers.Select(c => new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                UserId = c.UserId,
                Email = c.User?.Email ?? "N/A",
                FullName = c.FullName,
                DateOfBirth = c.DateOfBirth,
                Phone = c.Phone,
                Address = c.Address,
                NationalId = c.NationalId,
                JoinDate = c.User?.JoinDate ?? DateHelper.GetYemenTime(),
                IsActive = c.User?.IsActive ?? false
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع ({dtos.Count}) عميل بنجاح.", dtos);
        }

        public async Task<ResponseDto> GetCustomerByIdAsync(int id)
        {
            var c = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null) return ResponseDto.FailureResponse("العميل المطلوبة بياناته غير موجود.");

            var dto = new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                UserId = c.UserId,
                Email = c.User?.Email ?? "N/A",
                FullName = c.FullName,
                DateOfBirth = c.DateOfBirth,
                Phone = c.Phone,
                Address = c.Address,
                NationalId = c.NationalId,
                JoinDate = c.User?.JoinDate ?? DateHelper.GetYemenTime(),
                IsActive = c.User?.IsActive ?? false
            };
            return ResponseDto.SuccessResponse("تم جلب بيانات العميل بنجاح.", dto);
        }

        public async Task<ResponseDto> CreateCustomerAsync(CustomerCreateDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return ResponseDto.FailureResponse("البريد الإلكتروني مسجل مسبقاً في النظام.");

            var user = new User
            {
                Email = dto.Email,
                Password = dto.Password,
                Role = AccountRoles.Customer,
                JoinDate = DateHelper.GetYemenTime(),
                IsActive = true
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var customer = new Customer
            {
                UserId = user.UserId,
                FullName = dto.FullName,
                DateOfBirth = dto.DateOfBirth,
                Phone = dto.Phone,
                Address = dto.Address,
                NationalId = dto.NationalId
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم إنشاء حساب العميل بنجاح.");
        }

        public async Task<ResponseDto> UpdateCustomerAsync(int id, CustomerUpdateDto dto)
        {
            var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(x => x.CustomerId == id);
            if (customer == null || customer.User == null) return ResponseDto.FailureResponse("العميل غير موجود لتحديثه.");

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != customer.User.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                    return ResponseDto.FailureResponse("البريد الإلكتروني الجديد مستخدم بالفعل.");
                customer.User.Email = dto.Email;
            }
            if (!string.IsNullOrEmpty(dto.Password)) customer.User.Password = dto.Password;

            if (!string.IsNullOrEmpty(dto.FullName)) customer.FullName = dto.FullName;
            if (dto.DateOfBirth.HasValue) customer.DateOfBirth = dto.DateOfBirth.Value;
            if (!string.IsNullOrEmpty(dto.Phone)) customer.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Address)) customer.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.NationalId)) customer.NationalId = dto.NationalId;

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث بيانات العميل بنجاح.");
        }

        public async Task<ResponseDto> DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(x => x.CustomerId == id);
            if (customer == null) return ResponseDto.FailureResponse("العميل غير موجود لحذفه.");

            var associatedUser = customer.User;
            _context.Customers.Remove(customer);
            if (associatedUser != null) _context.Users.Remove(associatedUser);

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف حساب العميل نهائياً من النظام.");
        }

        #endregion

        #region Company Management Logic

        public async Task<ResponseDto> GetAllCompaniesAsync()
        {
            var companies = await _context.Companies
                .Include(c => c.User)
                .ToListAsync();

            var dtos = companies.Select(c => new CompanyResponseDto
            {
                CompanyId = c.CompanyId,
                UserId = c.UserId,
                Email = c.User?.Email ?? "N/A",
                Name = c.Name,
                Address = c.Address,
                LicenseUrl = !string.IsNullOrEmpty(c.License) ? _baseUrl + c.License : string.Empty,
                LogoUrl = !string.IsNullOrEmpty(c.Logo) ? _baseUrl + c.Logo : string.Empty,
                AverageRating = c.AverageRating,
                JoinDate = c.User?.JoinDate ?? DateHelper.GetYemenTime(),
                IsActive = c.User?.IsActive ?? false
            }).ToList();

            return ResponseDto.SuccessResponse($"تم استرجاع ({dtos.Count}) شركة نقل بنجاح.", dtos);
        }

        public async Task<ResponseDto> GetCompanyByIdAsync(int id)
        {
            var c = await _context.Companies.Include(c => c.User).FirstOrDefaultAsync(x => x.CompanyId == id);
            if (c == null) return ResponseDto.FailureResponse("شركة النقل المطلوبة بياناتها غير موجودة.");

            var dto = new CompanyResponseDto
            {
                CompanyId = c.CompanyId,
                UserId = c.UserId,
                Email = c.User?.Email ?? "N/A",
                Name = c.Name,
                Address = c.Address,
                LicenseUrl = !string.IsNullOrEmpty(c.License) ? _baseUrl + c.License : string.Empty,
                LogoUrl = !string.IsNullOrEmpty(c.Logo) ? _baseUrl + c.Logo : string.Empty,
                AverageRating = c.AverageRating,
                JoinDate = c.User?.JoinDate ?? DateHelper.GetYemenTime(),
                IsActive = c.User?.IsActive ?? false
            };
            return ResponseDto.SuccessResponse("تم جلب بيانات شركة النقل بنجاح.", dto);
        }

        public async Task<ResponseDto> CreateCompanyAsync(CompanyCreateDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return ResponseDto.FailureResponse("البريد الإلكتروني للشركة مسجل مسبقاً في النظام.");

            var user = new User
            {
                Email = dto.Email,
                Password = dto.Password,
                Role = AccountRoles.Company,
                JoinDate = DateHelper.GetYemenTime(),
                IsActive = dto.IsActive,
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            string logoPath = dto.LogoFile != null ? await _imageService.SaveImageAsync(dto.LogoFile, "CompaniesLogos") ?? string.Empty : string.Empty;
            string licensePath = dto.LicenseFile != null ? await _imageService.SaveImageAsync(dto.LicenseFile, "CompaniesLicenses") ?? string.Empty : string.Empty;

            var company = new Company
            {
                UserId = user.UserId,
                Name = dto.Name,
                Address = dto.Address,
                License = licensePath,
                Logo = logoPath,
                AverageRating = 0.0
            };
            await _context.Companies.AddAsync(company);
            await _context.SaveChangesAsync();

            return ResponseDto.SuccessResponse("تم إضافة شركة النقل بنجاح إلى النظام.");
        }

        public async Task<ResponseDto> UpdateCompanyAsync(int id, CompanyUpdateDto dto)
        {
            var company = await _context.Companies.Include(c => c.User).FirstOrDefaultAsync(x => x.CompanyId == id);
            if (company == null || company.User == null) return ResponseDto.FailureResponse("شركة النقل غير موجودة لتحديثها.");

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != company.User.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                    return ResponseDto.FailureResponse("البريد الإلكتروني الجديد مستخدم بالفعل من قبل حساب آخر.");
                company.User.Email = dto.Email;
            }
            if (!string.IsNullOrEmpty(dto.Password)) company.User.Password = dto.Password;
            if (dto.IsActive.HasValue) company.User.IsActive = dto.IsActive.Value;

            if (!string.IsNullOrEmpty(dto.Name)) company.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Address)) company.Address = dto.Address;

            if (dto.LogoFile != null)
            {
                var newLogoPath = await _imageService.UpdateImageAsync(dto.LogoFile, company.Logo, "CompaniesLogos");
                if (!string.IsNullOrEmpty(newLogoPath)) company.Logo = newLogoPath;
            }

            if (dto.LicenseFile != null)
            {
                var newLicensePath = await _imageService.UpdateImageAsync(dto.LicenseFile, company.License, "CompaniesLicenses");
                if (!string.IsNullOrEmpty(newLicensePath)) company.License = newLicensePath;
            }

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم تحديث بيانات شركة النقل بنجاح.");
        }

        public async Task<ResponseDto> DeleteCompanyAsync(int id)
        {
            var company = await _context.Companies
                .Include(c => c.User)
                .Include(c => c.Trips)
                .Include(c => c.Bus)
                .FirstOrDefaultAsync(x => x.CompanyId == id);

            if (company == null) return ResponseDto.FailureResponse("شركة النقل غير موجودة لحذفها.");

            if ((company.Trips != null && company.Trips.Any()) || (company.Bus != null && company.Bus.Any()))
            {
                return ResponseDto.FailureResponse("لا يمكن حذف الشركة، هناك حافلات أو رحلات نشطة تابعة لها بالنظام. يمكنك إلغاء تنشيطها بدلاً من ذلك.");
            }

            if (!string.IsNullOrEmpty(company.Logo)) _imageService.DeleteImage(company.Logo);
            if (!string.IsNullOrEmpty(company.License)) _imageService.DeleteImage(company.License);

            var associatedUser = company.User;
            _context.Companies.Remove(company);
            if (associatedUser != null) _context.Users.Remove(associatedUser);

            await _context.SaveChangesAsync();
            return ResponseDto.SuccessResponse("تم حذف شركة النقل وكافة حساباتها التابعة نهائياً من النظام.");
        }

        #endregion

        #region Unified Account Activation Logic

        public async Task<ResponseDto> ToggleUserActivationAsync(int userId)
        {
            // 1. البحث عن المستخدم
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return ResponseDto.FailureResponse("الحساب المستهدف غير موجود في النظام.");

            // 2. عكس الحالة الحالية تلقائياً (التوجل الفعلي)
            user.IsActive = !user.IsActive;

            // 3. حفظ التعديل في قاعدة البيانات
            await _context.SaveChangesAsync();

            // 4. صياغة رسالة ديناميكية واضحة بناءً على الحالة الجديدة والـ Role
            string profileType = user.Role == AccountRoles.Company ? "شركة النقل" : "العميل";
            string statusMessage = user.IsActive ? "تنشيطه بنجاح، وبإمكانه العمل الآن." : "إلغاء تنشيطه وحظر دخوله للنظام.";

            return ResponseDto.SuccessResponse($"تم تحديث حساب {profileType} و{statusMessage}");
        }

        #endregion



        #region Subscription Management Logic

        public async Task<ResponseDto> GetPendingSubscriptionsAsync()
        {
            var pendingSubs = await _context.CompanySubscription
                .Include(cs => cs.Company)
                .Where(cs => cs.Status == SubscriptionStatus.Pending && cs.RequestType == RequestType.Renewal)
                .Select(cs => new Darb.Api.DTOs.Admin.PendingSubscriptionDto
                {
                    CompanySubscriptionId = cs.CompanySubscriptionId,
                    CompanyId = cs.CompanyId,
                    CompanyName = cs.Company != null ? cs.Company.Name : "N/A",
                    PlanType = cs.PlanType,
                    SubscriptionDate = cs.SubscriptionDate,
                    PaymentSlipUrl = !string.IsNullOrEmpty(cs.PaymentSlip) ? _baseUrl + cs.PaymentSlip : string.Empty,
                    RequestType = cs.RequestType
                })
                .ToListAsync();

            return ResponseDto.SuccessResponse($"تم العثور على ({pendingSubs.Count}) طلبات اشتراك معلقة.", pendingSubs);
        }

        public async Task<ResponseDto> GetNewCompanyRegistrationRequestsAsync()
        {
            // STEP 1: Fetch pending subscription requests specifically for new company registrations.
            // Optimizing query performance with AsNoTracking since this is a read-only view.
            var newRequests = await _context.CompanySubscription
                .AsNoTracking()
                .Include(cs => cs.Company)
                    .ThenInclude(c => c!.User)
                .Where(cs => cs.Status == SubscriptionStatus.Pending && cs.RequestType == RequestType.NewRegistration)
                .OrderByDescending(cs => cs.SubscriptionDate) // STEP 2: Sort by newest requests first for better Admin UX
                .Select(cs => new Darb.Api.DTOs.Admin.CompanyRegistrationRequestDto
                {
                    CompanyId = cs.CompanyId,
                    Name = cs.Company!.Name ?? "N/A",
                    Address = cs.Company!.Address ?? "N/A",

                    // STEP 3: Generate full external URLs for all media assets consistently (Logo, License, and Payment Slip)
                    Logo = !string.IsNullOrEmpty(cs.Company.Logo) ? _baseUrl + cs.Company.Logo : string.Empty,
                    License = !string.IsNullOrEmpty(cs.Company.License) ? _baseUrl + cs.Company.License : string.Empty,
                    PaymentSlipUrl = !string.IsNullOrEmpty(cs.PaymentSlip) ? _baseUrl + cs.PaymentSlip : string.Empty,

                    Email = cs.Company.User!.Email ?? "N/A",
                    RequestDate = cs.SubscriptionDate,
                    SubscriptionId = cs.CompanySubscriptionId,
                    PlanType = cs.PlanType.ToString()
                })
                .ToListAsync();

            // STEP 4: Return a unified success response containing the count and the list of requests
            return ResponseDto.SuccessResponse($"تم العثور على ({newRequests.Count}) طلبات تسجيل جديدة.", newRequests);
        }

        public async Task<ResponseDto> AcceptSubscriptionAsync(int subscriptionId)
        {
            // Fetch the subscription along with the related Company and User entities
            var sub = await _context.CompanySubscription
                .Include(cs => cs.Company)
                    .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(cs => cs.CompanySubscriptionId == subscriptionId);

            if (sub == null)
                return ResponseDto.FailureResponse("الاشتراك غير موجود.");

            if (sub.Status != SubscriptionStatus.Pending)
                return ResponseDto.FailureResponse("لا يمكن قبول اشتراك وهو ليس in حالة معلقة.");

            sub.Status = SubscriptionStatus.Approved;

            // Set SubscriptionDate to now and calculate ExpiryDate based on the selected plan
            sub.SubscriptionDate = DateTime.UtcNow;
            if (sub.PlanType == SubscriptionPlans.Monthly)
            {
                sub.ExpiryDate = sub.SubscriptionDate.AddMonths(1);
            }
            else if (sub.PlanType == SubscriptionPlans.Yearly)
            {
                sub.ExpiryDate = sub.SubscriptionDate.AddYears(1);
            }

            // Determine whether this is a new registration or a renewal for the email
            bool isRenewal = sub.RequestType == RequestType.Renewal;

            // Activate company account if it's not already active
            if (sub.Company != null && sub.Company.User != null && !sub.Company.User.IsActive)
            {
                sub.Company.User.IsActive = true;
            }

            await _context.SaveChangesAsync();

            // Send approval notification email to the company's registered email address
            if (sub.Company?.User?.Email != null)
            {
                await _emailService.SendSubscriptionApprovalEmailAsync(
                    sub.Company.User.Email,
                    sub.Company.Name ?? "شركتكم",
                    isRenewal,
                    sub.ExpiryDate);
            }

            var successMsg = isRenewal
                ? "تم قبول تجديد الاشتراك بنجاح وتم إخطار الشركة عبر البريد الإلكتروني."
                : "تم قبول طلب تسجيل الشركة وتفعيل الحساب بنجاح وتم إخطارهم عبر البريد الإلكتروني.";

            return ResponseDto.SuccessResponse(successMsg);
        }

        public async Task<ResponseDto> RejectSubscriptionAsync(int subscriptionId)
        {
            // Fetch the subscription along with Company and User profiles to safely access contact details
            var sub = await _context.CompanySubscription
                .Include(cs => cs.Company)
                    .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(cs => cs.CompanySubscriptionId == subscriptionId);

            if (sub == null)
                return ResponseDto.FailureResponse("الاشتراك غير موجود.");

            if (sub.Status != SubscriptionStatus.Pending)
                return ResponseDto.FailureResponse("لا يمكن رفض اشتراك وهو ليس في حالة معلقة.");

            sub.Status = SubscriptionStatus.Rejected;
            await _context.SaveChangesAsync();

            // Determine request type for email context (New Registration vs Renewal rejection)
            bool isRenewal = sub.RequestType == RequestType.Renewal;

            // Send rejection notification email using the newly created EmailService method
            if (sub.Company?.User?.Email != null)
            {
                await _emailService.SendSubscriptionRejectionEmailAsync(
                    sub.Company.User.Email,
                    sub.Company.Name ?? "شركتكم",
                    isRenewal);
            }

            return ResponseDto.SuccessResponse("تم رفض طلب الاشتراك وإخطار الشركة عبر البريد الإلكتروني.");
        }

        #endregion
    }
}
