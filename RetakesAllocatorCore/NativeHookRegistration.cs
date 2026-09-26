namespace RetakesAllocatorCore;

/// <summary>
/// Owns one native registration. The callback must remain rooted until native
/// detachment succeeds, including partial registration failures.
/// </summary>
public sealed class NativeHookRegistration : IDisposable
{
    private readonly nint _handle;
    private readonly Action _attach;
    private readonly Action _detach;
    private readonly Action _releaseCallback;
    private bool _possiblyAttached;
    private bool _disposed;
    public bool IsAttached { get; private set; }

    public NativeHookRegistration(nint handle, Action attach, Action detach, Action releaseCallback)
    {
        _handle = handle;
        _attach = attach;
        _detach = detach;
        _releaseCallback = releaseCallback;
    }

    public void Attach()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsAttached) return;
        if (_possiblyAttached) throw new InvalidOperationException("Previous hook attempt needs cleanup.");
        if (_handle == 0) throw new InvalidOperationException("Cannot hook an unresolved native function.");
        _possiblyAttached = true; // A throwing native attach may have partially installed the hook.
        _attach();
        IsAttached = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        IsAttached = false; // Disable callbacks even if native detachment fails.
        if (_possiblyAttached)
        {
            _detach(); // If this throws, DO NOT release the callback; native may still invoke it.
            _possiblyAttached = false;
        }
        _releaseCallback();
        _disposed = true;
    }
}
