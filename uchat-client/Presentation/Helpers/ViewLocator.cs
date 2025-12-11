// ViewLocator.cs

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using uchat_client.Core.Application.Common.ViewModels;

namespace uchat_client.Presentation.Helpers;

[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewModelFullName = param.GetType().FullName!;

        // Map ViewModels from Core.Application.Features to Presentation.Views
        // Example: uchat_client.Core.Application.Features.Settings.ViewModels.SettingsViewModel
        //       -> uchat_client.Presentation.Views.Settings.SettingsView
        var viewName = viewModelFullName
            .Replace("Core.Application.Features.", "Presentation.Views.")
            .Replace(".ViewModels.", ".")
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var type = Type.GetType(viewName);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}