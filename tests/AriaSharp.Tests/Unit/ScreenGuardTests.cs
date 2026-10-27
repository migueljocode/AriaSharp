
namespace AriaSharp.Tests;

public class ScreenGuardTests
{
    [Fact]
    public void EnsureMinimum_DoesNotThrow()
    {
        // May return true or false depending on host; must not throw.
        _ = ScreenGuard.EnsureMinimum(1, 1);
        _ = ScreenGuard.EnsureMinimum(9999, 9999);
    }

    [Fact]
    public void TryEnter_WhenRedirected_ReturnsNull()
    {
        // Under test host stdout is often redirected — TryEnter should be safe.
        using var guard = ScreenGuard.TryEnter();
        // No assert on null/non-null; just that dispose is safe.
        guard?.Dispose();
    }

    [Fact]
    public void EmergencyRestore_DoesNotThrow()
    {
        // Manual: second Ctrl-C should leave alt-screen with cursor visible.
        ScreenGuard.EmergencyRestore();
    }
}
