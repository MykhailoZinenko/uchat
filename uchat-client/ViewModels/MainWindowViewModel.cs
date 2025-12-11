using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace uchat_client.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private object? _currentView;
    private string _currentUsername = "";

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

    public MainWindowViewModel()
    {
        ShowLogin();
    }

    public void ShowLogin()
    {
        CurrentView = new LoginViewModel(this);
    }

    public void ShowRegistration()
    {
        CurrentView = new RegistrationViewModel(this);
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

        CurrentView = new SessionViewModel(this, _sidebarViewModel, _currentUsername);
    }

    public void ShowSoundNotifications()
    {
        if (_sidebarViewModel == null) _sidebarViewModel = new SidebarViewModel(this);

        _sidebarViewModel.IsOnSettingsPage = true;

        CurrentView = new SoundNotificationsViewModel(this, _sidebarViewModel, _currentUsername);
    }

    // NEW METHOD: Show Contact/Friend List
    public void ShowContactFriendList()
    {
        if (_sidebarViewModel == null) _sidebarViewModel = new SidebarViewModel(this);

        _sidebarViewModel.IsOnSettingsPage = false;
        _sidebarViewModel.Username = _currentUsername;

        CurrentView = new ContactFriendListViewModel(this, _sidebarViewModel);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}