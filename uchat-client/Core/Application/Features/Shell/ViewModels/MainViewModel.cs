using System;
using System.Threading.Tasks;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;

namespace uchat_client.Core.Application.Features.Shell.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IHubConnectionService _hubConnection;
    private readonly SidebarViewModel _sidebarViewModel;
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public MainViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IHubConnectionService hubConnection,
        SidebarViewModel sidebarViewModel,
        ILoggingService logger)
        : base(logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _sidebarViewModel = sidebarViewModel ?? throw new ArgumentNullException(nameof(sidebarViewModel));

        _navigationService.Navigated += OnNavigated;
        _authService.SessionRevoked += OnSessionRevoked;

        // Initialize asynchronously without blocking constructor
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Logger.LogInformation("Starting application initialization");

        await ExecuteAsync(async () =>
        {
            // Start hub connection
            Logger.LogInformation("Starting hub connection");
            await _hubConnection.StartAsync();
            Logger.LogInformation("Hub connection started");

            // Try auto-login with saved session
            var savedToken = _authService.LoadSessionToken();
            if (!string.IsNullOrEmpty(savedToken))
            {
                Logger.LogInformation("Attempting auto-login with saved session");
                var response = await _authService.LoginWithRefreshTokenAsync(savedToken);

                if (response.Success)
                {
                    Logger.LogInformation("Auto-login successful");
                    var username = response.Data?.Username ?? _authService.LoadUsername() ?? "User";
                    Logger.LogInformation("Auto-login successful for {Username}, loading rooms", username);
                    await _sidebarViewModel.EnsureRoomsLoadedAsync();
                    var room = _sidebarViewModel.GetDefaultRoom();
                    if (room != null)
                    {
                        _navigationService.NavigateToChat(room.Id, room.DisplayName, room.IsGlobal);
                        return;
                    }
                    Logger.LogWarning("No rooms available after auto-login; navigating to settings");
                    _navigationService.NavigateToSettings();
                    return;
                }
                else
                {
                    Logger.LogWarning("Auto-login failed: {Message}", response.Message);
                }
            }
            else
            {
                Logger.LogInformation("No saved session found");
            }

            // Show login
            _navigationService.NavigateToLogin();
        }, showBusy: false);
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        Logger.LogInformation("OnNavigated called with ViewModel: {ViewModelType}", e.ViewModel?.GetType().Name ?? "null");
        CurrentView = e.ViewModel;
        Logger.LogInformation("CurrentView updated to: {ViewModelType}", CurrentView?.GetType().Name ?? "null");
    }

    public override void Dispose()
    {
        _navigationService.Navigated -= OnNavigated;
        _authService.SessionRevoked -= OnSessionRevoked;
        base.Dispose();
    }

    private void OnSessionRevoked(object? sender, EventArgs e)
    {
        Logger.LogWarning("Session revoked remotely. Navigating to login.");
        _navigationService.NavigateToLogin();
    }
}
