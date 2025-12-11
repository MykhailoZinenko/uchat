using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Common.Models;

namespace uchat_client.Presentation.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<object> _navigationStack = new();
    private object? _currentViewModel;

    public event EventHandler<NavigationEventArgs>? Navigated;
    public bool CanGoBack => _navigationStack.Count > 0;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Log.Information("NavigationService initialized");
    }

    public void NavigateToLogin()
    {
        Log.Information("NavigateToLogin called");
        NavigateTo("uchat_client.Core.Application.Features.Authentication.ViewModels.LoginViewModel");
    }

    public void NavigateToRegistration()
    {
        NavigateTo("uchat_client.Core.Application.Features.Authentication.ViewModels.RegistrationViewModel");
    }

    public void NavigateToChat(int roomId, string roomName, bool isGlobal)
    {
        var roomContext = new RoomNavigationContext(roomId, roomName, isGlobal);
        NavigateTo("uchat_client.Core.Application.Features.Chat.ViewModels.ChatViewModel", roomContext);
    }

    public void NavigateToSettings()
    {
        NavigateTo("uchat_client.Core.Application.Features.Settings.ViewModels.SettingsViewModel");
    }

    public void NavigateToMyAccount()
    {
        NavigateTo("uchat_client.Core.Application.Features.Settings.ViewModels.MyAccountViewModel");
    }

    public void NavigateToSessions()
    {
        NavigateTo("uchat_client.Core.Application.Features.Settings.ViewModels.SessionsViewModel");
    }

    public void NavigateToSoundNotifications()
    {
        NavigateTo("uchat_client.Core.Application.Features.Settings.ViewModels.SoundNotificationsViewModel");
    }

    public void NavigateToContacts()
    {
        NavigateTo("uchat_client.Core.Application.Features.Contacts.ViewModels.ContactsViewModel");
    }

    public void NavigateToAddContact()
    {
        NavigateTo("uchat_client.Core.Application.Features.Contacts.ViewModels.AddContactViewModel");
    }

    public void GoBack()
    {
        if (_navigationStack.Count > 0)
        {
            var previousViewModel = _navigationStack.Pop();
            NavigateToInternal(previousViewModel, parameter: null, addToStack: false);
        }
    }

    private void NavigateTo(string viewModelTypeName, object? parameter = null)
    {
        try
        {
            Log.Information("Attempting to navigate to: {ViewModelType}", viewModelTypeName);

            // Try to load the type from the assembly
            var viewModelType = Type.GetType($"{viewModelTypeName}, uchat");
            if (viewModelType == null)
            {
                Log.Error("ViewModel type not found: {ViewModelType}", viewModelTypeName);
                throw new InvalidOperationException($"ViewModel type not found: {viewModelTypeName}");
            }

            Log.Information("Type found, resolving from DI: {ViewModelType}", viewModelType.Name);
            var viewModel = _serviceProvider.GetRequiredService(viewModelType);
            Log.Information("ViewModel resolved, navigating internally");
            NavigateToInternal(viewModel, parameter, addToStack: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Navigation failed for {ViewModelType}", viewModelTypeName);
            throw;
        }
    }

    private async void NavigateToInternal(object viewModel, object? parameter = null, bool addToStack = true)
    {
        try
        {
            Log.Information("NavigateToInternal called with {ViewModelType}", viewModel.GetType().Name);

            // Call lifecycle methods on old ViewModel
            if (_currentViewModel is NavigableViewModelBase currentNavigable)
            {
                await currentNavigable.OnNavigatedFromAsync();
            }

            // Add current to stack if navigating forward
            if (addToStack && _currentViewModel != null)
            {
                _navigationStack.Push(_currentViewModel);
            }

            // Dispose old ViewModel if it's disposable
            if (_currentViewModel is IDisposable disposable && _currentViewModel != viewModel)
            {
                disposable.Dispose();
            }

            _currentViewModel = viewModel;

            // Call lifecycle methods on new ViewModel
            if (_currentViewModel is NavigableViewModelBase navigableViewModel)
            {
                await navigableViewModel.OnNavigatedToAsync(parameter);
            }

            Log.Information("Raising Navigated event");
            // Raise event
            Navigated?.Invoke(this, new NavigationEventArgs(viewModel, parameter));
            Log.Information("Navigation completed successfully to {ViewModelType}", viewModel.GetType().Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "NavigateToInternal failed");
            throw;
        }
    }
}
