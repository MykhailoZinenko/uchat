namespace uchat_client.ViewModels;

public class MyAccountViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    
    public SidebarViewModel SidebarViewModel { get; }
    
    private readonly string _username;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (_currentPassword != value)
            {
                _currentPassword = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (_newPassword != value)
            {
                _newPassword = value;
                OnPropertyChanged();
            }
        }
    }

    public RelayCommand ChangePasswordCommand { get; }
    public RelayCommand LogOutCommand { get; }
    public RelayCommand DeleteAccountCommand { get; }
    public RelayCommand BackCommand { get; }
    
    public MyAccountViewModel(MainWindowViewModel mainWindowViewModel, SidebarViewModel sidebarViewModel, string username)
    {
        _mainWindowViewModel = mainWindowViewModel;
        
        SidebarViewModel = sidebarViewModel;
        
        _username = username;
        
        ChangePasswordCommand = new RelayCommand(ChangePassword);
        LogOutCommand = new RelayCommand(LogOut);
        DeleteAccountCommand = new RelayCommand(DeleteAccount);
        BackCommand = new RelayCommand(GoBack);
    }

    private void ChangePassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
            return;
        
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
    }

    private void LogOut()
    {
        _mainWindowViewModel.ShowLogin();
    }

    private void DeleteAccount()
    {
        //todo
    }

    private void GoBack()
    {
        _mainWindowViewModel.ShowSettings();
    }
}