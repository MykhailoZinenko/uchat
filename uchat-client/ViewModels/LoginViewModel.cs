using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using uchat_client.Services;

namespace uchat_client.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly IAuthService _authService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand LoginCommand { get; }
    public RelayCommand ShowRegisterCommand { get; }

    public LoginViewModel(MainWindowViewModel mainWindow, IAuthService authService)
    {
        _mainWindow = mainWindow;
        _authService = authService;

        LoginCommand = new RelayCommand(async () => await LoginAsync());
        ShowRegisterCommand = new RelayCommand(ShowRegister);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var response = await _authService.LoginAsync(Username, Password);

            if (response.Success)
            {
                var name = response.Data?.Username ?? Username;
                _mainWindow.ShowChat(name);
            }
            else
            {
                ErrorMessage = response.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowRegister()
    {
        _mainWindow.ShowRegistration();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
