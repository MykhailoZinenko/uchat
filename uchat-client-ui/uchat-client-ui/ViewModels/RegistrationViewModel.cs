namespace uchat_client.ViewModels;

public class RegistrationViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public RelayCommand RegisterCommand { get; }
    public RelayCommand BackToLoginCommand { get; }

    public RegistrationViewModel(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow;
        RegisterCommand = new RelayCommand(Register);
        BackToLoginCommand = new RelayCommand(BackToLogin);
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            return;
        
        _mainWindow.ShowChat(Username);
    }

    private void BackToLogin()
    {
        _mainWindow.ShowLogin();
    }
}