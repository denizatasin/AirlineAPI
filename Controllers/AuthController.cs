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
using System.Collections.Concurrent;
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
    // Failed "forgot password" attempts per username (in memory; resets when the API restarts)
    private static readonly ConcurrentDictionary<string,List<DateTime>> _resetFailures=new();
    private const int MaxResetFailures=5;
    private static readonly TimeSpan ResetWindow=TimeSpan.FromMinutes(15);
    private static bool IsResetLocked(string key)
    {
        if(!_resetFailures.TryGetValue(key,out var list))return false;
        lock(list)
        {
            list.RemoveAll(t=>DateTime.UtcNow-t>ResetWindow);
            return list.Count>=MaxResetFailures;
        }
    }
    private static void RegisterResetFailure(string key)
    {
        var list=_resetFailures.GetOrAdd(key,_=>new List<DateTime>());
        lock(list){list.Add(DateTime.UtcNow);}
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
            return Conflict("Bu e-posta adresi zaten kayıtlı.");
        }
        if(await _context.Passengers.AnyAsync(p=>p.IdentityNumber==passenger.IdentityNumber))
        {
            return Conflict("Bu TC kimlik / pasaport numarası zaten kayıtlı.");
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
            return Conflict("Kullanıcı adı, e-posta veya TC kimlik / pasaport numarası zaten kayıtlı.");
        }
        _logger.LogInformation("User '{Username}' registered successfully as Passenger.",user.Username);
        return Ok("Kayıt başarılı.");
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
        return Ok("Admin hesabı oluşturuldu.");
    }
    // Admin cannot SEE a password (only a hash is stored). Instead the admin resets it:
    // a random temporary password is created, shown once, and the old password stops working.
    [Authorize(Roles="Admin")]
    [HttpPost("reset-password/{passengerId}")]
    public async Task<IActionResult>ResetPassword(int passengerId)
    {
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.PassengerId==passengerId);
        if(user==null)
        {
            return NotFound($"Passenger {passengerId} has no login account.");
        }
        const string alphabet="ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        string temporaryPassword=new string(Enumerable.Range(0,10).Select(_=>alphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
        user.PasswordHash=_passwordHasher.HashPassword(user,temporaryPassword);
        await _context.SaveChangesAsync();
        _logger.LogWarning("Admin '{Admin}' reset the password of user '{Username}'.",User.Identity?.Name,user.Username);
        return Ok(new{username=user.Username,temporaryPassword});
    }
    // "Forgot password": no login needed, but the caller must know username + email + TC/passport of the passenger.
    // Admin accounts have no passenger record, so they can never use this (an admin resets them by hand).
    [HttpPost("forgot-password")]
    public async Task<IActionResult>ForgotPassword(ForgotPasswordRequest request)
    {
        string username=request.Username.Trim();
        string key=username.ToLowerInvariant();
        if(IsResetLocked(key))
        {
            _logger.LogWarning("Password reset blocked for '{Username}': too many failed attempts.",username);
            return StatusCode(429,"Çok fazla hatalı deneme yaptın. 15 dakika sonra tekrar dene veya destekle iletişime geç.");
        }
        var user=await _context.Users.Include(u=>u.Passenger).FirstOrDefaultAsync(u=>u.Username==username);
        string? email=PassengerRules.NormalizeEmail(request.Email);
        string? identity=PassengerRules.NormalizeIdentity(request.IdentityNumber);
        var p=user?.Passenger;
        bool match=user!=null&&p!=null&&user.Role==UserRole.Passenger&&
            p.Email!=null&&p.Email==email&&p.IdentityNumber!=null&&p.IdentityNumber==identity;
        if(!match)
        {
            RegisterResetFailure(key);
            _logger.LogWarning("Password reset failed for '{Username}': information did not match.",username);
            // Same message for every reason, so nobody can find out which field was wrong or whether the user exists
            return BadRequest("Girdiğin bilgiler kayıtlarımızla eşleşmiyor.");
        }
        _resetFailures.TryRemove(key,out _);
        user!.PasswordHash=_passwordHasher.HashPassword(user,request.NewPassword);
        await _context.SaveChangesAsync();
        _logger.LogWarning("User '{Username}' reset their password via 'forgot password'.",user.Username);
        return Ok("Şifre sıfırlandı. Şimdi giriş yapabilirsin.");
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
            return BadRequest("Mevcut şifre yanlış.");
        }
        if(request.CurrentPassword==request.NewPassword)
        {
            return BadRequest("Yeni şifre mevcut şifreyle aynı olamaz.");
        }
        user.PasswordHash=_passwordHasher.HashPassword(user,request.NewPassword);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User '{Username}' changed their password.",user.Username);
        return Ok("Şifre değiştirildi.");
    }
    [HttpPost("login")]
    public async Task<IActionResult>Login(LoginRequest request)
    {
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.Username==request.Username);
        if(user==null)
        {
            _logger.LogWarning("Login failed. User '{Username}' not found.",request.Username);
            return Unauthorized("Kullanıcı adı veya şifre hatalı.");
        }
        var result=_passwordHasher.VerifyHashedPassword(user, user.PasswordHash,request.Password);
        if(result==PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed. Incorrect passfor for user '{Username}'.",request.Username);
            return Unauthorized("Kullanıcı adı veya şifre hatalı.");
        }
        var token = GenerateJwtToken(user);
        _logger.LogInformation("User '{Username}' logined successfully.",user.Username);
        return Ok(new { token });
    }
}
