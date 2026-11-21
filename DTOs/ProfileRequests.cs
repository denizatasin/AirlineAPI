using System.ComponentModel.DataAnnotations;
namespace AirlineAPI.DTOs;

/// <summary>Creates an admin account (only an existing admin may call this).</summary>
public class RegisterAdminRequest
{
    [Required(ErrorMessage ="Username is required.")]
    [RegularExpression(@"^[A-Za-z0-9_.]{3,30}$",ErrorMessage ="Username must be 3-30 characters: letters, digits, '_' or '.'.")]
    public string Username{get;set;}="";
    [Required(ErrorMessage ="Password is required.")]
    [StringLength(100,MinimumLength=6,ErrorMessage ="Password must be at least 6 characters.")]
    public string Password{get;set;}="";
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage ="Current password is required.")]
    public string CurrentPassword{get;set;}="";
    [Required(ErrorMessage ="New password is required.")]
    [StringLength(100,MinimumLength=6,ErrorMessage ="New password must be at least 6 characters.")]
    public string NewPassword{get;set;}="";
}

/// <summary>Self-service password reset: username + email + TC/passport must all match the passenger record.</summary>
public class ForgotPasswordRequest
{
    [Required(ErrorMessage ="Username is required.")]
    public string Username{get;set;}="";
    [Required(ErrorMessage ="Email is required.")]
    public string Email{get;set;}="";
    [Required(ErrorMessage ="TC kimlik / passport number is required.")]
    public string IdentityNumber{get;set;}="";
    [Required(ErrorMessage ="New password is required.")]
    [StringLength(100,MinimumLength=6,ErrorMessage ="New password must be at least 6 characters.")]
    public string NewPassword{get;set;}="";
}

/// <summary>
/// What a passenger may change on their own profile.
/// Nationality, IdentityNumber and BirthDate can only be filled in while they are still empty (old accounts);
/// once set, only an admin can change them.
/// </summary>
public class UpdateMyProfileRequest
{
    [Required(ErrorMessage ="Email is required.")]
    public string Email{get;set;}="";
    public string? Phone{get;set;}
    public string? Gender{get;set;}
    public string? Nationality{get;set;}
    public string? IdentityNumber{get;set;}
    public DateOnly? BirthDate{get;set;}
}
