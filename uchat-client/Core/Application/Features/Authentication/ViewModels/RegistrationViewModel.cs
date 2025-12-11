using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;

namespace uchat_client.Core.Application.Features.Authentication.ViewModels;

public class RegistrationViewModel : NavigableViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IValidationService _validationService;
    private readonly SidebarViewModel _sidebarViewModel;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

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

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public ICommand RegisterCommand { get; }
    public ICommand BackToLoginCommand { get; }

    public RegistrationViewModel(
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

        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        BackToLoginCommand = new RelayCommand(() => NavigationService.NavigateToLogin());
    }

    private async Task RegisterAsync()
    {
        // Validate input
        var validation = _validationService.ValidateRegistration(Username, Password, ConfirmPassword);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.ErrorMessage;
            return;
        }

        await ExecuteAsync(async () =>
        {
            Logger.LogInformation("Attempting registration for user: {Username}", Username);
            var response = await _authService.RegisterAsync(Username, Password);

            if (response.Success)
            {
                Logger.LogInformation("Registration successful for user: {Username}", Username);
                await NavigateToDefaultRoomAsync();
            }
            else
            {
                ErrorMessage = response.Message;
                Logger.LogWarning("Registration failed for user {Username}: {Message}", Username, response.Message);
            }
        });
    }

    private async Task NavigateToDefaultRoomAsync()
    {
        await _sidebarViewModel.EnsureRoomsLoadedAsync();
        Logger.LogInformation("Post-registration rooms loaded: Count={Count}", _sidebarViewModel.RoomCount);
        var room = _sidebarViewModel.GetDefaultRoom();
        if (room != null)
        {
            NavigationService.NavigateToChat(room.Id, room.DisplayName, room.IsGlobal);
        }
        else
        {
            Logger.LogWarning("No rooms available to navigate after registration");
        }
    }
}
