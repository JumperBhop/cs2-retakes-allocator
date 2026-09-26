using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;

namespace RetakesAllocator;

/// <summary>
/// Resolves the optional buy-menu hook exclusively against the installed CSSharp
/// gamedata. No plugin-local signatures, background updates or GiveNamedItem2.
/// </summary>
public sealed class CustomGameData : IDisposable
{
    // Exact ABI used by CounterStrikeSharp 375 VirtualFunctions.
    private MemoryFunctionWithReturn<CCSPlayer_ItemServices, CEconItemView, AcquireMethod, IntPtr, AcquireResult>? _canAcquire;
    public MemoryFunctionWithReturn<int, string, CCSWeaponBaseVData>? GetCSWeaponDataFromKeyFunc { get; private set; }
    private FunctionReference? _callbackReference;
    private Func<DynamicHook, HookResult>? _callback;
    private NativeHookRegistration? _registration;
    public bool IsHooked => _registration?.IsAttached == true;

    public bool TryHook(Func<DynamicHook, HookResult> handler)
    {
        if (_registration != null) throw new InvalidOperationException("Hook registration is immutable until unload.");
        try
        {
            _canAcquire = new(GameData.GetSignature("CCSPlayer_ItemServices_CanAcquire"));
            GetCSWeaponDataFromKeyFunc = new(GameData.GetSignature("GetCSWeaponDataFromKey"));
            // CSSharp's constructors can swallow resolution errors and return Handle=0.
            if (_canAcquire.Handle == IntPtr.Zero || GetCSWeaponDataFromKeyFunc.Handle == IntPtr.Zero)
            {
                Log.Warn("Buy-menu hook disabled: CSSharp gamedata did not resolve CanAcquire/GetCSWeaponDataFromKey. !guns and round allocation remain available.");
                return false;
            }

            _callback = handler;
            _callbackReference = FunctionReference.Create(_callback);
            var callbackPointer = _callbackReference.GetFunctionPointer();
            var functionHandle = _canAcquire.Handle;
            _registration = new NativeHookRegistration(functionHandle,
                () => NativeAPI.HookFunction(functionHandle, callbackPointer, false),
                () => NativeAPI.UnhookFunction(functionHandle, callbackPointer, false),
                ReleaseCallback);
            _registration.Attach();
            Log.Info("CanAcquire hook attached using CounterStrikeSharp gamedata (API 375 ABI).");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Buy-menu hook unavailable: {ex.Message}. !guns and round allocation remain available.");
            Dispose();
            return false;
        }
    }

    private void ReleaseCallback()
    {
        if (_callbackReference != null) FunctionReference.Remove(_callbackReference.Identifier);
        _callbackReference = null;
        _callback = null;
    }

    public void Dispose()
    {
        try
        {
            _registration?.Dispose();
        }
        catch (Exception ex)
        {
            // FunctionReference's permanent registry keeps the native thunk AND
            // managed delegate alive. Releasing it here would risk a GC callback crash.
            Log.Error($"Could not detach CanAcquire hook; callback retained, no rehook attempted. Restart server: {ex.Message}");
        }
    }
}
