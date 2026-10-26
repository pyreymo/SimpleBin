using Microsoft.Win32;
using SimpleBin.Helpers;
using System.Diagnostics;

namespace SimpleBin;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly BinHelper _bin = new();
    private readonly IconHelper _icons = new(IsDarkTheme());
    private readonly TrayMenu _menu = new();
    private readonly NotifyIcon _tray;
    private readonly Control _dispatcher = new();

    public TrayApplicationContext()
    {
        _dispatcher.CreateControl();
        _menu.Items.Add("清空...", CreateMenuIcon("\uE74D"), (_, _) => BinHelper.ClearBin());
        _menu.Items.Add("退出", CreateMenuIcon("\uE8BB"), (_, _) => ExitThread());
        _menu.ShowItemToolTips = false;
        foreach (ToolStripItem item in _menu.Items)
        {
            item.AutoSize = false;
            item.Size = new Size(144, 36);
        }
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

    private static Bitmap CreateMenuIcon(string glyph)
    {
        var image = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(image);
        using var font = new Font("Segoe MDL2 Assets", 12, GraphicsUnit.Pixel);
        TextRenderer.DrawText(graphics, glyph, font, new Rectangle(0, 0, 16, 16),
            SystemColors.MenuText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return image;
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
        _tray.Icon = _bin.IsEmpty() ? _icons.GetEmptyIcon() : _icons.GetIcon();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
            _bin.Update -= OnBinChanged;
            _bin.Dispose();
            _tray.Dispose();
            foreach (ToolStripItem item in _menu.Items)
                item.Image?.Dispose();
            _menu.Dispose();
            _dispatcher.Dispose();
        }
        base.Dispose(disposing);
    }
}


