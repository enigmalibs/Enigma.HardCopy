using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Enigma.HardCopy.Desktop.ViewModels;

namespace Enigma.HardCopy.Desktop;

/// <summary>
/// Finds the view for a page ViewModel by name: <c>…ViewModels.BackupViewModel</c> is rendered by
/// <c>…Views.BackupView</c>.
/// </summary>
/// <remarks>
/// Registered in <c>App.axaml</c>'s data templates, which is what lets the shell bind a
/// <see cref="PageViewModel"/> straight into a content control and get its view. It matches only page
/// ViewModels, so the smaller ViewModels bound inside a view — an activity entry, a density option — still fall
/// through to their own explicit templates.
/// </remarks>
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc/>
    public bool Match(object? data) => data is PageViewModel;

    /// <inheritdoc/>
    public Control Build(object? param)
    {
        if (param is null)
        {
            return new TextBlock { Text = "No page to show." };
        }

        string viewName = param.GetType().FullName!
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        Type? viewType = Type.GetType(viewName);

        // A missing view is a wiring mistake, and saying so on the surface it was supposed to fill is more
        // useful during development than an exception from deep inside template application.
        return viewType is null
            ? new TextBlock { Text = $"View not found: {viewName}" }
            : (Control)Activator.CreateInstance(viewType)!;
    }
}
