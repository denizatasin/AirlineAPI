using AirlineAPI.Data;
using AirlineAPI.DTOs;
using AirlineAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AirlineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController: ControllerBase
{
    private readonly AirlineDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher=new();
    private readonly IConfiguration _configuration;
    public AuthController(AirlineDbContext context, IConfiguration configuration)
    {
        _context=context;
        _configuration=configuration;
    }
    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        if(user.PassengerId.HasValue)
        {
            claims.Add(new Claim("PassengerId", user.PassengerId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    [HttpPost("register")]
    public async Task<IActionResult>Register(RegisterRequest request)
    {
        if(!Enum.TryParse<UserRole>(request.Role,true,out var role))
        {
            return BadRequest($"Invalid role {request.Role}. Valid roles: Admin, Passenger.");
        }
        bool exists=await _context.Users.AnyAsync(u=>u.Username==request.Username);
        if(exists)
        {
            return Conflict($"Username {request.Username} is already taken.");
        }
        var user=new User
        {
            Username=request.Username,
            Role=role
        };
        user.PasswordHash=_passwordHasher.HashPassword(user,request.Password);

        if(role==UserRole.Passenger)
        {
            if(string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest("FirstName and LastName are required for Passenger role.");
            }
            var passenger=new Passenger
            {
                FirstName=request.FirstName,
                LastName=request.LastName
            };
            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            user.PassengerId=passenger.Id;
        }
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("User registered successfully.");
    }
    [HttpPost("login")]
    public async Task<IActionResult>Login(LoginRequest request)
    {
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.Username==request.Username);
        if(user==null)
        {
            return Unauthorized("Invalid username or password.");
        }
        var result=_passwordHasher.VerifyHashedPassword(user, user.PasswordHash,request.Password);
        if(result==PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid username or password.");
        }
        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }
}
