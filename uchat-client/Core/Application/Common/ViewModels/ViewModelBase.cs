using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Core.Application.Common.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    protected readonly ILoggingService Logger;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    protected ViewModelBase(ILoggingService logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Safe async command execution with error handling
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, bool showBusy = true)
    {
        try
        {
            if (showBusy) IsBusy = true;
            ErrorMessage = string.Empty;

            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing operation in {ViewModelName}", GetType().Name);
            ErrorMessage = ex.Message;
        }
        finally
        {
            if (showBusy) IsBusy = false;
        }
    }

    public virtual void Dispose()
    {
        // Override in derived classes to clean up subscriptions
        GC.SuppressFinalize(this);
    }
}
