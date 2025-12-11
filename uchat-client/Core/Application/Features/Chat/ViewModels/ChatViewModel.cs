using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.Models;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Features.Chat.ViewModels;

public class ChatViewModel : NavigableViewModelBase
{
    private readonly SidebarViewModel _sidebarViewModel;
    private readonly IMessageService _messageService;
    private readonly IAuthService _authService;
    private int _roomId;
    private string _roomName = string.Empty;
    private bool _isGlobal;
    private string _outgoingMessage = string.Empty;
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    public string RoomName
    {
        get => _roomName;
        set => SetProperty(ref _roomName, value);
    }

    public string Header => string.IsNullOrEmpty(RoomName)
        ? "Chat"
        : _isGlobal ? RoomName : $"Chat - {RoomName}";

    public string OutgoingMessage
    {
        get => _outgoingMessage;
        set => SetProperty(ref _outgoingMessage, value);
    }

    public ObservableCollection<ChatMessageViewModel> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    public SidebarViewModel SidebarViewModel => _sidebarViewModel;

    public ICommand SendCommand { get; }
    public ICommand EditMessageCommand { get; }
    public ICommand DeleteMessageCommand { get; }

    public ChatViewModel(
        INavigationService navigationService,
        SidebarViewModel sidebarViewModel,
        IMessageService messageService,
        IAuthService authService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _sidebarViewModel = sidebarViewModel;
        _messageService = messageService;
        _authService = authService;

        SendCommand = new RelayCommand(SendMessage);
        EditMessageCommand = new RelayCommand<ChatMessageViewModel>(EditMessage);
        DeleteMessageCommand = new RelayCommand<ChatMessageViewModel>(DeleteMessage);
    }

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        if (parameter is RoomNavigationContext room)
        {
            // Check if we're already viewing this room - if so, skip reload
            bool isAlreadyInRoom = _roomId == room.RoomId && Messages.Count > 0;

            if (isAlreadyInRoom)
            {
                Logger.LogDebug("Already viewing room {RoomId}, skipping reload", room.RoomId);
                await base.OnNavigatedToAsync(parameter);
                return;
            }

            // Unsubscribe from old room events if switching rooms
            if (_roomId != 0)
            {
                _messageService.MessageReceived -= OnMessageReceived;
                _messageService.MessageAcknowledged -= OnMessageAcknowledged;
                _messageService.DeliveryReceiptReceived -= OnDeliveryReceiptReceived;
            }

            _roomId = room.RoomId;
            RoomName = room.RoomName;
            _isGlobal = room.IsGlobal;
            _sidebarViewModel.IsOnSettingsPage = false;
            await _sidebarViewModel.EnsureRoomsLoadedAsync(forceReload: true);
            _sidebarViewModel.SetActiveRoom(_roomId);
            Logger.LogInformation("Navigated to room: {RoomName} ({RoomId})", room.RoomName, room.RoomId);

            await LoadMessagesAsync();

            // Listen for live messages and delivery updates
            _messageService.MessageReceived += OnMessageReceived;
            _messageService.MessageAcknowledged += OnMessageAcknowledged;
            _messageService.DeliveryReceiptReceived += OnDeliveryReceiptReceived;

            // Join the SignalR room so we receive broadcasts (not required for user-channel delivery, but harmless)
            await JoinRoomAsync();

            // Mark all visible messages as read
            await MarkVisibleMessagesAsReadAsync();
        }

        await base.OnNavigatedToAsync(parameter);
    }

    public override Task OnNavigatedFromAsync()
    {
        _messageService.MessageReceived -= OnMessageReceived;
        _messageService.MessageAcknowledged -= OnMessageAcknowledged;
        _messageService.DeliveryReceiptReceived -= OnDeliveryReceiptReceived;
        return base.OnNavigatedFromAsync();
    }

    private async Task LoadMessagesAsync()
    {
        var response = await _messageService.GetMessagesAsync(_roomId);
        if (response.Success && response.Data != null)
        {
            Messages = new ObservableCollection<ChatMessageViewModel>();
            foreach (var msg in response.Data)
            {
                Messages.Add(MapMessage(msg));
            }
        }
        else
        {
            Logger.LogWarning("Failed to load messages: {Message}", response.Message);
        }
    }

    private async Task JoinRoomAsync()
    {
        try
        {
            await _messageService.JoinRoomAsync(_roomId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to join room {RoomId}", _roomId);
        }
    }

    private ChatMessageViewModel MapMessage(MessageDto dto)
    {
        var isOutgoing = !string.IsNullOrWhiteSpace(dto.SenderUsername) &&
                         string.Equals(dto.SenderUsername, _authService.CurrentUsername, StringComparison.OrdinalIgnoreCase);

        Logger.LogDebug("MapMessage: Id={Id}, IsOutgoing={IsOutgoing}, DeliveryStatus={Status}",
            dto.Id, isOutgoing, dto.DeliveryStatus);

        return new ChatMessageViewModel
        {
            MessageId = dto.Id,
            Sender = dto.SenderUsername,
            Text = dto.Content,
            Time = dto.SentAt.ToLocalTime().ToShortTimeString(),
            IsOutgoing = isOutgoing,
            DeliveryStatus = dto.DeliveryStatus
        };
    }

    private async void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(OutgoingMessage))
            return;

        Logger.LogDebug("Sending message: {Message}", OutgoingMessage);

        var response = await _messageService.SendMessageAsync(_roomId, OutgoingMessage);
        if (response.Success && response.Data != null)
        {
            var vm = MapMessage(response.Data);
            vm.IsOutgoing = true;
            Messages.Add(vm);
            OutgoingMessage = string.Empty;
        }
        else
        {
            Logger.LogWarning("Failed to send message: {Message}", response.Message);
        }

    }

    private void OnMessageReceived(object? sender, MessageDto message)
    {
        if (message.RoomId != _roomId)
        {
            return;
        }

        var vm = MapMessage(message);
        Messages.Add(vm);
    }

    private void EditMessage(ChatMessageViewModel? message)
    {
        if (message == null) return;

        Logger.LogDebug("Edit message requested: {Text}", message.Text);
        // TODO: Implement message editing via MessageService
        message.IsEditing = false;
    }

    private void DeleteMessage(ChatMessageViewModel? message)
    {
        if (message == null) return;

        Logger.LogDebug("Delete message requested: {Text}", message.Text);
        // TODO: Implement message deletion via MessageService
        Messages.Remove(message);
    }

    // ==================== TELEGRAM-LIKE DELIVERY TRACKING ====================

    private void OnMessageAcknowledged(object? sender, MessageAckDto ack)
    {
        Logger.LogDebug("Message acknowledged: ClientId={ClientId} ServerId={ServerId}", ack.ClientMessageId, ack.ServerMessageId);
        // Message was acknowledged by server - could update UI here if needed
    }

    private void OnDeliveryReceiptReceived(object? sender, DeliveryReceiptDto receipt)
    {
        Logger.LogDebug("Delivery receipt: MessageId={MessageId} Status={Status}", receipt.MessageId, receipt.Status);

        // Update the delivery status of the message in our UI
        var message = Messages.FirstOrDefault(m => m.MessageId == receipt.MessageId);
        if (message != null)
        {
            Logger.LogDebug("Updating message {MessageId} status from {OldStatus} to {NewStatus}",
                receipt.MessageId, message.DeliveryStatus, receipt.Status);
            message.DeliveryStatus = receipt.Status;
        }
        else
        {
            Logger.LogWarning("Message {MessageId} not found in Messages collection for delivery receipt", receipt.MessageId);
        }
    }

    private async Task MarkVisibleMessagesAsReadAsync()
    {
        // Only mark messages that are not already read
        var unreadMessages = Messages
            .Where(m => !m.IsOutgoing && m.DeliveryStatus != uchat_common.Enums.DeliveryStatus.Read)
            .ToList();

        if (unreadMessages.Count == 0)
        {
            return;
        }

        Logger.LogDebug("Marking {Count} unread messages as read", unreadMessages.Count);

        // Mark all unread messages (fire and forget for better UX)
        foreach (var message in unreadMessages)
        {
            try
            {
                await _messageService.MarkMessageReadAsync(message.MessageId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to mark message as read: {MessageId}", message.MessageId);
            }
        }
    }
}

// Simple message ViewModel
public class ChatMessageViewModel : ObservableObject
{
    private string _sender = string.Empty;
    private string _text = string.Empty;
    private string _time = string.Empty;
    private bool _isOutgoing;
    private bool _isEditing;
    private uchat_common.Enums.DeliveryStatus _deliveryStatus = uchat_common.Enums.DeliveryStatus.Pending;
    private int _messageId;

    public int MessageId
    {
        get => _messageId;
        set => SetProperty(ref _messageId, value);
    }

    public string Sender
    {
        get => _sender;
        set => SetProperty(ref _sender, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    public bool IsOutgoing
    {
        get => _isOutgoing;
        set => SetProperty(ref _isOutgoing, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    // Telegram-like delivery status
    public uchat_common.Enums.DeliveryStatus DeliveryStatus
    {
        get => _deliveryStatus;
        set
        {
            if (SetProperty(ref _deliveryStatus, value))
            {
                // Notify UI that checkmark properties have changed
                OnPropertyChanged(nameof(ShowSingleCheckmark));
                OnPropertyChanged(nameof(ShowDoubleCheckmark));
                OnPropertyChanged(nameof(ShowBlueCheckmark));
            }
        }
    }

    // Checkmark visibility properties for UI binding
    public bool ShowSingleCheckmark => IsOutgoing && DeliveryStatus == uchat_common.Enums.DeliveryStatus.Sent;
    public bool ShowDoubleCheckmark => IsOutgoing && DeliveryStatus == uchat_common.Enums.DeliveryStatus.Delivered;
    public bool ShowBlueCheckmark => IsOutgoing && DeliveryStatus == uchat_common.Enums.DeliveryStatus.Read;
}
