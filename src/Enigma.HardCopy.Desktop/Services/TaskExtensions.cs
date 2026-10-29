using System;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.HardCopy.Desktop.Services;

/// <summary>
/// Work started and not awaited.
/// </summary>
public static class TaskExtensions
{
    extension(Task task)
    {
        /// <summary>
        /// Lets the work run on without anyone awaiting it, and hands a failure to the safety net
        /// (<see cref="UnhandledExceptionHooks"/>) instead of letting it vanish in a task nobody looks at.
        /// </summary>
        /// <remarks>
        /// The one way the application starts work it does not await. A cancelled task is no failure and
        /// says nothing; a failure is reported once, from whatever thread the task ends on — the safety net
        /// logs it there and shows it on the UI thread. Work that handles its own failures goes through here
        /// too: what it does not handle is exactly what the net is for.
        /// </remarks>
        public void Forget() => task.Forget(UnhandledErrorSource.FireAndForget);

        /// <summary>
        /// Lets the work run on without anyone awaiting it, and reports a failure as coming from
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Where a failure is said to come from — <see cref="UnhandledErrorSource.Startup"/>
        /// for the work that starts the application.</param>
        public void Forget(UnhandledErrorSource source)
        {
            ArgumentNullException.ThrowIfNull(task);

            if (task.IsCompleted)
            {
                Observe(task, source);
                return;
            }

            // The continuation's own task is discarded on purpose: Observe cannot fail, so it never faults.
            _ = task.ContinueWith(
                (completed, state) => Observe(completed, (UnhandledErrorSource)state!),
                source,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private static void Observe(Task task, UnhandledErrorSource source)
    {
        if (task is not { IsFaulted: true, Exception: { } failure })
        {
            return;
        }

        foreach (Exception exception in failure.InnerExceptions)
        {
            UnhandledExceptionHooks.Report(exception, source);
        }
    }
}
