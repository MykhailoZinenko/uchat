using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;

namespace uchat_client.Core.Application.Features.Settings.ViewModels;

public class MyAccountViewModel : NavigableViewModelBase
{
    private readonly SidebarViewModel _sidebarViewModel;
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    public SidebarViewModel SidebarViewModel => _sidebarViewModel;

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public ICommand BackCommand { get; }
    public ICommand ChangePasswordCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand LogOutCommand => LogoutCommand; // Alias for XAML binding
    public ICommand DeleteAccountCommand { get; }

    public MyAccountViewModel(
        INavigationService navigationService,
        SidebarViewModel sidebarViewModel,
        IAuthService authService,
        IDialogService dialogService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _sidebarViewModel = sidebarViewModel;
        _authService = authService;
        _dialogService = dialogService;

        BackCommand = new RelayCommand(() => NavigationService.NavigateToSettings());
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        DeleteAccountCommand = new AsyncRelayCommand(DeleteAccountAsync);
    }

    public override Task OnNavigatedToAsync(object? parameter = null)
    {
        _sidebarViewModel.IsOnSettingsPage = true;
        return base.OnNavigatedToAsync(parameter);
    }

    private async Task ChangePasswordAsync()
    {
        // TODO: Implement password change
        await _dialogService.ShowInformationAsync("Not Implemented", "Password change feature is not yet implemented");
    }

    private async Task LogoutAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync("Logout", "Are you sure you want to logout?");
        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("User logging out");
            await _authService.LogoutAsync();
            NavigationService.NavigateToLogin();
        });
    }

    private async Task DeleteAccountAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync("Delete Account", "Are you sure you want to delete your account? This action cannot be undone.");
        if (!confirmed) return;

        // TODO: Implement account deletion
        await _dialogService.ShowInformationAsync("Not Implemented", "Account deletion feature is not yet implemented");
    }
}
