using Microsoft.AspNetCore.Identity;

namespace Prezentownik.WebApi.Modules.Auth;

public sealed class LocalizedIdentityErrorDescriber
    : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
    {
        Code = nameof(DefaultError),
        Description = Resources.IdentityErrors.DefaultError
    };

    public override IdentityError ConcurrencyFailure() => new()
    {
        Code = nameof(ConcurrencyFailure),
        Description = Resources.IdentityErrors.ConcurrencyFailure
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = Resources.IdentityErrors.PasswordMismatch
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = nameof(InvalidToken),
        Description = Resources.IdentityErrors.InvalidToken
    };

    public override IdentityError RecoveryCodeRedemptionFailed() => new()
    {
        Code = nameof(RecoveryCodeRedemptionFailed),
        Description = Resources.IdentityErrors.RecoveryCodeRedemptionFailed
    };

    public override IdentityError LoginAlreadyAssociated() => new()
    {
        Code = nameof(LoginAlreadyAssociated),
        Description = Resources.IdentityErrors.LoginAlreadyAssociated
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = string.Format(Resources.IdentityErrors.InvalidUserName, userName)
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = string.Format(Resources.IdentityErrors.InvalidEmail, email)
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = string.Format(Resources.IdentityErrors.DuplicateUserName, userName)
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = string.Format(Resources.IdentityErrors.DuplicateEmail, email)
    };

    public override IdentityError InvalidRoleName(string? role) => new()
    {
        Code = nameof(InvalidRoleName),
        Description = string.Format(Resources.IdentityErrors.InvalidRoleName, role)
    };

    public override IdentityError DuplicateRoleName(string role) => new()
    {
        Code = nameof(DuplicateRoleName),
        Description = string.Format(Resources.IdentityErrors.DuplicateRoleName, role)
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = nameof(UserAlreadyHasPassword),
        Description = Resources.IdentityErrors.UserAlreadyHasPassword
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = Resources.IdentityErrors.UserLockoutNotEnabled
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
    {
        Code = nameof(UserAlreadyInRole),
        Description = string.Format(Resources.IdentityErrors.UserAlreadyInRole, role)
    };

    public override IdentityError UserNotInRole(string role) => new()
    {
        Code = nameof(UserNotInRole),
        Description = string.Format(Resources.IdentityErrors.UserNotInRole, role)
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = string.Format(Resources.IdentityErrors.PasswordTooShort, length)
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = string.Format(Resources.IdentityErrors.PasswordRequiresUniqueChars, uniqueChars)
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = Resources.IdentityErrors.PasswordRequiresNonAlphanumeric
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = Resources.IdentityErrors.PasswordRequiresDigit
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = Resources.IdentityErrors.PasswordRequiresLower
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = Resources.IdentityErrors.PasswordRequiresUpper
    };
}
