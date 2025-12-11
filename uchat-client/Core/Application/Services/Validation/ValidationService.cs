using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.Models;

namespace uchat_client.Core.Application.Services.Validation;

public class ValidationService : IValidationService
{
    public ValidationResult ValidateLogin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Failure("Username is required");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return ValidationResult.Failure("Password is required");
        }

        return ValidationResult.Success();
    }

    public ValidationResult ValidateRegistration(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Failure("Username is required");
        }

        if (username.Length < 3)
        {
            return ValidationResult.Failure("Username must be at least 3 characters");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return ValidationResult.Failure("Password is required");
        }

        if (password.Length < 6)
        {
            return ValidationResult.Failure("Password must be at least 6 characters");
        }

        if (password != confirmPassword)
        {
            return ValidationResult.Failure("Passwords do not match");
        }

        return ValidationResult.Success();
    }

    public ValidationResult ValidatePasswordChange(string oldPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            return ValidationResult.Failure("Current password is required");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return ValidationResult.Failure("New password is required");
        }

        if (newPassword.Length < 6)
        {
            return ValidationResult.Failure("New password must be at least 6 characters");
        }

        if (newPassword != confirmPassword)
        {
            return ValidationResult.Failure("Passwords do not match");
        }

        if (oldPassword == newPassword)
        {
            return ValidationResult.Failure("New password must be different from current password");
        }

        return ValidationResult.Success();
    }

    public ValidationResult ValidateContactUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Failure("Username is required");
        }

        if (username.Length < 3)
        {
            return ValidationResult.Failure("Username must be at least 3 characters");
        }

        return ValidationResult.Success();
    }
}
