using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace uchat_client.ViewModels;

public class SidebarViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private bool _isOnSettingsPage;

    public bool IsOnSettingsPage
    {
        get => _isOnSettingsPage;
        set
        {
            if (_isOnSettingsPage != value)
            {
                _isOnSettingsPage = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _username = "Username";
    public string Username
    {
        get => _username;
        set
        {
            // Limit to 16 characters
            var newValue = value.Length > 16 ? value.Substring(0, 16) : value;
            if (_username != newValue)
            {
                _username = newValue;
                OnPropertyChanged();
            }
        }
    }

    private string _bio = "Bio goes here…";
    public string Bio
    {
        get => _bio;
        set
        {
            // Limit to 80 characters
            var newValue = value.Length > 80 ? value.Substring(0, 80) : value;
            if (_bio != newValue)
            {
                _bio = newValue;
                OnPropertyChanged();
            }
        }
    }

    private bool _isEditingProfile = false;
    public bool IsEditingProfile
    {
        get => _isEditingProfile;
        set
        {
            if (_isEditingProfile != value)
            {
                _isEditingProfile = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _profilePicturePath;
    public string? ProfilePicturePath
    {
        get => _profilePicturePath;
        set
        {
            if (_profilePicturePath != value)
            {
                _profilePicturePath = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isSidebarCollapsed = false;
    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (_isSidebarCollapsed != value)
            {
                _isSidebarCollapsed = value;
                OnPropertyChanged();
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }
    }
    
    private bool _isAddContactOpen = false;
    public bool IsAddContactOpen
    {
        get => _isAddContactOpen;
        set
        {
            if (_isAddContactOpen != value)
            {
                _isAddContactOpen = value;
                OnPropertyChanged();
            }
        }
    }

    private AddContactViewModel? _addContactViewModel;
    public AddContactViewModel? AddContactViewModel
    {
        get => _addContactViewModel;
        set
        {
            if (_addContactViewModel != value)
            {
                _addContactViewModel = value;
                OnPropertyChanged();
            }
        }
    }

    public RelayCommand BackCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand ToggleEditProfileCommand { get; }
    public RelayCommand ToggleSidebarCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand OpenAddContactCommand { get; }
    public RelayCommand OpenContactFriendListCommand { get; } // NEW

    public SidebarViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        ToggleEditProfileCommand = new RelayCommand(ToggleEditProfile);
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        SearchCommand = new RelayCommand(PerformSearch);
        OpenAddContactCommand = new RelayCommand(OpenAddContact);
        OpenContactFriendListCommand = new RelayCommand(OpenContactFriendList); // NEW

        BackCommand = new RelayCommand(GoBack);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
    }

    private void GoBack()
    {
        _mainWindowViewModel.ShowChat(Username);
    }

    private void OpenSettings()
    {
        _mainWindowViewModel.ShowSettings();
    }

    private void ToggleEditProfile()
    {
        IsEditingProfile = !IsEditingProfile;
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    public void PerformSearch()
    {
        // TODO: Implement contact filtering based on SearchText
        // For now, this is a placeholder
    }

    private void OpenAddContact()
    {
        AddContactViewModel = new AddContactViewModel(CloseAddContact, OpenChatWithContact);
        IsAddContactOpen = true;
    }

    private void CloseAddContact()
    {
        IsAddContactOpen = false;
        AddContactViewModel = null;
    }

    private void OpenChatWithContact(string contactName)
    {
        // TODO: Implement switching to chat with specific contact
        // For now, just add a system message
    }

    // NEW METHOD
    private void OpenContactFriendList()
    {
        _mainWindowViewModel.ShowContactFriendList();
    }
}