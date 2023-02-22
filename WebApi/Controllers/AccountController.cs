using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using WebApi.Areas.Identity.Data;
using System.Linq;
using WebApi.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private WebApi.Data.WebApiContext _dbContext;
        private readonly UserManager<WebApiUser> _userManager;
        private readonly SignInManager<WebApiUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AccountController(Data.WebApiContext dbContext,
                UserManager<WebApiUser> userManager,
                SignInManager<WebApiUser> signInManager,
                IConfiguration configuration
            )
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("getToken")]
        public async Task<ActionResult> GetToken([FromBody] MyLoginModelType myLoginModel)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Email == myLoginModel.Email);
            if(user != null)
            {
                var signInResult = await _signInManager.CheckPasswordSignInAsync(user, myLoginModel.Password, false);

                if (signInResult.Succeeded)
                {
                    var key = _configuration.GetValue<string>("JwtConfig:Key");
                    var keyBytes = Encoding.ASCII.GetBytes(key);

                    var tokenHandler = new JwtSecurityTokenHandler();

                    var tokenDescriptor = new SecurityTokenDescriptor()
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.NameIdentifier, myLoginModel.Email),
                        }),
                        Expires = DateTime.UtcNow.AddMinutes(30),
                        SigningCredentials = new SigningCredentials
                            (new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256)

                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    // return tokenHandler.WriteToken(token);
                    return Ok(new { Token = tokenString });
                    throw new System.NotImplementedException();
                } 
                else
                {
                    return Unauthorized(new { message = "Failed, try again" });
                }
            }
            return Unauthorized(new { message = "Failed, try again" });

            //if (!Data2.Users.Any(x => x.Key.Equals(username) && x.Value.Equals(password)))
            //{
            //    return null;
            //}
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] MyLoginModelType myLoginModel)
        {
            WebApiUser webApiUser = new WebApiUser()
            {
                Email = myLoginModel.Email,
                UserName = myLoginModel.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(webApiUser, myLoginModel.Password);

            if (result.Succeeded)
            {
                return Ok(new { message = "Register Success" });
            }
            else
            {
                StringBuilder stringBuilder= new StringBuilder();
                foreach (var error in result.Errors)
                {
                    stringBuilder.Append(error.Description);
                    stringBuilder.Append("\r\n");
                }

                return Ok(new {result = $"Register Fail: {stringBuilder.ToString()}" });
            }
        }
    }


}
