using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// One of the shell's two pages.
/// </summary>
/// <remarks>
/// The shell navigates between ViewModels, not between views: it holds a list of these and the
/// <see cref="ViewLocator"/> finds each one's view. The abstraction is deliberately thin — a title for the
/// navigation strip is all the shell needs to know about a page.
/// </remarks>
public abstract class PageViewModel : ObservableObject
{
    /// <summary>Gets the page's title, as shown in the navigation strip.</summary>
    public abstract string Title { get; }
}
