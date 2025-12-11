using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;

namespace uchat_client.Core.Application.Features.Contacts.ViewModels;

public class AddContactViewModel : NavigableViewModelBase
{
    private readonly IContactService _contactService;
    private readonly IValidationService _validationService;
    private readonly IRoomService _roomService;
    private Action? _closeAction;
    private string _searchQuery = string.Empty;
    private string _searchUsername = string.Empty;
    private ObservableCollection<ContactItemViewModel> _searchResults = new();
    private ObservableCollection<ContactItemViewModel> _recentContacts = new();
    private string _groupName = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public string SearchUsername
    {
        get => _searchUsername;
        set => SetProperty(ref _searchUsername, value);
    }

    public ObservableCollection<ContactItemViewModel> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    public ObservableCollection<ContactItemViewModel> RecentContacts
    {
        get => _recentContacts;
        set => SetProperty(ref _recentContacts, value);
    }

    public string GroupName
    {
        get => _groupName;
        set => SetProperty(ref _groupName, value);
    }

    public new string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrEmpty(value);
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand AddContactCommand { get; }
    public ICommand CreateGroupCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand OpenChatCommand { get; }
    public ICommand ToggleSelectContactCommand { get; }

    public AddContactViewModel(
        INavigationService navigationService,
        IContactService contactService,
        IValidationService validationService,
        IRoomService roomService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _contactService = contactService;
        _validationService = validationService;
        _roomService = roomService;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        AddContactCommand = new AsyncRelayCommand(AddContactAsync);
        CreateGroupCommand = new AsyncRelayCommand(CreateGroupAsync);
        CloseCommand = new RelayCommand(Close);
        OpenChatCommand = new RelayCommand<ContactItemViewModel>(OpenChat);
        ToggleSelectContactCommand = new RelayCommand<ContactItemViewModel>(ToggleSelectContact);
    }

    public void SetCloseAction(Action closeAction)
    {
        _closeAction = closeAction;
    }

    private void Close()
    {
        _closeAction?.Invoke();
    }

    private async Task SearchAsync()
    {
        var validation = _validationService.ValidateContactUsername(SearchQuery);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.ErrorMessage;
            return;
        }

        await ExecuteAsync(async () =>
        {
            Logger.LogDebug("Searching for users: {Query}", SearchQuery);
            var response = await _contactService.SearchUsersAsync(SearchQuery);

            if (response.Success && response.Data != null)
            {
                SearchResults.Clear();
                foreach (var user in response.Data)
                {
                    SearchResults.Add(new ContactItemViewModel
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        IsOnline = user.IsOnline
                    });
                }

                if (SearchResults.Count == 0)
                {
                    ErrorMessage = "No users found";
                }
                else
                {
                    ErrorMessage = string.Empty;
                }
            }
            else
            {
                ErrorMessage = response.Message;
            }
        });
    }

    private async Task AddContactAsync()
    {
        await ExecuteAsync(async () =>
        {
            Logger.LogDebug("Adding contact: {Username}", SearchQuery);
            var response = await _contactService.AddContactAsync(SearchQuery);

            if (response.Success)
            {
                ErrorMessage = string.Empty;
                // Optionally open chat with the contact
                // Close();
            }
            else
            {
                ErrorMessage = response.Message;
            }
        });
    }

    private async Task CreateGroupAsync()
    {
        // Validate group name
        if (string.IsNullOrWhiteSpace(GroupName))
        {
            ErrorMessage = "Please enter a group name";
            return;
        }

        // Get selected members
        var selectedMembers = SearchResults.Where(c => c.IsSelected).ToList();
        if (selectedMembers.Count == 0)
        {
            ErrorMessage = "Please select at least one member";
            return;
        }

        await ExecuteAsync(async () =>
        {
            Logger.LogDebug("Creating group: {GroupName} with {Count} members", GroupName, selectedMembers.Count);

            // Create the group room
            var createResponse = await _roomService.CreateRoomAsync("Group", GroupName, null);

            if (!createResponse.Success || createResponse.Data == null)
            {
                ErrorMessage = createResponse.Message;
                return;
            }

            var roomId = createResponse.Data.Id;
            Logger.LogDebug("Group created: RoomId={RoomId}", roomId);

            // Add selected members to the group
            var memberIds = selectedMembers.Select(m => m.UserId).ToList();
            var addMembersResponse = await _roomService.AddRoomMembersAsync(roomId, memberIds);

            if (!addMembersResponse.Success)
            {
                ErrorMessage = $"Group created but failed to add members: {addMembersResponse.Message}";
                return;
            }

            Logger.LogInformation("Group created successfully: {GroupName} with {Count} members", GroupName, memberIds.Count);
            ErrorMessage = string.Empty;

            // Navigate to the newly created group
            Close();
            NavigationService.NavigateToChat(roomId, GroupName, false);
        });
    }

    private async void OpenChat(ContactItemViewModel? contact)
    {
        if (contact == null) return;

        Logger.LogDebug("Opening chat with: {Username}", contact.Username);
        Close();
        var roomsResponse = await _roomService.GetAccessibleRoomsAsync();
        var room = roomsResponse.Success && roomsResponse.Data != null
            ? roomsResponse.Data
                .OrderByDescending(r => r.IsGlobal)
                .ThenBy(r => r.RoomName ?? string.Empty)
                .FirstOrDefault()
            : null;

        if (room == null)
        {
            Logger.LogWarning("No rooms available when opening chat from AddContact");
            return;
        }

        NavigationService.NavigateToChat(room.Id, room.RoomName ?? "Room", room.IsGlobal);
    }

    private void ToggleSelectContact(ContactItemViewModel? contact)
    {
        if (contact == null) return;

        contact.IsSelected = !contact.IsSelected;
        Logger.LogDebug("Toggled contact selection: {Username} = {IsSelected}", contact.Username, contact.IsSelected);
    }
}
