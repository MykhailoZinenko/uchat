namespace uchat_client.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly string _username;
    
    public SidebarViewModel SidebarViewModel { get; }
    
    public RelayCommand OpenMyAccountCommand { get; }
    public RelayCommand OpenCurrentSessionCommand { get; }
    public RelayCommand OpenSoundNotificationCommand { get; }
    
    public SettingsViewModel(MainWindowViewModel mainWindowViewModel, SidebarViewModel sidebarViewModel, string username)
    {
        _mainWindowViewModel = mainWindowViewModel;
        SidebarViewModel = sidebarViewModel;
        _username = username;
        
        OpenMyAccountCommand = new RelayCommand(OpenMyAccount);
        OpenCurrentSessionCommand = new RelayCommand(OpenCurrentSession);
        OpenSoundNotificationCommand = new RelayCommand(OpenSoundNotification);
    }

    private void OpenMyAccount()
    {
        _mainWindowViewModel.ShowMyAccount();
    }

    private void OpenCurrentSession()
    {
        _mainWindowViewModel.ShowSessions();
    }

    private void OpenSoundNotification()
    {
       _mainWindowViewModel.ShowSoundNotifications();
    }
}