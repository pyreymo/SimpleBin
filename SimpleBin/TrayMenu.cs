using Microsoft.Win32;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SimpleBin;

internal sealed class TrayMenu : NativeWindow, IDisposable
{
    private float _scale;
    private Font? _font;
    private Color _background;
    private Color _foreground;
    private bool _showing;

    internal TrayMenu() => CreateHandle(new CreateParams
    {
        Caption = "SimpleBin.MenuOwner",
        Style = unchecked((int)0x80000000) // A top-level owner lets Windows dismiss the tray menu normally.
    });

    internal uint Show(Point position)
    {
        if (_showing) return 0;
        _showing = true;
        nint menu = 0;
        nint backgroundBrush = 0;
        try
        {
            Native.SetWindowPos(Handle, 0, position.X, position.Y, 0, 0, 0x0015);
            _scale = Native.GetDpiForWindow(Handle) / 96f;
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            bool dark = key?.GetValue("AppsUseLightTheme") is int value && value == 0;
            _background = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
            _foreground = dark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(32, 32, 32);
            var systemFont = SystemFonts.MenuFont ?? SystemFonts.DefaultFont;
            _font = new Font(systemFont.FontFamily, systemFont.SizeInPoints * 96 * _scale / 72, GraphicsUnit.Pixel);

            menu = Native.CreatePopupMenu();
            if (menu == 0) throw new Win32Exception();
            AddItem(menu, 1, "清空...");
            AddItem(menu, 2, "退出");
            backgroundBrush = Native.CreateSolidBrush((uint)ColorTranslator.ToWin32(_background));
            var menuInfo = new MenuInfo
            {
                Size = (uint)Marshal.SizeOf<MenuInfo>(),
                Mask = 2, // MIM_BACKGROUND
                Background = backgroundBrush
            };
            if (!Native.SetMenuInfo(menu, ref menuInfo)) throw new Win32Exception();

            Native.SetForegroundWindow(Handle);
            var placement = new PopupParameters
            {
                Size = (uint)Marshal.SizeOf<PopupParameters>(),
                Exclude = new Rect(position.X - 1, position.Y - 1, position.X + 1, position.Y + 1)
            };
            // Windows owns the input loop: hover never executes a command; click, Enter or Esc ends it.
            return Native.TrackPopupMenuEx(menu, 0x0100 | 0x0040, position.X, position.Y, Handle, ref placement);
        }
        finally
        {
            if (menu != 0) Native.DestroyMenu(menu);
            if (backgroundBrush != 0) Native.DeleteObject(backgroundBrush);
            _font?.Dispose();
            _font = null;
            _showing = false;
            // Required by the Windows notification-area menu contract for subsequent invocations.
            Native.PostMessageW(Handle, 0, 0, 0);
        }
    }

    private static void AddItem(nint menu, uint id, string text)
    {
        if (!Native.AppendMenuW(menu, 0, id, text)) throw new Win32Exception();
        var info = new MenuItemInfo
        {
            Size = (uint)Marshal.SizeOf<MenuItemInfo>(),
            Mask = 0x0100, // MIIM_FTYPE; preserve the text for accessibility.
            Type = 0x0100 // MFT_OWNERDRAW
        };
        if (!Native.SetMenuItemInfoW(menu, id, false, ref info)) throw new Win32Exception();
    }

    protected override void WndProc(ref Message m)
    {
        const int MeasureItem = 0x002C;
        const int DrawItem = 0x002B;
        const int ActivateApp = 0x001C;
        if (m.Msg == MeasureItem && _font is not null)
        {
            var item = Marshal.PtrToStructure<MeasureItemInfo>(m.LParam);
            item.Width = (uint)Math.Max(160 * _scale,
                TextRenderer.MeasureText("清空...", _font).Width + 64 * _scale);
            item.Height = (uint)Math.Round(36 * _scale);
            Marshal.StructureToPtr(item, m.LParam, false);
            m.Result = 1;
            return;
        }
        if (m.Msg == DrawItem && _font is not null)
        {
            Draw(Marshal.PtrToStructure<DrawItemInfo>(m.LParam));
            m.Result = 1;
            return;
        }
        if (m.Msg == ActivateApp && m.WParam == 0 && _showing)
            Native.EndMenu();
        base.WndProc(ref m);
    }

    private void Draw(DrawItemInfo item)
    {
        using var graphics = Graphics.FromHdc(item.DeviceContext);
        var bounds = Rectangle.FromLTRB(item.Bounds.Left, item.Bounds.Top, item.Bounds.Right, item.Bounds.Bottom);
        using var background = new SolidBrush(_background);
        graphics.FillRectangle(background, bounds);
        if ((item.State & 1) != 0) // ODS_SELECTED
        {
            var highlight = bounds;
            highlight.Inflate(-(int)Math.Round(5 * _scale), -(int)Math.Round(2 * _scale));
            using var hover = new SolidBrush(Color.FromArgb(25, _foreground));
            graphics.FillRectangle(hover, highlight);
        }
        var state = graphics.Save();
        graphics.TranslateTransform(bounds.Left + 12 * _scale, bounds.Top + (bounds.Height - 16 * _scale) / 2);
        graphics.ScaleTransform(_scale, _scale);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var pen = new Pen(_foreground, 1.25f))
        {
            if (item.Id == 1)
            {
                graphics.DrawLine(pen, 2, 4, 14, 4);
                graphics.DrawLines(pen, [new(5, 4), new(5, 2), new(11, 2), new(11, 4)]);
                graphics.DrawLines(pen, [new(3, 5), new(4, 14), new(12, 14), new(13, 5)]);
                graphics.DrawLine(pen, 6, 7, 6, 11);
                graphics.DrawLine(pen, 10, 7, 10, 11);
            }
            else
            {
                graphics.DrawLines(pen, [new(7, 2), new(2, 2), new(2, 14), new(7, 14)]);
                graphics.DrawLine(pen, 6, 8, 14, 8);
                graphics.DrawLines(pen, [new(11, 5), new(14, 8), new(11, 11)]);
            }
        }
        graphics.Restore(state);
        var textBounds = Rectangle.FromLTRB(bounds.Left + (int)(40 * _scale), bounds.Top,
            bounds.Right - (int)(24 * _scale), bounds.Bottom);
        TextRenderer.DrawText(graphics, item.Id == 1 ? "清空..." : "退出", _font, textBounds, _foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    public void Dispose() => DestroyHandle();

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect(int left, int top, int right, int bottom)
    {
        public int Left = left, Top = top, Right = right, Bottom = bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PopupParameters
    {
        public uint Size;
        public Rect Exclude;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MeasureItemInfo
    {
        public uint ControlType, ControlId, Id, Width, Height;
        public nuint Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DrawItemInfo
    {
        public uint ControlType, ControlId, Id, Action, State;
        public nint ItemWindow, DeviceContext;
        public Rect Bounds;
        public nuint Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MenuInfo
    {
        public uint Size, Mask, Style, MaxHeight;
        public nint Background;
        public uint HelpId;
        public nuint Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MenuItemInfo
    {
        public uint Size, Mask, Type, State, Id;
        public nint SubMenu, CheckedBitmap, UncheckedBitmap;
        public nuint Data;
        public nint Text;
        public uint TextLength;
        public nint Bitmap;
    }

    private static class Native
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint CreatePopupMenu();
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AppendMenuW(nint menu, uint flags, nuint id, string text);
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetMenuItemInfoW(nint menu, uint id, [MarshalAs(UnmanagedType.Bool)] bool byPosition, ref MenuItemInfo info);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetMenuInfo(nint menu, ref MenuInfo info);
        [DllImport("user32.dll")]
        internal static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, ref PopupParameters parameters);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyMenu(nint menu);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(nint window);
        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessageW(nint window, uint message, nuint wParam, nint lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EndMenu();
        [DllImport("user32.dll")]
        internal static extern uint GetDpiForWindow(nint window);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
        [DllImport("gdi32.dll")]
        internal static extern nint CreateSolidBrush(uint color);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(nint handle);
    }
}
