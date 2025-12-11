using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;

namespace uchat_client.Core.Application.Features.Contacts.ViewModels;

public class ContactsViewModel : NavigableViewModelBase
{
    private ObservableCollection<ContactItemViewModel> _contacts = new();
    private string _searchText = string.Empty;

    public ObservableCollection<ContactItemViewModel> Contacts
    {
        get => _contacts;
        set => SetProperty(ref _contacts, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand AddContactCommand { get; }

    public ContactsViewModel(
        INavigationService navigationService,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        SearchCommand = new RelayCommand(PerformSearch);
        AddContactCommand = new RelayCommand(() => NavigationService.NavigateToAddContact());
    }

    private void PerformSearch()
    {
        Logger.LogDebug("Searching contacts: {SearchText}", SearchText);
        // TODO: Implement contact search
    }
}

public class ContactItemViewModel : ObservableObject
{
    private int _userId;
    private string _username = string.Empty;
    private bool _isSelected;
    private bool _isOnline;

    public int UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }
}
