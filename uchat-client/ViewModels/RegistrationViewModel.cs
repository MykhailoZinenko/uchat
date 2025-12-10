using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using uchat_client.Services;

namespace uchat_client.ViewModels;

public class RegistrationViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly IAuthService _authService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
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

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            _confirmPassword = value;
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

    public RelayCommand RegisterCommand { get; }
    public RelayCommand BackToLoginCommand { get; }

    public RegistrationViewModel(MainWindowViewModel mainWindow, IAuthService authService)
    {
        _mainWindow = mainWindow;
        _authService = authService;

        RegisterCommand = new RelayCommand(async () => await RegisterAsync());
        BackToLoginCommand = new RelayCommand(BackToLogin);
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var response = await _authService.RegisterAsync(Username, Password);

            Console.WriteLine($"Registration response: {response.Success} {response.Message}");

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
            ErrorMessage = $"Registration failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BackToLogin()
    {
        _mainWindow.ShowLogin();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
