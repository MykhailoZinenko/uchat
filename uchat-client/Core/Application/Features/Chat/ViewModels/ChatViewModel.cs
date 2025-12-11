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
using uchat_common.Enums;

namespace uchat_client.Core.Application.Features.Chat.ViewModels;

public class ChatViewModel : NavigableViewModelBase
{
    private const int PageSize = 50;
    private readonly SidebarViewModel _sidebarViewModel;
    private readonly IMessageService _messageService;
    private readonly IAuthService _authService;
    private readonly IRoomService _roomService;
    private int _roomId;
    private string _roomName = string.Empty;
    private int? _roomCreatorUserId;
    private bool _isGlobal;
    private string _outgoingMessage = string.Empty;
    private ObservableCollection<ChatMessageViewModel> _messages = new();
    private bool _isRenaming;
    private string _editableRoomName = string.Empty;
    private bool _isLoadingMore;
    private bool _hasMoreMessages = true;

    public bool IsLoadingMore
    {
        get => _isLoadingMore;
        private set => SetProperty(ref _isLoadingMore, value);
    }

    public string RoomName
    {
        get => _roomName;
        set => SetProperty(ref _roomName, value);
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (SetProperty(ref _isRenaming, value))
            {
                RefreshRenameCommands();
            }
        }
    }

    public string EditableRoomName
    {
        get => _editableRoomName;
        set
        {
            if (SetProperty(ref _editableRoomName, value))
            {
                RefreshRenameCommands();
            }
        }
    }

    public bool CanRename => !_isGlobal && _roomCreatorUserId.HasValue && _authService.CurrentUserId == _roomCreatorUserId;

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
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteMessageCommand { get; }
    public ICommand StartRenameCommand { get; }
    public ICommand SaveRenameCommand { get; }
    public ICommand CancelRenameCommand { get; }
    public ICommand LeaveRoomCommand { get; }

    public ChatViewModel(
        INavigationService navigationService,
        SidebarViewModel sidebarViewModel,
        IMessageService messageService,
        IAuthService authService,
        IRoomService roomService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _sidebarViewModel = sidebarViewModel;
        _messageService = messageService;
        _authService = authService;
        _roomService = roomService;

        SendCommand = new RelayCommand(SendMessage);
        EditMessageCommand = new RelayCommand<ChatMessageViewModel>(StartEditMessage);
        SaveEditCommand = new RelayCommand<ChatMessageViewModel>(SaveEditMessage);
        CancelEditCommand = new RelayCommand<ChatMessageViewModel>(CancelEditMessage);
        DeleteMessageCommand = new RelayCommand<ChatMessageViewModel>(DeleteMessage);
        StartRenameCommand = new RelayCommand(StartRename, () => CanRename);
        SaveRenameCommand = new AsyncRelayCommand(SaveRenameAsync, () => CanRename && IsRenaming && !string.IsNullOrWhiteSpace(EditableRoomName));
        CancelRenameCommand = new RelayCommand(CancelRename);
        LeaveRoomCommand = new AsyncRelayCommand(LeaveRoomAsync);
    }

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        if (parameter is RoomNavigationContext room)
        {
            bool isAlreadyInRoom = _roomId == room.RoomId && Messages.Count > 0;

            if (isAlreadyInRoom)
            {
                Logger.LogDebug("Already viewing room {RoomId}, skipping reload", room.RoomId);
                await base.OnNavigatedToAsync(parameter);
                return;
            }

            if (_roomId != 0)
            {
                _messageService.MessageReceived -= OnMessageReceived;
                _messageService.MessageEdited -= OnMessageEdited;
                _messageService.MessageDeleted -= OnMessageDeleted;
            }

            _roomId = room.RoomId;
            RoomName = room.RoomName;
            _roomCreatorUserId = room.CreatedByUserId;
            _isGlobal = room.IsGlobal;
            OnPropertyChanged(nameof(CanRename));
            RefreshRenameCommands();
            _sidebarViewModel.IsOnSettingsPage = false;
            await _sidebarViewModel.EnsureRoomsLoadedAsync(forceReload: true);
            _sidebarViewModel.SetActiveRoom(_roomId);
            Logger.LogInformation("Navigated to room: {RoomName} ({RoomId})", room.RoomName, room.RoomId);

            await LoadMessagesAsync();

            _messageService.MessageReceived += OnMessageReceived;
            _messageService.MessageEdited += OnMessageEdited;
            _messageService.MessageDeleted += OnMessageDeleted;

            await JoinRoomAsync();
        }

        await base.OnNavigatedToAsync(parameter);
    }

    public override Task OnNavigatedFromAsync()
    {
        _messageService.MessageReceived -= OnMessageReceived;
        _messageService.MessageEdited -= OnMessageEdited;
        _messageService.MessageDeleted -= OnMessageDeleted;
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

            _hasMoreMessages = response.Data.Count == PageSize;
        }
        else
        {
            Logger.LogWarning("Failed to load messages: {Message}", response.Message);
            _hasMoreMessages = false;
        }
    }

    public async Task LoadMoreMessagesAsync()
    {
        if (IsLoadingMore || !_hasMoreMessages)
        {
            return;
        }

        IsLoadingMore = true;

        try
        {
            var oldestMessageId = Messages.FirstOrDefault()?.MessageId;
            if (oldestMessageId == null)
            {
                return;
            }

            Logger.LogDebug("Loading more messages before messageId: {MessageId}", oldestMessageId);

            var response = await _messageService.GetMessagesAsync(_roomId, oldestMessageId);
            if (response.Success && response.Data != null && response.Data.Count > 0)
            {
                var newMessages = new List<ChatMessageViewModel>();
                foreach (var msg in response.Data)
                {
                    // Avoid duplicates if the server returns overlapping messages
                    if (Messages.Any(m => m.MessageId == msg.Id))
                    {
                        continue;
                    }

                    newMessages.Add(MapMessage(msg));
                }

                for (int i = newMessages.Count - 1; i >= 0; i--)
                {
                    Messages.Insert(0, newMessages[i]);
                }

                _hasMoreMessages = response.Data.Count == PageSize;

                Logger.LogDebug("Loaded {Count} more messages", response.Data.Count);
            }
            else
            {
                _hasMoreMessages = false;
                Logger.LogDebug("No more messages to load");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading more messages");
        }
        finally
        {
            IsLoadingMore = false;
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

        var isService = dto.MessageType == MessageType.Service;

        return new ChatMessageViewModel
        {
            MessageId = dto.Id,
            Sender = dto.SenderUsername,
            Text = dto.Content,
            Time = dto.SentAt.ToLocalTime().ToShortTimeString(),
            IsOutgoing = isOutgoing,
            IsService = isService,
            ServiceAction = dto.ServiceAction
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

        // Update room title if a rename system message arrives for current room
        if (message.MessageType == MessageType.Service && message.ServiceAction == ServiceAction.RoomRenamed)
        {
            RoomName = message.Content;
            EditableRoomName = RoomName;
        }
    }

    private void OnMessageEdited(object? sender, (int messageId, string newContent) edit)
    {
        var message = Messages.FirstOrDefault(m => m.MessageId == edit.messageId);
        if (message != null)
        {
            message.Text = edit.newContent;
            message.IsEdited = true;
            message.IsEditing = false;
        }
    }

    private void OnMessageDeleted(object? sender, int messageId)
    {
        var message = Messages.FirstOrDefault(m => m.MessageId == messageId);
        if (message != null)
        {
            Messages.Remove(message);
        }
    }

    public void ToggleContextMenu(ChatMessageViewModel? message)
    {
        if (message == null || !message.IsOutgoing || message.IsService) return;

        foreach (var msg in Messages)
        {
            if (msg != message)
            {
                msg.ShowContextMenu = false;
            }
        }

        message.ShowContextMenu = !message.ShowContextMenu;
    }

    private void StartEditMessage(ChatMessageViewModel? message)
    {
        if (message == null || !message.IsOutgoing) return;

        message.ShowContextMenu = false;
        message.EditedText = message.Text;
        message.IsEditing = true;
    }

    private async void SaveEditMessage(ChatMessageViewModel? message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.EditedText)) return;

        Logger.LogDebug("Saving edited message: {MessageId}", message.MessageId);

        try
        {
            var response = await _messageService.EditMessageAsync(message.MessageId, message.EditedText);
            if (response.Success)
            {
                message.IsEditing = false;
            }
            else
            {
                Logger.LogWarning("Failed to edit message: {Message}", response.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error editing message");
        }
    }

    private void CancelEditMessage(ChatMessageViewModel? message)
    {
        if (message == null) return;

        message.IsEditing = false;
        message.EditedText = string.Empty;
    }

    private async void DeleteMessage(ChatMessageViewModel? message)
    {
        if (message == null || message.IsService) return;

        Logger.LogDebug("Delete message requested: {Text}", message.Text);

        try
        {
            var response = await _messageService.DeleteMessageAsync(message.MessageId);
            if (response.Success)
            {
                Messages.Remove(message);
            }
            else
            {
                Logger.LogWarning("Failed to delete message: {Message}", response.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting message");
        }
    }

    private void StartRename()
    {
        if (!CanRename) return;
        IsRenaming = true;
        EditableRoomName = RoomName;
        RefreshRenameCommands();
    }

    private async Task SaveRenameAsync()
    {
        if (!CanRename || string.IsNullOrWhiteSpace(EditableRoomName))
            return;

        var response = await _roomService.UpdateRoomAsync(_roomId, EditableRoomName, null, null);
        if (response.Success && response.Data != null)
        {
            RoomName = response.Data.RoomName ?? EditableRoomName;
            IsRenaming = false;
        }
        else
        {
            Logger.LogWarning("Failed to rename room: {Message}", response.Message);
        }

        RefreshRenameCommands();
    }

    private void CancelRename()
    {
        IsRenaming = false;
        EditableRoomName = RoomName;
        RefreshRenameCommands();
    }

    private async Task LeaveRoomAsync()
    {
        var response = await _roomService.LeaveRoomAsync(_roomId);
        if (!response.Success)
        {
            Logger.LogWarning("Failed to leave room: {Message}", response.Message);
            return;
        }

        await _sidebarViewModel.EnsureRoomsLoadedAsync(forceReload: true);
        var fallback = _sidebarViewModel.GetDefaultRoom();
        if (fallback != null)
        {
            NavigationService.NavigateToChat(fallback.Id, fallback.DisplayName, fallback.IsGlobal, fallback.CreatedByUserId);
        }
        else
        {
            NavigationService.NavigateToSettings();
        }
    }

    private void RefreshRenameCommands()
    {
        (StartRenameCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (SaveRenameCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }
}

public class ChatMessageViewModel : ObservableObject
{
    private string _sender = string.Empty;
    private string _text = string.Empty;
    private string _editedText = string.Empty;
    private string _time = string.Empty;
    private bool _isOutgoing;
    private bool _isEditing;
    private bool _isEdited;
    private bool _showContextMenu;
    private int _messageId;
    private bool _isService;
    private ServiceAction? _serviceAction;

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

    public string EditedText
    {
        get => _editedText;
        set => SetProperty(ref _editedText, value);
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

    public bool IsEdited
    {
        get => _isEdited;
        set => SetProperty(ref _isEdited, value);
    }

    public bool ShowContextMenu
    {
        get => _showContextMenu;
        set => SetProperty(ref _showContextMenu, value);
    }

    public bool IsService
    {
        get => _isService;
        set
        {
            if (SetProperty(ref _isService, value))
            {
                OnPropertyChanged(nameof(ShowBubble));
            }
        }
    }

    public ServiceAction? ServiceAction
    {
        get => _serviceAction;
        set => SetProperty(ref _serviceAction, value);
    }

    public bool ShowBubble => !IsService;
}
