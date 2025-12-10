using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using uchat_client.Services;

namespace uchat_client.ViewModels;

public class SessionItem
{
    public int SessionId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrent { get; set; }
}

public class SessionViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly string _username;
    private readonly IAuthService _authService;
    private string _currentSessionTitle = "Current session";

    public SidebarViewModel SidebarViewModel { get; }
    public SessionItem CurrentSession { get; set; } = new SessionItem { DeviceName = "Current device", IsCurrent = true };
    public ObservableCollection<SessionItem> ActiveSessions { get; set; } = new ObservableCollection<SessionItem>();
    public RelayCommand TerminateAllSessionsCommand { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand<SessionItem> TerminateSessionCommand { get; }

    public string CurrentSessionTitle
    {
        get => _currentSessionTitle;
        set
        {
            if (_currentSessionTitle != value)
            {
                _currentSessionTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public SessionViewModel(MainWindowViewModel mainWindowViewModel, SidebarViewModel sidebarViewModel, string username, IAuthService authService)
    {
        _mainWindowViewModel = mainWindowViewModel;
        SidebarViewModel = sidebarViewModel;
        _username = username;
        _authService = authService;

        BackCommand = new RelayCommand(GoBack);
        TerminateAllSessionsCommand = new RelayCommand(TerminateAllSessions);
        TerminateSessionCommand = new RelayCommand<SessionItem>(async s => await TerminateSessionAsync(s));

        _ = LoadSessionsAsync();
    }
    
    private void TerminateAllSessions()
    {
        // ActiveSessions.Clear();
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            var result = await _authService.GetActiveSessionsAsync();
            ActiveSessions.Clear();
            if (result.Success && result.Data != null)
            {
                SessionItem? current = null;
                foreach (var s in result.Data)
                {
                    var item = new SessionItem
                    {
                        SessionId = s.Id,
                        DeviceName = s.DeviceInfo,
                        CreatedAt = s.CreatedAt,
                        LastActivityAt = s.LastActivityAt,
                        ExpiresAt = s.ExpiresAt,
                        IsCurrent = s.Token == _authService.SessionToken
                    };
                    if (item.IsCurrent) current = item;
                    ActiveSessions.Add(item);
                }

                if (current != null)
                {
                    CurrentSession = current;
                    CurrentSessionTitle = "Current session";
                }
            }
            else
            {
                Console.WriteLine($"Failed to load sessions: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load sessions: {ex.Message}");
        }
    }
    
    private void GoBack()
    {
        _mainWindowViewModel.ShowSettings();
    }

    private async Task TerminateSessionAsync(SessionItem? sessionItem)
    {
        if (sessionItem == null) return;
        try
        {
            var result = await _authService.RevokeSessionAsync(sessionItem.SessionId);
            if (!result.Success)
            {
                Console.WriteLine($"Failed to revoke session {sessionItem.SessionId}: {result.Message}");
                return;
            }

            if (sessionItem.IsCurrent)
            {
                await _mainWindowViewModel.LogoutAsync();
            }
            else
            {
                await LoadSessionsAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error revoking session {sessionItem.SessionId}: {ex.Message}");
        }
    }
}