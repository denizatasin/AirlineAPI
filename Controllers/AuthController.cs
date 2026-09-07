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
    private readonly ILogger<AuthController> _logger;
    public AuthController(AirlineDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _logger=logger;
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
            _logger.LogWarning("Registration failed. Invalid role specified: {Role}",request.Role);
            return BadRequest($"Invalid role {request.Role}. Valid roles: Admin, Passenger.");
        }
        bool exists=await _context.Users.AnyAsync(u=>u.Username==request.Username);
        if(exists)
        {
            _logger.LogWarning("Registration failed. Username {Username} already exists.",request.Username);
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
                _logger.LogWarning("Registration failed. Missing first name or last name for passenger user: {Username}",request.Username);
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

        _logger.LogInformation("User '{Username}' registered successfully with role: '{Role}'.",user.Username,user.Role);
        return Ok("User registered successfully.");
    }
    [HttpPost("login")]
    public async Task<IActionResult>Login(LoginRequest request)
    {
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.Username==request.Username);
        if(user==null)
        {
            _logger.LogWarning("Login failed. User '{Username}' not found.",request.Username);
            return Unauthorized("Invalid username or password.");
        }
        var result=_passwordHasher.VerifyHashedPassword(user, user.PasswordHash,request.Password);
        if(result==PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed. Incorrect passfor for user '{Username}'.",request.Username);
            return Unauthorized("Invalid username or password.");
        }
        var token = GenerateJwtToken(user);
        _logger.LogInformation("User '{Username}' logined successfully.",user.Username);
        return Ok(new { token });
    }
}
