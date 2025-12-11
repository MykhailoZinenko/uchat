using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Features.Settings.ViewModels;

public class SessionsViewModel : NavigableViewModelBase
{
    private readonly SidebarViewModel _sidebarViewModel;
    private readonly IAuthService _authService;
    private ObservableCollection<SessionItemViewModel> _activeSessions = new();
    private SessionItemViewModel _currentSession;

    public SidebarViewModel SidebarViewModel => _sidebarViewModel;

    public ObservableCollection<SessionItemViewModel> ActiveSessions
    {
        get => _activeSessions;
        set => SetProperty(ref _activeSessions, value);
    }

    public SessionItemViewModel CurrentSession
    {
        get => _currentSession;
        set
        {
            if (SetProperty(ref _currentSession, value))
            {
                OnPropertyChanged(nameof(CurrentSessionTitle));
            }
        }
    }

    public string CurrentSessionTitle => "This Device";

    public ICommand BackCommand { get; }
    public ICommand TerminateOtherSessionsCommand { get; }
    public ICommand TerminateSessionCommand { get; }

    public SessionsViewModel(
        INavigationService navigationService,
        SidebarViewModel sidebarViewModel,
        IAuthService authService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _sidebarViewModel = sidebarViewModel;
        _authService = authService;
        _currentSession = new SessionItemViewModel();

        BackCommand = new RelayCommand(() => NavigationService.NavigateToSettings());
        TerminateOtherSessionsCommand = new AsyncRelayCommand(TerminateOtherSessionsAsync);
        TerminateSessionCommand = new AsyncRelayCommand<SessionItemViewModel>(async (session) =>
        {
            if (session != null && !session.IsCurrent) await TerminateSessionAsync(session.SessionId);
        });
    }

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        _sidebarViewModel.IsOnSettingsPage = true;
        await LoadSessionsAsync();
        await base.OnNavigatedToAsync(parameter);
    }

    private async Task LoadSessionsAsync()
    {
        await ExecuteAsync(async () =>
        {
            Logger.LogDebug("Loading active sessions");
            var response = await _authService.GetActiveSessionsAsync();

            if (response.Success && response.Data != null)
            {
                var currentToken = _authService.SessionToken ?? _authService.LoadSessionToken();

                var sessionItems = response.Data
                    .Select(session => MapSession(session, currentToken))
                    .ToList();

                var currentSession = sessionItems.FirstOrDefault(s => s.IsCurrent) ?? sessionItems.FirstOrDefault();
                if (currentSession != null)
                {
                    CurrentSession = currentSession;
                }

                ActiveSessions = new ObservableCollection<SessionItemViewModel>(sessionItems);
            }
            else
            {
                ErrorMessage = response.Message;
            }
        });
    }

    private async Task TerminateSessionAsync(int sessionId)
    {
        await ExecuteAsync(async () =>
        {
            if (CurrentSession?.SessionId == sessionId)
            {
                Logger.LogInformation("Skipping termination of current session: {SessionId}", sessionId);
                return;
            }

            Logger.LogInformation("Terminating session: {SessionId}", sessionId);
            var response = await _authService.RevokeSessionAsync(sessionId);

            if (response.Success)
            {
                var session = ActiveSessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session != null)
                {
                    ActiveSessions.Remove(session);
                }
            }
            else
            {
                ErrorMessage = response.Message;
            }
        });
    }

    private async Task TerminateOtherSessionsAsync()
    {
        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("Terminating all other sessions");

            try
            {
                var otherSessionIds = ActiveSessions
                    .Where(s => !s.IsCurrent)
                    .Select(s => s.SessionId)
                    .ToList();

                if (otherSessionIds.Count == 0)
                {
                    ErrorMessage = "No other sessions to terminate.";
                    return;
                }

                var response = await _authService.RevokeSessionsAsync(otherSessionIds);

                if (response.Success)
                {
                    foreach (var session in ActiveSessions.Where(s => !s.IsCurrent).ToList())
                    {
                        ActiveSessions.Remove(session);
                    }
                }
                else
                {
                    ErrorMessage = response.Message;
                }
            }
            catch (Exception ex) when (ex.Message.Contains("Method does not exist"))
            {
                Logger.LogWarning("RevokeSessions method not implemented on server");
                ErrorMessage = "Feature not yet implemented on the server. Please terminate sessions individually.";
            }
        });
    }

    private static SessionItemViewModel MapSession(SessionInfo session, string? currentToken)
    {
        var isCurrent = !string.IsNullOrWhiteSpace(currentToken) && session.Token == currentToken;

        return new SessionItemViewModel
        {
            SessionId = session.Id,
            SessionToken = session.Token,
            DeviceName = session.DeviceInfo,
            CreatedAt = session.CreatedAt.ToLocalTime(),
            LastActivityAt = session.LastActivityAt.ToLocalTime(),
            IsCurrent = isCurrent
        };
    }
}

public class SessionItemViewModel : ObservableObject
{
    private int _sessionId;
    private string _deviceName = string.Empty;
    private string _sessionToken = string.Empty;
    private DateTime _lastActivityAt;
    private DateTime _createdAt;
    private bool _isCurrent;

    public int SessionId
    {
        get => _sessionId;
        set => SetProperty(ref _sessionId, value);
    }

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public string SessionToken
    {
        get => _sessionToken;
        set => SetProperty(ref _sessionToken, value);
    }

    public DateTime LastActivityAt
    {
        get => _lastActivityAt;
        set => SetProperty(ref _lastActivityAt, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }

}
