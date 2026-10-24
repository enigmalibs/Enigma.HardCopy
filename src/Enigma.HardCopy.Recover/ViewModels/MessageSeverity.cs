namespace Enigma.HardCopy.Recover.ViewModels;

/// <summary>
/// How much weight to give one message shown to the user.
/// </summary>
/// <remarks>
/// The same four levels as the desktop app, with the same meaning. The distinction that matters is
/// <see cref="Warning"/> versus <see cref="Error"/>: an error means nothing happened; a warning means something did
/// happen and the user has to understand what — a code from another backup ignored, bytes that failed their hash.
/// </remarks>
public enum MessageSeverity
{
    /// <summary>Plain progress or explanation. Nothing is wrong.</summary>
    Information = 0,

    /// <summary>Something worked, and the user can stop worrying about it.</summary>
    Success,

    /// <summary>Something happened, but not the way the user should assume.</summary>
    Warning,

    /// <summary>Something failed. Nothing was produced.</summary>
    Error,
}
