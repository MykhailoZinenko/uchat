using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Common.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;

namespace uchat_client.Core.Application.Features.Settings.ViewModels;

public class SoundNotificationsViewModel : NavigableViewModelBase
{
    private readonly SidebarViewModel _sidebarViewModel;
    private double _volume = 50;
    private bool _notificationsEnabled = true;

    public SidebarViewModel SidebarViewModel => _sidebarViewModel;

    public double Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public double SoundVolume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public ICommand BackCommand { get; }

    public SoundNotificationsViewModel(
        INavigationService navigationService,
        SidebarViewModel sidebarViewModel,
        ILoggingService logger)
        : base(navigationService, logger)
    {
        _sidebarViewModel = sidebarViewModel;
        BackCommand = new RelayCommand(() => NavigationService.NavigateToSettings());
    }

    public override Task OnNavigatedToAsync(object? parameter = null)
    {
        _sidebarViewModel.IsOnSettingsPage = true;
        return base.OnNavigatedToAsync(parameter);
    }
}
