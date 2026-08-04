namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// How much weight to give one message shown to the user.
/// </summary>
/// <remarks>
/// The distinction that matters here is <see cref="Warning"/> versus <see cref="Error"/>. An error means
/// nothing happened; a warning means something did happen and the user has to understand what — a recovered
/// file whose hash does not match is the case this application exists to make unmistakable.
/// </remarks>
public enum MessageSeverity
{
    /// <summary>Plain progress or explanation. Nothing is wrong.</summary>
    Information = 0,

    /// <summary>Something worked, and the user can stop worrying about it.</summary>
    Success,

    /// <summary>
    /// Something worked, but not the way the user should assume — unverified bytes, a backup that will run to
    /// an unreasonable number of pages.
    /// </summary>
    Warning,

    /// <summary>Something failed. Nothing was produced.</summary>
    Error,
}
