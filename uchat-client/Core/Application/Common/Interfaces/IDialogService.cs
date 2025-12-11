using System.Threading.Tasks;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task ShowInformationAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "");
}
