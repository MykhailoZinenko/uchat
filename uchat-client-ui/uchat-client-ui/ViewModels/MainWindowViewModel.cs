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
        var loginVm = new LoginViewModel(this);
        CurrentView = loginVm;
    }

    public void ShowChat(string username)
    {
        var chatVm = new ChatViewModel(username);
        CurrentView = chatVm;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}