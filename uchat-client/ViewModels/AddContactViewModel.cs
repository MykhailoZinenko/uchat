using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace uchat_client.ViewModels;

public class AddContactViewModel : ViewModelBase
{
    // Search for individual contacts
    private string _searchUsername = string.Empty;
    public string SearchUsername
    {
        get => _searchUsername;
        set
        {
            if (_searchUsername != value)
            {
                _searchUsername = value;
                OnPropertyChanged();
                FilterSearchResults();
            }
        }
    }

    public ObservableCollection<Contact> SearchResults { get; } = new();
    public ObservableCollection<Contact> AllContacts { get; } = new();

    // Create group
    private string _groupName = string.Empty;
    public string GroupName
    {
        get => _groupName;
        set
        {
            if (_groupName != value)
            {
                _groupName = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _groupPicturePath;
    public string? GroupPicturePath
    {
        get => _groupPicturePath;
        set
        {
            if (_groupPicturePath != value)
            {
                _groupPicturePath = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<Contact> RecentContacts { get; } = new();

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public RelayCommand CloseCommand { get; }
    public RelayCommand<Contact> OpenChatCommand { get; }
    public RelayCommand<Contact> ToggleSelectContactCommand { get; }
    public RelayCommand CreateGroupCommand { get; }
    public RelayCommand UploadGroupPictureCommand { get; }

    private readonly Action _closeAction;
    private readonly Action<string> _openChatAction;

    // Parameterless constructor for XAML
    public AddContactViewModel() : this(() => { }, _ => { })
    {
    }

    public AddContactViewModel(Action closeAction, Action<string> openChatAction)
    {
        _closeAction = closeAction;
        _openChatAction = openChatAction;

        CloseCommand = new RelayCommand(Close);
        OpenChatCommand = new RelayCommand<Contact>(OpenChat);
        ToggleSelectContactCommand = new RelayCommand<Contact>(ToggleSelectContact);
        CreateGroupCommand = new RelayCommand(CreateGroup);
        UploadGroupPictureCommand = new RelayCommand(UploadGroupPicture);

        // Initialize with fake data for demonstration
        InitializeFakeData();
    }

    private void InitializeFakeData()
    {
        // Add some fake contacts
        var contacts = new[]
        {
            "Joy", "Mäelise", "Jennie", "Alice", "Bob",
            "Charlie", "Diana", "Eve", "Frank", "Grace",
            "Charlie", "Diana"
        };

        foreach (var name in contacts)
        {
            var contact = new Contact { Username = name };
            AllContacts.Add(contact);
            RecentContacts.Add(contact);
        }

        FilterSearchResults();
    }

    private void FilterSearchResults()
    {
        SearchResults.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchUsername)
            ? AllContacts
            : AllContacts.Where(c => c.Username.Contains(SearchUsername, StringComparison.OrdinalIgnoreCase));

        foreach (var contact in filtered)
        {
            SearchResults.Add(contact);
        }
    }

    private void OpenChat(Contact? contact)
    {
        if (contact != null)
        {
            _openChatAction(contact.Username);
            _closeAction();
        }
    }

    private void ToggleSelectContact(Contact? contact)
    {
        if (contact != null)
        {
            contact.IsSelected = !contact.IsSelected;
            ErrorMessage = string.Empty;
        }
    }

    private void CreateGroup()
    {
        var selectedContacts = RecentContacts.Where(c => c.IsSelected).ToList();

        // Validation
        if (selectedContacts.Count < 2)
        {
            ErrorMessage = "Please select at least 2 contacts to create a group";
            return;
        }

        if (string.IsNullOrWhiteSpace(GroupName))
        {
            ErrorMessage = "Please enter a group name";
            return;
        }

        // TODO: Implement actual group creation logic
        // For now, just open a chat with the group name
        _openChatAction($"Group: {GroupName}");
        _closeAction();
    }

    private void UploadGroupPicture()
    {
        // This will be called from the code-behind when file is selected
        // The actual file picker logic is in the View
    }

    public void SetGroupPicturePath(string path)
    {
        GroupPicturePath = path;
    }

    private void Close()
    {
        _closeAction();
    }
}