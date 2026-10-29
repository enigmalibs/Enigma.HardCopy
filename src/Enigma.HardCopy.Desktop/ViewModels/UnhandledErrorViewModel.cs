using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.HardCopy.Desktop.Resources;
using Enigma.HardCopy.Desktop.Services;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// What the safety net's dialog says: that something went wrong and the application is still running, what went
/// wrong, how many more errors came, and the details for a bug report.
/// </summary>
/// <remarks>
/// Enigma.GitClient's, as Enigma.TextEditor ported it. It follows its batch: an error counted into it while the
/// dialog is open shows at once, in the count and in the details.
/// </remarks>
public sealed class UnhandledErrorViewModel : ObservableObject
{
    private readonly UnhandledErrorBatch _batch;
    private readonly Func<string, Task> _copy;

    /// <summary>Initializes a new instance of the <see cref="UnhandledErrorViewModel"/> class.</summary>
    /// <param name="batch">The errors to show.</param>
    /// <param name="copy">Puts a text on the clipboard.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UnhandledErrorViewModel(UnhandledErrorBatch batch, Func<string, Task> copy)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(copy);

        _batch = batch;
        _copy = copy;

        CopyDetailsCommand = new AsyncRelayCommand(OnCopyDetailsAsync);

        batch.Changed += OnBatchChanged;
    }

    /// <summary>Gets the sentence that opens the dialog.</summary>
    public string Lead => Strings.UnhandledErrorLead;

    /// <summary>Gets what went wrong.</summary>
    public string Message => _batch.Message;

    /// <summary>Gets a value indicating whether more errors came after the first.</summary>
    public bool HasMore => _batch.More > 0;

    /// <summary>Gets how many more errors came after the first, as a sentence.</summary>
    public string MoreText
        => _batch.More == 1
            ? Strings.UnhandledErrorMoreOne
            : string.Format(CultureInfo.CurrentCulture, Strings.UnhandledErrorMoreFormat, _batch.More);

    /// <summary>Gets the details of every error, for a bug report.</summary>
    public string Details => _batch.Details;

    /// <summary>Gets the command that puts the details on the clipboard.</summary>
    public AsyncRelayCommand CopyDetailsCommand { get; }

    private Task OnCopyDetailsAsync() => _copy(Details);

    private void OnBatchChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(MoreText));
        OnPropertyChanged(nameof(Details));
    }
}
