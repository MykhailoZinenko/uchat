namespace uchat_client.ViewModels;

public class SoundNotificationsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly string _username;
    private double _soundVolume = 50;
    private bool _notificationsEnabled = true;

    public SidebarViewModel SidebarViewModel { get; }

    public RelayCommand BackCommand { get; }
    
    public double SoundVolume
    {
        get => _soundVolume;
        set
        {
            if (_soundVolume != value)
            {
                _soundVolume = value;
                OnPropertyChanged();
            }
        }
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (_notificationsEnabled != value)
            {
                _notificationsEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public SoundNotificationsViewModel(MainWindowViewModel mainWindowViewModel, SidebarViewModel sidebarViewModel, string username)
    {
        _mainWindowViewModel = mainWindowViewModel;
        SidebarViewModel = sidebarViewModel;
        _username = username;

        BackCommand = new RelayCommand(GoBack);
    }

    public void GoBack()
    {
        _mainWindowViewModel.ShowSettings();
    }
}