using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;

namespace uchat_client.Core.Application.Features.Authentication.ViewModels;

public class LoginViewModel : NavigableViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IValidationService _validationService;
    private readonly SidebarViewModel _sidebarViewModel;
    private string _username = string.Empty;
    private string _password = string.Empty;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand ShowRegisterCommand { get; }

    public LoginViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IValidationService validationService,
        SidebarViewModel sidebarViewModel,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _sidebarViewModel = sidebarViewModel ?? throw new ArgumentNullException(nameof(sidebarViewModel));

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        ShowRegisterCommand = new RelayCommand(() => NavigationService.NavigateToRegistration());
    }

    private async Task LoginAsync()
    {
        // Validate input
        var validation = _validationService.ValidateLogin(Username, Password);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.ErrorMessage;
            return;
        }

        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("Attempting login for user: {Username}", Username);
            var response = await _authService.LoginAsync(Username, Password);

            if (response.Success)
            {
                Logger.LogInformation("Login successful for user: {Username}", Username);
                await NavigateToDefaultRoomAsync();
            }
            else
            {
                ErrorMessage = response.Message;
                Logger.LogWarning("Login failed for user {Username}: {Message}", Username, response.Message);
            }
        });
    }
    private async Task NavigateToDefaultRoomAsync()
    {
        await _sidebarViewModel.EnsureRoomsLoadedAsync();
        var room = _sidebarViewModel.GetDefaultRoom();
        if (room != null)
        {
            NavigationService.NavigateToChat(room.Id, room.DisplayName, room.IsGlobal);
        }
        else
        {
            Logger.LogWarning("No rooms available to navigate after login");
        }
    }
}
