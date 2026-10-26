using Microsoft.Win32;

namespace SimpleBin.Helpers;

internal static class StartupHelper
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    internal static void AddToStartup()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue("SimpleBin", $"\"{Application.ExecutablePath}\"");
    }

    internal static void RemoveFromStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
        key?.DeleteValue("SimpleBin", false);
    }
}
