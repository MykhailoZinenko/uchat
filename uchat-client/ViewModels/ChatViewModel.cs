using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace uchat_client.ViewModels;

public class ChatMessage : INotifyPropertyChanged
{
    private string _sender = string.Empty;
    private string _text = string.Empty;
    private string _time = string.Empty;
    private bool _isEditing = false;
    private bool _isOutgoing = false;

    public string Sender
    {
        get => _sender;
        set
        {
            if (_sender != value)
            {
                _sender = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    public string Time
    {
        get => _time;
        set
        {
            if (_time != value)
            {
                _time = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing != value)
            {
                _isEditing = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsOutgoing
    {
        get => _isOutgoing;
        set
        {
            if (_isOutgoing != value)
            {
                _isOutgoing = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class ChatViewModel : ViewModelBase
{
    public string Header { get; }
    public ObservableCollection<ChatMessage> Messages { get; } =
        new ObservableCollection<ChatMessage>();

    private string _outgoingMessage = string.Empty;
    public string OutgoingMessage
    {
        get => _outgoingMessage;
        set
        {
            if (_outgoingMessage != value)
            {
                _outgoingMessage = value;
                OnPropertyChanged();
            }
        }
    }

    private ChatMessage? _editingMessage;
    public ChatMessage? EditingMessage
    {
        get => _editingMessage;
        set
        {
            if (_editingMessage != value)
            {
                // Close previous editing popup
                if (_editingMessage != null)
                {
                    _editingMessage.IsEditing = false;
                }

                _editingMessage = value;

                // Open new editing popup
                if (_editingMessage != null)
                {
                    _editingMessage.IsEditing = true;
                }

                OnPropertyChanged();
            }
        }
    }

    public RelayCommand SendCommand { get; }
    public RelayCommand<ChatMessage> EditMessageCommand { get; }
    public RelayCommand<ChatMessage> DeleteMessageCommand { get; }
    public RelayCommand CloseEditingCommand { get; }

    private readonly string _usernameForMessages;
    public SidebarViewModel SidebarViewModel { get; }
    private readonly MainWindowViewModel _mainWindowViewModel;

    public ChatViewModel(string username, MainWindowViewModel mainWindowViewModel, SidebarViewModel sidebarViewModel)
    {
        _usernameForMessages = username;
        Header = $"Chat – {_usernameForMessages}";
        SendCommand = new RelayCommand(Send);
        EditMessageCommand = new RelayCommand<ChatMessage>(StartEditMessage);
        DeleteMessageCommand = new RelayCommand<ChatMessage>(DeleteMessage);
        CloseEditingCommand = new RelayCommand(CloseEditing);
        _mainWindowViewModel = mainWindowViewModel;
        SidebarViewModel = sidebarViewModel;

        // Fake initial messages
        Messages.Add(new ChatMessage
        {
            Sender = "system",
            Text = "Welcome to uchat 👋",
            Time = DateTime.Now.ToShortTimeString(),
            IsOutgoing = false
        });
    }

    public void Send()
    {
        if (string.IsNullOrWhiteSpace(OutgoingMessage))
            return;

        Messages.Add(new ChatMessage
        {
            Sender = _usernameForMessages,
            Text = OutgoingMessage,
            Time = DateTime.Now.ToShortTimeString(),
            IsOutgoing = true
        });

        OutgoingMessage = string.Empty;
    }
    
    public void ToggleEditingPopup(ChatMessage message)
    {
        if (message.Sender != _usernameForMessages)
            return;

        if (EditingMessage == message)
        {
            EditingMessage = null;
        }
        else
        {
            EditingMessage = message;
        }
    }

    private void StartEditMessage(ChatMessage? message)
    {
        if (message != null && message.Sender == _usernameForMessages)
        {
            EditingMessage = message;
        }
    }

    private void DeleteMessage(ChatMessage? message)
    {
        if (message != null && message.Sender == _usernameForMessages)
        {
            Messages.Remove(message);
            EditingMessage = null;
        }
    }

    private void CloseEditing()
    {
        EditingMessage = null;
    }
}