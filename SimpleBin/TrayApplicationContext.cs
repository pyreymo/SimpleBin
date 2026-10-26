using Microsoft.Win32;
using SimpleBin.Helpers;
using System.Diagnostics;

namespace SimpleBin;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly BinHelper _bin = new();
    private readonly IconHelper _icons = new(IsDarkTheme());
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _tray;
    private readonly Control _dispatcher = new();
    private readonly ToolStripMenuItem _count = new();
    private readonly ToolStripMenuItem _size = new();

    public TrayApplicationContext()
    {
        _dispatcher.CreateControl();
        _menu.Items.AddRange([_count, _size]);
        _menu.Items.Add("清空...", null, (_, _) => BinHelper.ClearBin());
        _menu.Items.Add("退出", null, (_, _) => ExitThread());
        _tray = new NotifyIcon { Text = "SimpleBin", ContextMenuStrip = _menu };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                Process.Start("explorer.exe", "shell:RecycleBinFolder");
        };
        _bin.Update += OnBinChanged;
        SystemEvents.UserPreferenceChanged += OnPreferenceChanged;
        RefreshIcon();
        _tray.Visible = true;
    }

    private static bool IsDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is not int value || value == 0;
    }

    private void OnBinChanged(object sender, FileSystemEventArgs e) => RefreshOnUiThread();
    private void OnPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => RefreshOnUiThread();

    private void RefreshOnUiThread()
    {
        if (!_dispatcher.IsDisposed)
            _dispatcher.BeginInvoke(RefreshIcon);
    }

    private void RefreshIcon()
    {
        _icons.SetTheme(IsDarkTheme());
        var (size, count) = BinHelper.GetBinSize();
        _count.Text = $"数量：{count}";
        _size.Text = $"大小：{size} B";
        _tray.Icon = count == 0 ? _icons.GetEmptyIcon() : _icons.GetIcon();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
            _bin.Update -= OnBinChanged;
            _bin.Dispose();
            _tray.Dispose();
            _menu.Dispose();
            _dispatcher.Dispose();
        }
        base.Dispose(disposing);
    }
}
