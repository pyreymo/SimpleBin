using SimpleBin.Helpers;
using Velopack;

namespace SimpleBin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => StartupHelper.AddToStartup())
            .OnAfterUpdateFastCallback(_ => StartupHelper.AddToStartup())
            .OnBeforeUninstallFastCallback(_ => StartupHelper.RemoveFromStartup())
            .Run();

        using var instance = new Mutex(true, @"Local\SimpleBin", out bool firstInstance);
        if (!firstInstance) return;

        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);
            using var context = new TrayApplicationContext();
            Application.Run(context);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "SimpleBin", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
