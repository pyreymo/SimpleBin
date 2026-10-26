namespace SimpleBin.Helpers;

internal sealed class IconHelper : IDisposable
{
    private readonly Icon _dark = Load("BinLight.ico");
    private readonly Icon _darkEmpty = Load("EmptyBinLight.ico");
    private readonly Icon _light = Load("BinDark.ico");
    private readonly Icon _lightEmpty = Load("EmptyBinDark.ico");
    private bool _isDarkTheme;

    internal IconHelper(bool isDarkTheme) => SetTheme(isDarkTheme);
    internal Icon GetEmptyIcon() => _isDarkTheme ? _darkEmpty : _lightEmpty;
    internal Icon GetIcon() => _isDarkTheme ? _dark : _light;
    internal void SetTheme(bool isDarkTheme) => _isDarkTheme = isDarkTheme;

    private static Icon Load(string name)
    {
        using var stream = typeof(IconHelper).Assembly.GetManifestResourceStream($"SimpleBin.Resources.{name}")!;
        return new Icon(stream);
    }

    public void Dispose()
    {
        _dark.Dispose();
        _darkEmpty.Dispose();
        _light.Dispose();
        _lightEmpty.Dispose();
    }
}
