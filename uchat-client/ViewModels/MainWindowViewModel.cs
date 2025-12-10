using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using uchat_client.Services;

namespace uchat_client.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IHubConnectionService _hubConnection;
    private readonly IAuthService _authService;

    private object? _currentView;
    private string _currentUsername = string.Empty;
    private SidebarViewModel? _sidebarViewModel;

    public object? CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView != value)
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel(IHubConnectionService hubConnection, IAuthService authService)
    {
        _hubConnection = hubConnection;
        _authService = authService;

        // Try to connect and restore session
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        Console.WriteLine("[MainWindow] Starting initialization...");
        try
        {
            Console.WriteLine("[MainWindow] Attempting to start hub connection...");
            await _hubConnection.StartAsync();
            Console.WriteLine("[MainWindow] Hub connection started successfully");

            // Try to load saved session
            var savedToken = _authService.LoadSessionToken();
            var savedUsername = _authService.LoadUsername();
            if (!string.IsNullOrEmpty(savedToken))
            {
                Console.WriteLine("[MainWindow] Found saved session token, attempting auto-login...");
                var response = await _authService.LoginWithRefreshTokenAsync(savedToken);
                if (response.Success)
                {
                    Console.WriteLine("[MainWindow] Auto-login successful");
                    var username = response.Data?.Username ?? savedUsername ?? "User";
                    ShowChat(username);
                    return;
                }
                else
                {
                    Console.WriteLine($"[MainWindow] Auto-login failed: {response.Message}");
                }
            }
            else
            {
                Console.WriteLine("[MainWindow] No saved session token found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindow] Initialization error: {ex.Message}");
            Console.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
        }

        // Show login screen
        Console.WriteLine("[MainWindow] Showing login screen");
        ShowLogin();
    }

    public void ShowLogin()
    {
        CurrentView = new LoginViewModel(this, _authService);
    }

    public void ShowRegistration()
    {
        CurrentView = new RegistrationViewModel(this, _authService);
    }

    public void ShowChat(string username)
    {
        _currentUsername = username;

        if (_sidebarViewModel == null)
        {
            _sidebarViewModel = new SidebarViewModel(this);
        }

        _sidebarViewModel.Username = username;
        _sidebarViewModel.IsOnSettingsPage = false;

        CurrentView = new ChatViewModel(username, this, _sidebarViewModel);
    }

    public async Task LogoutAsync()
    {
        Console.WriteLine("[MainWindow] Logging out...");
        try
        {
            var result = await _authService.LogoutAsync();
            Console.WriteLine($"[MainWindow] Logout result: {result.Success} {result.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindow] Logout error: {ex.Message}");
        }
        _currentUsername = string.Empty;
        _sidebarViewModel = null;
        ShowLogin();
    }

    public void ShowSettings()
    {
        if (_sidebarViewModel == null) _sidebarViewModel = new SidebarViewModel(this);

        _sidebarViewModel.IsOnSettingsPage = true;
        _sidebarViewModel.Username = _currentUsername;

        CurrentView = new SettingsViewModel(this, _sidebarViewModel, _currentUsername);
    }

    public void ShowMyAccount()
    {
        if (_sidebarViewModel == null) _sidebarViewModel = new SidebarViewModel(this);

        _sidebarViewModel.IsOnSettingsPage = true;

        CurrentView = new MyAccountViewModel(this, _sidebarViewModel, _currentUsername);
    }

    public void ShowSessions()
    {
        if (_sidebarViewModel == null) _sidebarViewModel = new SidebarViewModel(this);

        _sidebarViewModel.IsOnSettingsPage = true;

        CurrentView = new SessionViewModel(this, _sidebarViewModel, _currentUsername, _authService);
    }

    public void ShowSoundNotifications()
    {
        if (_sidebarViewModel == null) _sidebarViewModel = new SidebarViewModel(this);

        _sidebarViewModel.IsOnSettingsPage = true;

        CurrentView = new SoundNotificationsViewModel(this, _sidebarViewModel, _currentUsername);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
