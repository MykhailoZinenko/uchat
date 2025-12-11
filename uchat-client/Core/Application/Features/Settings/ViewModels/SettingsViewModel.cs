using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;

namespace uchat_client.Core.Application.Features.Settings.ViewModels;

public class SettingsViewModel : NavigableViewModelBase
{
    private readonly SidebarViewModel _sidebarViewModel;
    private readonly IAuthService _authService;
    private string _username;

    public SidebarViewModel SidebarViewModel => _sidebarViewModel;

    public ICommand OpenMyAccountCommand { get; }
    public ICommand OpenSessionsCommand { get; }
    public ICommand OpenCurrentSessionCommand => OpenSessionsCommand; // Alias for XAML binding
    public ICommand OpenSoundNotificationsCommand { get; }
    public ICommand OpenSoundNotificationCommand => OpenSoundNotificationsCommand; // Alias for XAML binding
    public ICommand LogoutCommand { get; }

    public SettingsViewModel(
        INavigationService navigationService,
        SidebarViewModel sidebarViewModel,
        IAuthService authService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _sidebarViewModel = sidebarViewModel;
        _authService = authService;
        _username = string.Empty;

        OpenMyAccountCommand = new RelayCommand(() => NavigationService.NavigateToMyAccount());
        OpenSessionsCommand = new RelayCommand(() => NavigationService.NavigateToSessions());
        OpenSoundNotificationsCommand = new RelayCommand(() => NavigationService.NavigateToSoundNotifications());
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
    }

    public override System.Threading.Tasks.Task OnNavigatedToAsync(object? parameter = null)
    {
        _sidebarViewModel.IsOnSettingsPage = true;
        return base.OnNavigatedToAsync(parameter);
    }

    private async System.Threading.Tasks.Task LogoutAsync()
    {
        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("User logging out from settings");
            await _authService.LogoutAsync();
            NavigationService.NavigateToLogin();
        });
    }
}
