namespace SimpleBin;

internal sealed class TrayMenu : ContextMenuStrip
{
    private Point _openingPosition;
    private bool _mouseMoved;
    private bool _mousePressed;

    protected override void OnOpening(System.ComponentModel.CancelEventArgs e)
    {
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
}
