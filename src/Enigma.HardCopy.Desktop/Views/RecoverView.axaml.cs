using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Enigma.HardCopy.Desktop.Services;
using Enigma.HardCopy.Desktop.ViewModels;

namespace Enigma.HardCopy.Desktop.Views;

/// <summary>
/// The recovery page. Its <see cref="Control.DataContext"/> is the
/// <see cref="RecoverViewModel"/> the shell binds into it.
/// </summary>
/// <remarks>
/// The drag-and-drop handling lives here because a drop is a gesture, not a decision: the code-behind's whole
/// job is to translate the platform's dropped items into the <see cref="IPickedFile"/> values the ViewModel
/// already accepts from its picker, and then hand them over. No import logic is duplicated.
/// </remarks>
public partial class RecoverView : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="RecoverView"/> class.</summary>
    public RecoverView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
        => e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not RecoverViewModel viewModel)
        {
            return;
        }

        IReadOnlyList<IPickedFile> files = StorageProviderFile.Adapt(e.DataTransfer.TryGetFiles());
        if (files.Count == 0)
        {
            return;
        }

        // Fire and forget by design: the command owns its own task and reports through the ViewModel, and an
        // async void event handler would only add a way for a failure to escape unobserved.
        if (viewModel.ImportFilesCommand.CanExecute(files))
        {
            viewModel.ImportFilesCommand.Execute(files);
        }
    }
}
