using System.Text;

namespace AriaSharp.Console;

public static class SignalHandler
{
    public static event EventHandler? Interrupted;
    public static event EventHandler? Terminated;

    public static void Install()
    {
        System.Console.CancelKeyPress += OnCancel;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private static void OnCancel(object? sender, System.ConsoleCancelEventArgs e)
    {
        if (e.SpecialKey == System.ConsoleSpecialKey.ControlC)
        {
            e.Cancel = true;
            Interrupted?.Invoke(sender, EventArgs.Empty);
        }
        else if (e.SpecialKey == System.ConsoleSpecialKey.ControlBreak)
        {
            e.Cancel = false;
            Terminated?.Invoke(sender, EventArgs.Empty);
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e) => Terminated?.Invoke(sender, EventArgs.Empty);
}