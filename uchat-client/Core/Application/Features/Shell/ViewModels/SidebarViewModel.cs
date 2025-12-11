using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Contacts.ViewModels;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_client.Core.Application.Features.Shell.ViewModels;

public class SidebarViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IContactService _contactService;
    private readonly IRoomService _roomService;
    private readonly IMessageService _messageService;
    private readonly IAuthService _authService;
    private readonly AddContactViewModel _addContactViewModel;
    private bool _isOnSettingsPage;
    private bool _isSidebarCollapsed;
    private string _searchText = string.Empty;
    private bool _isAddContactOpen;
    private bool _hasLoadedRooms;
    private bool _suppressSelectionNavigation;
    private RoomItemViewModel? _selectedRoom;
    private readonly ObservableCollection<RoomItemViewModel> _rooms = new();
    private readonly ObservableCollection<RoomItemViewModel> _filteredRooms = new();

    public bool IsOnSettingsPage
    {
        get => _isOnSettingsPage;
        set => SetProperty(ref _isOnSettingsPage, value);
    }

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsAddContactOpen
    {
        get => _isAddContactOpen;
        set => SetProperty(ref _isAddContactOpen, value);
    }

    public RoomItemViewModel? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (SetProperty(ref _selectedRoom, value) && value != null)
            {
                UpdateSelectionState(value);
                if (_suppressSelectionNavigation)
                {
                    return;
                }

                NavigateToRoom(value);
            }
        }
    }

    public ReadOnlyObservableCollection<RoomItemViewModel> Rooms { get; }
    public ReadOnlyObservableCollection<RoomItemViewModel> FilteredRooms { get; }

    public AddContactViewModel AddContactViewModel => _addContactViewModel;

    public int RoomCount => _rooms.Count;

    public RoomItemViewModel? GetDefaultRoom()
    {
        return _rooms.FirstOrDefault();
    }

    public ICommand BackCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ToggleSidebarCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand OpenAddContactCommand { get; }
    public IAsyncRelayCommand LoadRoomsCommand { get; }
    public ICommand SelectRoomCommand { get; }

    public SidebarViewModel(
        INavigationService navigationService,
        IContactService contactService,
        AddContactViewModel addContactViewModel,
        IMessageService messageService,
        IAuthService authService,
        IRoomService roomService,
        ILoggingService logger)
        : base(logger)
    {
        _navigationService = navigationService;
        _contactService = contactService;
        _roomService = roomService;
        _messageService = messageService;
        _authService = authService;
        _addContactViewModel = addContactViewModel;
        _addContactViewModel.SetCloseAction(() => IsAddContactOpen = false);

        Rooms = new ReadOnlyObservableCollection<RoomItemViewModel>(_rooms);
        FilteredRooms = new ReadOnlyObservableCollection<RoomItemViewModel>(_filteredRooms);

        BackCommand = new RelayCommand(GoBack);
        OpenSettingsCommand = new RelayCommand(() => _navigationService.NavigateToSettings());
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        SearchCommand = new RelayCommand(ApplyFilter);
        OpenAddContactCommand = new RelayCommand(OpenAddContact);
        LoadRoomsCommand = new AsyncRelayCommand(LoadRoomsAsync);
        SelectRoomCommand = new RelayCommand<RoomItemViewModel>(room =>
        {
            if (room != null)
            {
                SelectedRoom = room;
            }
        });

        // Only start updates after authentication; otherwise wait for login/registration flow
    }

    public async Task EnsureRoomsLoadedAsync(bool forceReload = false)
    {
        if (_hasLoadedRooms && !forceReload)
        {
            ApplyFilter();
            return;
        }

        await LoadRoomsCommand.ExecuteAsync(null);
    }

    public void SetActiveRoom(int roomId)
    {
        var match = _rooms.FirstOrDefault(r => r.Id == roomId);

        if (match != null)
        {
            _suppressSelectionNavigation = true;
            SelectedRoom = match;
            _suppressSelectionNavigation = false;
        }
    }

    private void GoBack()
    {
        var room = SelectedRoom ?? GetDefaultRoom();
        if (room != null)
        {
            _navigationService.NavigateToChat(room.Id, room.DisplayName, room.IsGlobal);
        }
    }

    private void ApplyFilter()
    {
        _filteredRooms.Clear();

        var query = SearchText?.Trim() ?? string.Empty;
        var matches = string.IsNullOrWhiteSpace(query)
            ? _rooms
            : _rooms.Where(r =>
                (!string.IsNullOrWhiteSpace(r.Name) && r.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.Description) && r.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));

        foreach (var room in matches)
        {
            _filteredRooms.Add(room);
        }

        // If the current selection is not present after filtering, clear it without navigating.
        if (SelectedRoom is not null && !_filteredRooms.Contains(SelectedRoom))
        {
            _suppressSelectionNavigation = true;
            SelectedRoom = null;
            _suppressSelectionNavigation = false;
        }
    }

    private async Task LoadRoomsAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (!_authService.IsAuthenticated || string.IsNullOrEmpty(_authService.SessionToken))
            {
                Logger.LogWarning("LoadRoomsAsync skipped: not authenticated");
                return;
            }

            _rooms.Clear();

            Logger.LogInformation("Loading rooms...");

            var response = await _roomService.GetAccessibleRoomsAsync();
            if (response.Success && response.Data is { Count: > 0 })
            {
                Logger.LogInformation("Rooms fetched: Count={Count}", response.Data.Count);
                foreach (var room in response.Data)
                {
                    _rooms.Add(MapToRoom(room));
                }
            }
            else
            {
                Logger.LogWarning("No rooms returned from server. Success={Success} Message={Message}", response.Success, response.Message);
            }

            // Ensure global room is first
            var ordered = _rooms.OrderByDescending(r => r.IsGlobal).ThenBy(r => r.Name).ToList();
            _rooms.Clear();
            foreach (var room in ordered)
            {
                _rooms.Add(room);
            }

            Logger.LogInformation("Rooms after ordering: Count={Count}", _rooms.Count);

            _hasLoadedRooms = true;
            ApplyFilter();
        }, showBusy: false);
    }

    private RoomItemViewModel MapToRoom(RoomDto room)
    {
        return new RoomItemViewModel
        {
            Id = room.Id,
            Name = string.IsNullOrWhiteSpace(room.RoomName) ? "Room" : room.RoomName,
            Description = room.RoomDescription ?? string.Empty,
            IsGlobal = room.IsGlobal,
            LastActivityAt = room.CreatedAt
        };
    }

    private void UpdateSelectionState(RoomItemViewModel selected)
    {
        foreach (var room in _rooms)
        {
            room.IsSelected = room == selected;
        }
    }

    private void NavigateToRoom(RoomItemViewModel room)
    {
        // Defer navigation to allow ListBox selection to settle before view swap
        Dispatcher.UIThread.Post(() => _navigationService.NavigateToChat(room.Id, room.Name, room.IsGlobal));
    }

    private void OpenAddContact()
    {
        IsAddContactOpen = true;
    }
}

public class RoomItemViewModel : ObservableObject
{
    private int _id;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool _isGlobal;
    private bool _isSelected;
    private int _unreadCount;
    private DateTime _lastActivityAt = DateTime.MinValue;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsGlobal
    {
        get => _isGlobal;
        set => SetProperty(ref _isGlobal, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Room" : Name;

    public int UnreadCount
    {
        get => _unreadCount;
        set => SetProperty(ref _unreadCount, value);
    }

    public DateTime LastActivityAt
    {
        get => _lastActivityAt;
        set => SetProperty(ref _lastActivityAt, value);
    }
}
