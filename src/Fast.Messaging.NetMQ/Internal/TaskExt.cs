namespace Fast.Messaging.Internal;

internal static class TaskExt
{
    public static Task Run(Func<CancellationToken, Task> func, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled(ct);

        return Task.Factory.StartNew(() => func(ct), ct,
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.RunContinuationsAsynchronously,
            TaskScheduler.Default);
    }
}