using System;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface INavigationService
{
    void NavigateToLogin();
    void NavigateToRegistration();
    void NavigateToChat(int roomId, string roomName, bool isGlobal);
    void NavigateToSettings();
    void NavigateToMyAccount();
    void NavigateToSessions();
    void NavigateToSoundNotifications();
    void NavigateToContacts();
    void NavigateToAddContact();
    void GoBack();
    bool CanGoBack { get; }
    event EventHandler<NavigationEventArgs>? Navigated;
}

public class NavigationEventArgs : EventArgs
{
    public object ViewModel { get; }
    public object? Parameter { get; }

    public NavigationEventArgs(object viewModel, object? parameter)
    {
        ViewModel = viewModel;
        Parameter = parameter;
    }
}
