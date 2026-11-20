using AirlineAPI.Data;
using AirlineAPI.DTOs;
using AirlineAPI.Models;
using AirlineAPI.Services;
using Microsoft.AspNetCore.Authorization;
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
    // Public registration: always creates a Passenger (nobody can make themselves an Admin here)
    [HttpPost("register")]
    public async Task<IActionResult>Register(RegisterRequest request)
    {
        string username=request.Username.Trim();
        if(await _context.Users.AnyAsync(u=>u.Username==username))
        {
            _logger.LogWarning("Registration failed. Username {Username} already exists.",username);
            return Conflict($"Username {username} is already taken.");
        }
        var passenger=new Passenger
        {
            FirstName=request.FirstName,
            LastName=request.LastName,
            Email=PassengerRules.NormalizeEmail(request.Email),
            Nationality=PassengerRules.NormalizeText(request.Nationality),
            IdentityNumber=PassengerRules.NormalizeIdentity(request.IdentityNumber),
            Gender=PassengerRules.NormalizeText(request.Gender),
            Phone=PassengerRules.NormalizePhone(request.Phone),
            BirthDate=request.BirthDate
        };
        var error=PassengerRules.Validate(passenger);
        if(error!=null)
        {
            _logger.LogWarning("Registration failed for {Username}: {Reason}",username,error);
            return BadRequest(error);
        }
        if(await _context.Passengers.AnyAsync(p=>p.Email==passenger.Email))
        {
            return Conflict("This email address is already registered.");
        }
        if(await _context.Passengers.AnyAsync(p=>p.IdentityNumber==passenger.IdentityNumber))
        {
            return Conflict("This TC kimlik / passport number is already registered.");
        }
        var user=new User{Username=username,Role=UserRole.Passenger,Passenger=passenger};
        user.PasswordHash=_passwordHasher.HashPassword(user,request.Password);
        try
        {
            // Passenger and User are saved together: either both exist or neither
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch(DbUpdateException ex)
        {
            _logger.LogWarning(ex,"Registration could not be saved for {Username}.",username);
            return Conflict("Username, email or TC kimlik / passport number is already registered.");
        }
        _logger.LogInformation("User '{Username}' registered successfully as Passenger.",user.Username);
        return Ok("User registered successfully.");
    }
    // Only an existing admin can create another admin account
    [Authorize(Roles="Admin")]
    [HttpPost("register-admin")]
    public async Task<IActionResult>RegisterAdmin(RegisterAdminRequest request)
    {
        string username=request.Username.Trim();
        if(await _context.Users.AnyAsync(u=>u.Username==username))
        {
            return Conflict($"Username {username} is already taken.");
        }
        var user=new User{Username=username,Role=UserRole.Admin};
        user.PasswordHash=_passwordHasher.HashPassword(user,request.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin '{Username}' created by '{Creator}'.",username,User.Identity?.Name);
        return Ok("Admin registered successfully.");
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult>ChangePassword(ChangePasswordRequest request)
    {
        string? username=User.FindFirst(ClaimTypes.Name)?.Value;
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.Username==username);
        if(user==null)
        {
            return NotFound();
        }
        var check=_passwordHasher.VerifyHashedPassword(user,user.PasswordHash,request.CurrentPassword);
        if(check==PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Password change failed for '{Username}': wrong current password.",user.Username);
            return BadRequest("Current password is incorrect.");
        }
        if(request.CurrentPassword==request.NewPassword)
        {
            return BadRequest("New password must be different from the current password.");
        }
        user.PasswordHash=_passwordHasher.HashPassword(user,request.NewPassword);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User '{Username}' changed their password.",user.Username);
        return Ok("Password changed successfully.");
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
