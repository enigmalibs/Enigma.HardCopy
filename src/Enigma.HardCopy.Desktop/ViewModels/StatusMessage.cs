using System;

namespace Enigma.HardCopy.Desktop.ViewModels;

/// <summary>
/// One line of text to show the user, and how much weight to give it.
/// </summary>
/// <remarks>
/// Used both for a view's single prominent message and for the entries of the recovery activity log — they
/// are the same thing at different sizes, so they are the same type.
/// <para>
/// The <c>Is…</c> properties exist so a view can style a message without a value converter: Avalonia binds a
/// boolean straight onto a style class. Exposing a brush from a ViewModel would put theme colours in the
/// wrong layer.
/// </para>
/// </remarks>
public sealed record StatusMessage
{
    /// <summary>Initializes a new instance of the <see cref="StatusMessage"/> record.</summary>
    /// <param name="text">The text to show. Already localized and formatted.</param>
    /// <param name="severity">How much weight to give it.</param>
    /// <exception cref="ArgumentException"><paramref name="text"/> is <see langword="null"/> or white space.</exception>
    public StatusMessage(string text, MessageSeverity severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Text = text;
        Severity = severity;
    }

    /// <summary>Gets the text to show.</summary>
    public string Text { get; }

    /// <summary>Gets how much weight to give the message.</summary>
    public MessageSeverity Severity { get; }

    /// <summary>Gets a value indicating whether this reports something that worked.</summary>
    public bool IsSuccess => Severity is MessageSeverity.Success;

    /// <summary>Gets a value indicating whether this reports something the user has to look at.</summary>
    public bool IsWarning => Severity is MessageSeverity.Warning;

    /// <summary>Gets a value indicating whether this reports a failure.</summary>
    public bool IsError => Severity is MessageSeverity.Error;

    /// <summary>Creates an <see cref="MessageSeverity.Information"/> message.</summary>
    /// <param name="text">The text to show.</param>
    /// <returns>The message.</returns>
    public static StatusMessage Information(string text) => new(text, MessageSeverity.Information);

    /// <summary>Creates a <see cref="MessageSeverity.Success"/> message.</summary>
    /// <param name="text">The text to show.</param>
    /// <returns>The message.</returns>
    public static StatusMessage Success(string text) => new(text, MessageSeverity.Success);

    /// <summary>Creates a <see cref="MessageSeverity.Warning"/> message.</summary>
    /// <param name="text">The text to show.</param>
    /// <returns>The message.</returns>
    public static StatusMessage Warning(string text) => new(text, MessageSeverity.Warning);

    /// <summary>Creates an <see cref="MessageSeverity.Error"/> message.</summary>
    /// <param name="text">The text to show.</param>
    /// <returns>The message.</returns>
    public static StatusMessage Error(string text) => new(text, MessageSeverity.Error);
}
