using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace uchat_client.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView != value)
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel()
    {
        ShowLogin();
    }

    public void ShowChat(string username)
    {
        CurrentView = new ChatViewModel(username);
    }

    public void ShowLogin()
    {
        CurrentView = new LoginViewModel(this);
    }

    public void ShowRegistration()
    {
        CurrentView = new RegistrationViewModel(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}