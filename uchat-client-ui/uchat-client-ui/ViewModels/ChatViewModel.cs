using System;
using System.Collections.ObjectModel;

namespace uchat_client.ViewModels;

public class ChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
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

    public RelayCommand SendCommand { get; }

    private readonly string _username;

    public ChatViewModel(string username)
    {
        _username = username;
        Header = $"Chat – {_username}";
        SendCommand = new RelayCommand(Send);

        // Fake initial messages
        Messages.Add(new ChatMessage
        {
            Sender = "system",
            Text = "Welcome to uchat 👋",
            Time = DateTime.Now.ToShortTimeString()
        });
    }

    private void Send()
    {
        if (string.IsNullOrWhiteSpace(OutgoingMessage))
            return;

        Messages.Add(new ChatMessage
        {
            Sender = _username,
            Text = OutgoingMessage,
            Time = DateTime.Now.ToShortTimeString()
        });

        OutgoingMessage = string.Empty;
    }
}