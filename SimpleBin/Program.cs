using SimpleBin.Helpers;
using Velopack;
using Velopack.Windows;

namespace SimpleBin;

internal static class Program
{
    private static void ConfigureInstallation()
    {
        StartupHelper.AddToStartup();
        string directory = Path.GetDirectoryName(Application.ExecutablePath)!;
        foreach (var location in new[] { Environment.SpecialFolder.Programs, Environment.SpecialFolder.DesktopDirectory })
        {
            string folder = Environment.GetFolderPath(location);
            Directory.CreateDirectory(folder);
            using var shortcut = new ShellLink
            {
                Target = Application.ExecutablePath,
                WorkingDirectory = directory,
                IconPath = Path.Combine(directory, "App.ico"),
                IconIndex = 0,
                Description = "SimpleBin"
            };
            shortcut.SetAppUserModelId("velopack.SimpleBin");
            shortcut.Save(Path.Combine(folder, "SimpleBin.lnk"));
        }
    }

    [STAThread]
    private static void Main()
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => ConfigureInstallation())
            .OnAfterUpdateFastCallback(_ => ConfigureInstallation())
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
