namespace uchat_client.ViewModels;

public class LoginViewModel
{
    private readonly MainWindowViewModel _mainWindow;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public RelayCommand LoginCommand { get; }
    public RelayCommand ShowRegisterCommand { get; }

    public LoginViewModel(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow;
        LoginCommand = new RelayCommand(Login);
        ShowRegisterCommand = new RelayCommand(ShowRegister);
    }

    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Username))
            return;

        _mainWindow.ShowChat(Username);
    }

    private void ShowRegister()
    {
        _mainWindow.ShowRegistration();
    }
}