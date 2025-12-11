using uchat_client.Core.Application.Common.Models;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IValidationService
{
    ValidationResult ValidateLogin(string username, string password);
    ValidationResult ValidateRegistration(string username, string password, string confirmPassword);
    ValidationResult ValidatePasswordChange(string oldPassword, string newPassword, string confirmPassword);
    ValidationResult ValidateContactUsername(string username);
}
