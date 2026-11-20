using System.Text.RegularExpressions;
using AirlineAPI.Models;

namespace AirlineAPI.Services;

/// <summary>Validation and cleaning rules for passenger profile data (used by Register, profile and admin endpoints).</summary>
public static class PassengerRules
{
    public const string Turkey="Türkiye";
    public static readonly string[] Genders={"Female","Male","Other"};
    private static readonly Regex EmailPattern=new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",RegexOptions.Compiled);
    private static readonly Regex PassportPattern=new(@"^[A-Z0-9]{6,12}$",RegexOptions.Compiled);
    private static readonly Regex PhonePattern=new(@"^\+?[0-9]{10,14}$",RegexOptions.Compiled);

    public static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
    // "0555 123 45 67" -> "05551234567"
    public static string? NormalizePhone(string? phone)
    {
        if(string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }
        return new string(phone.Where(c=>!char.IsWhiteSpace(c) && c!='-' && c!='(' && c!=')').ToArray());
    }
    public static string? NormalizeIdentity(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : id.Trim().ToUpperInvariant();
    }
    public static string? NormalizeText(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>TC kimlik no algorithm: 11 digits, first digit not 0, 10th and 11th digits are check digits.</summary>
    public static bool IsValidTurkishId(string id)
    {
        if(id.Length!=11 || id[0]=='0' || !id.All(char.IsAsciiDigit))
        {
            return false;
        }
        int[] d=id.Select(c=>c-'0').ToArray();
        int tenth=(((d[0]+d[2]+d[4]+d[6]+d[8])*7-(d[1]+d[3]+d[5]+d[7]))%10+10)%10;
        int eleventh=d.Take(10).Sum()%10;
        return d[9]==tenth && d[10]==eleventh;
    }

    /// <summary>Returns an error message, or null when everything is valid. Expects already normalized values.</summary>
    public static string? Validate(Passenger p)
    {
        if(p.Email!=null && (p.Email.Length>254 || !EmailPattern.IsMatch(p.Email)))
        {
            return "Email address is not valid.";
        }
        if(p.Phone!=null && !PhonePattern.IsMatch(p.Phone))
        {
            return "Phone number must contain 10-14 digits (an optional leading + is allowed).";
        }
        if(p.Gender!=null && !Genders.Contains(p.Gender))
        {
            return "Gender must be one of: Female, Male, Other.";
        }
        if(p.Nationality!=null && p.Nationality.Length>60)
        {
            return "Nationality is too long.";
        }
        if(p.BirthDate!=null)
        {
            var today=DateOnly.FromDateTime(DateTime.Today);
            if(p.BirthDate>today || p.BirthDate<new DateOnly(1900,1,1))
            {
                return "Birth date is not valid.";
            }
        }
        if(p.IdentityNumber!=null)
        {
            if(p.Nationality==null)
            {
                return "Nationality is required when an identity number is given.";
            }
            if(p.Nationality==Turkey)
            {
                if(!IsValidTurkishId(p.IdentityNumber))
                {
                    return "TC kimlik number is not valid.";
                }
            }
            else if(!PassportPattern.IsMatch(p.IdentityNumber))
            {
                return "Passport number must be 6-12 letters or digits.";
            }
        }
        return null;
    }

    /// <summary>"12345678901" -> "123******01"</summary>
    public static string? MaskIdentity(string? id)
    {
        if(id==null || id.Length<6)
        {
            return id;
        }
        return id[..3]+new string('*',id.Length-5)+id[^2..];
    }
}
