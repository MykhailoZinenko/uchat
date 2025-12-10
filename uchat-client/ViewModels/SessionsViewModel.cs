using System.Collections.ObjectModel;
using System.Windows.Input;

namespace uchat_client.ViewModels;

public class SessionItem
{
    public string DeviceName { get; set; }
}

public class SessionViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly string _username;

    public SidebarViewModel SidebarViewModel { get; }
    public SessionItem CurrentSession { get; set; }
    public ObservableCollection<SessionItem> ActiveSessions { get; set; }
    public RelayCommand TerminateAllSessionsCommand { get; }
    public RelayCommand BackCommand { get; }

    public SessionViewModel(MainWindowViewModel mainWindowViewModel, SidebarViewModel sidebarViewModel, string username)
    {
        _mainWindowViewModel = mainWindowViewModel;
        SidebarViewModel = sidebarViewModel;
        _username = username;
        
        // CurrentSession = new SessionItem { DeviceName = "This Device"};
        // ActiveSessions = new ObservableCollection<SessionItem>();
        
        BackCommand = new RelayCommand(GoBack);
        TerminateAllSessionsCommand = new RelayCommand(TerminateAllSessions);
    }
    
    private void TerminateAllSessions()
    {
        // ActiveSessions.Clear();
    }
    
    private void GoBack()
    {
        _mainWindowViewModel.ShowSettings();
    }
}