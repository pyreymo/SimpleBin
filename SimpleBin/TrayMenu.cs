using Microsoft.Win32;
using System.Drawing.Drawing2D;

namespace SimpleBin;

internal sealed class TrayMenu : ToolStripDropDown
{
    private Point _openingPosition;
    private bool _mouseMoved;
    private bool _mousePressed;

    internal TrayMenu()
    {
        AutoSize = true;
        LayoutStyle = ToolStripLayoutStyle.Flow;
        var layout = (FlowLayoutSettings)LayoutSettings!;
        layout.FlowDirection = FlowDirection.TopDown;
        layout.WrapContents = false;
        ShowItemToolTips = false;
        Padding = new Padding(5);
        Renderer = new MenuRenderer();
    }

    protected override void OnOpening(System.ComponentModel.CancelEventArgs e)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        bool dark = key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        BackColor = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
        ForeColor = dark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(32, 32, 32);
        float scale = DeviceDpi / 96f;
        int width = Math.Max((int)(160 * scale), Items.Cast<ToolStripItem>()
            .Max(item => TextRenderer.MeasureText(item.Text, Font).Width) + (int)(64 * scale));
        int height = (int)(36 * scale);
        Padding = new Padding((int)(5 * scale));
        foreach (ToolStripItem item in Items)
        {
            item.Margin = Padding.Empty;
            item.AutoSize = false;
            item.Size = new Size(width, height);
        }
        _openingPosition = Cursor.Position;
        _mouseMoved = false;
        _mousePressed = false;
        base.OnOpening(e);
    }

    protected override void WndProc(ref Message m)
    {
        const int MouseMove = 0x0200;
        const int LeftButtonDown = 0x0201;
        const int LeftButtonUp = 0x0202;
        const int RightButtonUp = 0x0205;

        if (m.Msg == MouseMove)
        {
            // Ignore the stationary cursor inherited from the tray right-click.
            _mouseMoved |= Cursor.Position != _openingPosition;
            if (!_mouseMoved)
                return;
        }
        if (m.Msg == LeftButtonDown)
            _mousePressed = true;
        if (m.Msg == LeftButtonUp)
        {
            bool pressed = _mousePressed;
            _mousePressed = false;
            if (!pressed)
                return;
        }
        if (m.Msg == RightButtonUp)
            return;
        base.WndProc(ref m);
    }

    private sealed class MenuRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) =>
            e.Graphics.Clear(e.ToolStrip.BackColor);

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(55, e.ToolStrip.ForeColor));
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.ToolStrip is null || !e.Item.Selected) return;
            using var brush = new SolidBrush(Color.FromArgb(25, e.ToolStrip.ForeColor));
            // Drop-down layout places rows at the edge; inset the highlight within each row.
            float scale = e.ToolStrip.DeviceDpi / 96f;
            int horizontalInset = 0;
            int verticalInset = (int)(2 * scale);
            var bounds = new Rectangle(horizontalInset, verticalInset,
                e.Item.Width - horizontalInset * 2, e.Item.Height - verticalInset * 2);
            e.Graphics.FillRectangle(brush, bounds);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.ToolStrip is null) return;
            float scale = e.ToolStrip.DeviceDpi / 96f;
            var state = e.Graphics.Save();
            e.Graphics.TranslateTransform(12 * scale, (e.Item.Height - 16 * scale) / 2);
            e.Graphics.ScaleTransform(scale, scale);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(e.ToolStrip.ForeColor, 1.25f))
            {
                if (e.Item == e.ToolStrip.Items[0])
                {
                    e.Graphics.DrawLine(pen, 2, 4, 14, 4);
                    e.Graphics.DrawLines(pen, [new(5, 4), new(5, 2), new(11, 2), new(11, 4)]);
                    e.Graphics.DrawLines(pen, [new(3, 5), new(4, 14), new(12, 14), new(13, 5)]);
                    e.Graphics.DrawLine(pen, 6, 7, 6, 11);
                    e.Graphics.DrawLine(pen, 10, 7, 10, 11);
                }
                else
                {
                    e.Graphics.DrawLines(pen, [new(7, 2), new(2, 2), new(2, 14), new(7, 14)]);
                    e.Graphics.DrawLine(pen, 6, 8, 14, 8);
                    e.Graphics.DrawLines(pen, [new(11, 5), new(14, 8), new(11, 11)]);
                }
            }
            e.Graphics.Restore(state);
            e.TextColor = e.ToolStrip.ForeColor;
            e.TextRectangle = new Rectangle((int)(40 * scale), 0,
                e.Item.Width - (int)(64 * scale), e.Item.Height);
            e.TextFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            base.OnRenderItemText(e);
        }
    }
}
