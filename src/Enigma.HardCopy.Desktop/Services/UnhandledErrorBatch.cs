using System;
using System.Collections.Generic;
using System.Text;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// The errors one dialog shows: the first, and how many more came while it waited or was open.
/// </summary>
public sealed class UnhandledErrorBatch
{
    private readonly List<Exception> _errors = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledErrorBatch"/> class.
    /// </summary>
    /// <param name="first">The error that opens it.</param>
    public UnhandledErrorBatch(Exception first)
    {
        ArgumentNullException.ThrowIfNull(first);

        _errors.Add(first);
    }

    /// <summary>
    /// Raised when another error is counted into the batch, so a dialog on screen can follow.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets the error that opened the batch.
    /// </summary>
    public Exception First => _errors[0];

    /// <summary>
    /// Gets every error of the batch, the first one first.
    /// </summary>
    public IReadOnlyList<Exception> Errors => _errors;

    /// <summary>
    /// Gets what the dialog says went wrong: the first error's message.
    /// </summary>
    public string Message => UnhandledErrorPolicy.Summarise(First);

    /// <summary>
    /// Gets how many errors came after the first.
    /// </summary>
    public int More => _errors.Count - 1;

    /// <summary>
    /// Gets the details of every error, for a bug report: type, message, inner exceptions and stack trace.
    /// </summary>
    public string Details
    {
        get
        {
            if (_errors.Count == 1)
            {
                return UnhandledErrorPolicy.BuildDetails(First);
            }

            StringBuilder details = new();

            for (int index = 0; index < _errors.Count; index++)
            {
                if (index > 0)
                {
                    details.AppendLine().AppendLine();
                }

                details.Append("Error ").Append(index + 1).Append(" of ").Append(_errors.Count).AppendLine();
                details.Append(UnhandledErrorPolicy.BuildDetails(_errors[index]));
            }

            return details.ToString();
        }
    }

    /// <summary>
    /// Counts another error into the batch.
    /// </summary>
    /// <param name="error">The error.</param>
    internal void Add(Exception error)
    {
        _errors.Add(error);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
