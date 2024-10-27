﻿using _2B_Egypt.Domain.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace _2B_Egypt.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signInManager;
    private readonly IMapper mapper;
    private readonly IConfiguration configure;

    public AccountController(UserManager<User> userManager, SignInManager<User> _signInManager, IMapper mapper, IConfiguration _configure)
    {
        this.userManager = userManager;
        this.signInManager = _signInManager;
        this.mapper = mapper;
        configure = _configure;
    }
    private JwtSecurityToken generateToken(List<Claim> claims)
    {
        
        SymmetricSecurityKey signinKey = new(Encoding.UTF8.GetBytes(configure["JWT:SecretKey"]!));
        JwtSecurityToken token = new
            (
                issuer: configure["JWT:IssuerIP"],
                audience: configure["JWT:AudienceIP"],
                expires: DateTime.Now.AddDays(2),
                claims: claims,
                signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
            );
        return token;
    }

    [HttpPost("[Action]")]
    public async Task<ActionResult> Register(CreateUserDTO createUserDTO)
    {
        if(ModelState.IsValid)
        {
            if (await userManager.FindByEmailAsync(createUserDTO?.Email!) is not null)
            {
                return await Task.FromResult(BadRequest(ModelState));
            }
            User user = mapper.Map<User>(createUserDTO);
            user.Id = Guid.NewGuid();
            user.UserName = createUserDTO?.Email;
            IdentityResult result = await userManager.CreateAsync(user,createUserDTO?.Password!);
            if(result.Succeeded)
            {
                List<Claim> userClaims =
                [
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Email,user.Email!),
                    new(ClaimTypes.Name,user.FirstName + " " + user.LastName),
                    new(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                ];
                var token = generateToken(userClaims);
                return StatusCode(201, new
                {
                    tokens = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    user = new CreateUserDTO() { FirstName = user.FirstName, LastName = user.LastName, Email = user.Email!, PhoneNumber = user.PhoneNumber }
                });
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }
        return await Task.FromResult(BadRequest(ModelState));
    }

    [HttpPost("[Action]")]
    public async Task<ActionResult> Login(LoginDTO loginDTO)
    {
        if(ModelState.IsValid)
        {
            var user = await userManager.FindByEmailAsync(loginDTO.Email);
            if (user is null)
            {
                return BadRequest("Wrong Email");
            }
            if (await userManager.CheckPasswordAsync(user, loginDTO.Password))
            {
                //await signInManager.SignInAsync(user, loginDTO.RememberMe);
                List<Claim> userClaims =
                [
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Email,user.Email!),
                    new(ClaimTypes.Name,user.FirstName + " " + user.LastName),
                    new(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                ];
                var token = generateToken(userClaims);
                return StatusCode(200, new
                {
                    tokens = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    user = new CreateUserDTO() { FirstName = user.FirstName, LastName = user.LastName, Email = user.Email!, PhoneNumber = user.PhoneNumber }
                });
            }
            return BadRequest("Model state not valid");
        }
        return await Task.FromResult(Ok());
    }

}
