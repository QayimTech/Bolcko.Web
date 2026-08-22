using System.Linq;
using System.Threading.Tasks;
using Blocko.Services.DTOs.Api;
using Blocko.Services.DTOs.Api.Auth;
using Blocko.Services.Interfaces.Auth;
using UserEntity = Bolcko.Domain.Entities.User.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    [AllowAnonymous]
    public class AuthController : BaseApiController
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly SignInManager<UserEntity> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly Blocko.Services.Interfaces.User.IEmailSender _emailSender;

        public AuthController(
            UserManager<UserEntity> userManager,
            SignInManager<UserEntity> signInManager,
            ITokenService tokenService,
            Blocko.Services.Interfaces.User.IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequestDto request)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("Invalid data", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());

            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null)
                return ErrorResponse("User already exists", statusCode: 409);

            var names = (request.FullName ?? string.Empty).Split(new[] { ' ' }, 2);
            var firstName = names.Length > 0 ? names[0] : string.Empty;
            var lastName = names.Length > 1 ? names[1] : string.Empty;

            var user = new UserEntity
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true // Assuming auto-confirmed for mobile API demo, but can be configured otherwise
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return ErrorResponse("Failed to create user", result.Errors.Select(e => e.Description).ToList(), 500);

            // Assign standard Customer role for all buyers (Mobile & Web)
            await _userManager.AddToRoleAsync(user, "Customer");

            var token = await _tokenService.GenerateTokenAsync(user);

            return OkResponse(new LoginResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim()
            }, "User registered successfully");
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("Invalid data", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return ErrorResponse("Invalid email or password", statusCode: 401);

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return ErrorResponse("Invalid email or password", statusCode: 401);

            var token = await _tokenService.GenerateTokenAsync(user);

            return OkResponse(new LoginResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim()
            });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("Invalid data", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return OkResponse("If the email exists, a reset link will be sent."); // Do not reveal user existence

            // Generate 6-digit OTP code for mobile reset flow
            var random = new System.Random();
            var otpCode = random.Next(100000, 999999).ToString();

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var otpClaim = existingClaims.FirstOrDefault(c => c.Type == "PasswordResetOTP");
            if (otpClaim != null) await _userManager.RemoveClaimAsync(user, otpClaim);
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("PasswordResetOTP", otpCode));

            var otpTimeClaim = existingClaims.FirstOrDefault(c => c.Type == "PasswordResetOTPExpiry");
            if (otpTimeClaim != null) await _userManager.RemoveClaimAsync(user, otpTimeClaim);
            var expiryTime = System.DateTime.UtcNow.AddMinutes(15).ToString("O");
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("PasswordResetOTPExpiry", expiryTime));

            string emailBody = Blocko.Services.Helpers.EmailTemplates.GetOtpTemplate(otpCode, "15");
            _ = Task.Run(() => _emailSender.SendEmailAsync(request.Email, "رمز التحقق لإعادة تعيين كلمة المرور - BLOCKO", emailBody));

            return OkResponse("If the email exists, a reset link will be sent."); 
        }

        [HttpPost("SignOut")]
        [Authorize]
        public IActionResult SignOut()
        {
            // For JWT, sign out is handled on the client side by deleting the token.
            // But we can keep this endpoint for potential token blacklisting.
            return OkResponse("Successfully signed out");
        }
    }
}
