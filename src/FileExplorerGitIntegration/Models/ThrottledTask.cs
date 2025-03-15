// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace FileExplorerGitIntegration.Models;

internal sealed class ThrottledTask
{
    private readonly TimeSpan _interval;
    private readonly object _lock = new();

    private readonly Action _action;

    private Task? _currentTask;
    private bool _shouldQueue;

    // Run an action, but ensure that `interval` time has elapsed after the last action completed before running again.
    // If a task is already running when Run is called again, we "queue" that request to execute after enough time has passed.
    // Multiple requests during this period of time result in only a single action being run after the waiting period.
    // In other words, when there is a rapid flood of calls to Run(), this is coalesced into:
    //   - The first call to Run invokes _action immediately.
    //   - The subsequent calls within the window are all coalesced into a second, delayed invoke of _action
    //   - If more calls arrive during this second invoke, they are coalesced into a third, delayed invoke.
    //   - and so on...
    public ThrottledTask(Action action, TimeSpan interval)
    {
        _action = action;
        _interval = interval;
    }

    // The first time Run is called, wait until new requests stop getting queued, checking every _interval, then create a task to invoke _action.
    // If Run is not called again while the task is active (during the action or cooldown)
    //   then the task exits normally and resets state back to initial.
    // Otherwise, if Run is called again while the task is active,
    //   then set _shouldQueue to true.
    //   Now, when the action and cooldown complete, we'll loopback and execute one more call and reset the queue flag.
    public void Run(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_currentTask != null && !_currentTask.IsCompleted)
            {
                _shouldQueue = true;
                return;
            }

            _currentTask = Task.Run(
                async () =>
                {
                    bool runAgain = true;
                    while (runAgain)
                    {
                        bool waitAgain = false;
                        do
                        {
                            await Task.Delay(_interval, cancellationToken);
                            lock (_lock)
                            {
                                if (_shouldQueue)
                                {
                                    _shouldQueue = false;
                                    waitAgain = true;
                                }
                                else
                                {
                                    waitAgain = false;
                                }
                            }
                        }
                        while (!waitAgain);

                        _action.Invoke();
                        lock (_lock)
                        {
                            if (_shouldQueue)
                            {
                                _shouldQueue = false;
                            }
                            else
                            {
                                runAgain = false;
                                _currentTask = null;
                            }
                        }
                    }
                },
                cancellationToken);
        }
    }
}
