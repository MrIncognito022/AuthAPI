using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.CodeDom.Compiler;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _jwtKey;
        private readonly string? _jwtIssuer;
        private readonly string? _jwtAudience;
        private readonly int _JwtExpiry;

        public UserAuthController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {

            _signInManager = signInManager;
            _userManager = userManager;
            _jwtKey = configuration["Jwt:Key"];
            _jwtIssuer = configuration["Jwt:Issuer"];
            _jwtAudience = configuration["Jwt:Audience"];
            _JwtExpiry = int.Parse(configuration["Jwt:ExpiryMinutes"]);
        }


        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            if(registerModel == null
                || string.IsNullOrEmpty(registerModel.Name)
                || string.IsNullOrEmpty(registerModel.Email)
                || string.IsNullOrEmpty(registerModel.Password))
            {
                return BadRequest("Invalid registration details");
            }

            var existingUser = await _userManager.FindByEmailAsync(registerModel.Email);
            if (existingUser != null)
            {
                return Conflict("Email already Exist");
            }

            var user = new ApplicationUser
            {
                UserName = registerModel.Email,
                Email = registerModel.Email,
                Name = registerModel.Name
            };

            var result = await _userManager.CreateAsync(user, registerModel.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("User Created Successfully");

        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var user = await _userManager.FindByEmailAsync(loginModel.Email);
            if(user == null)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password"});
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password" });
            }

            var token = GeneratedJwtToken(user);
            return Ok(new { success = true, token });
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("User logged out successfully.");
        }
        private string GeneratedJwtToken (ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub ,user.Id),
                new Claim(JwtRegisteredClaimNames.Email , user.Email),
                new Claim("Name" , user.Name),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                //issuer: _jwtIssuer,
                //audience: _jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_JwtExpiry),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
