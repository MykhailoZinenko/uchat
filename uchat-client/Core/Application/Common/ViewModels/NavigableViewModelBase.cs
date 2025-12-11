using System;
using System.Threading.Tasks;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Core.Application.Common.ViewModels;

public abstract class NavigableViewModelBase : ViewModelBase
{
    protected readonly INavigationService NavigationService;

    protected NavigableViewModelBase(
        INavigationService navigationService,
        ILoggingService logger) : base(logger)
    {
        NavigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    /// <summary>
    /// Called when navigating TO this ViewModel
    /// </summary>
    public virtual Task OnNavigatedToAsync(object? parameter = null)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when navigating FROM this ViewModel
    /// </summary>
    public virtual Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}
