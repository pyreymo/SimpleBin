using System.Runtime.InteropServices;
using System.Security.Principal;
using Timer = System.Threading.Timer;

namespace SimpleBin
{
    static partial class NativeBinMethods
    {
        [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SHEmptyRecycleBinW(
             IntPtr hwnd,
             string? pszRootPath,
             uint dwFlags);
    }

    public sealed class BinHelper : IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = [];
        public delegate void BinUpdateHandler(object sender, FileSystemEventArgs e);
        public event BinUpdateHandler? Update;
        private Timer? _debounceTimer;
        private readonly Lock _lock = new();
        private bool _isDisposed;

        public BinHelper()
        {
            string? sid = (WindowsIdentity.GetCurrent().User?.Value)
                ?? throw new InvalidOperationException("No such user");
            string binPath = Path.Combine("$Recycle.Bin", sid);

            var bins = DriveInfo.GetDrives().Where(d => d is
            {
                IsReady: true,
                DriveType: DriveType.Fixed or DriveType.Network
            }).Select(d => Path.Combine(d.RootDirectory.FullName, binPath));

            foreach (var bin in bins)
            {
                if (!Directory.Exists(bin)) continue;

                var watcher = new FileSystemWatcher(bin)
                {
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                    InternalBufferSize = 65536,
                    Filters = { "$I*", "$R*" },
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                };

                watcher.Created += OnBinChanged;
                watcher.Deleted += OnBinChanged;
                _watchers.Add(watcher);
            }
        }

        internal static bool ClearBin()
        {
            var parentWindow = IntPtr.Zero;
            const string? pszRootPath = null; //need to clear data from all disks

            const uint flags = 0; // Let Windows confirm before permanently deleting anything.

            int resultCode = NativeBinMethods.SHEmptyRecycleBinW(parentWindow, pszRootPath, flags);

            if (resultCode < 0 && resultCode != unchecked((int)0x800704C7))
                MessageBox.Show(Marshal.GetExceptionForHR(resultCode)?.Message ?? "无法清空回收站。",
                    "SimpleBin", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return true;
        }

        private void OnBinChanged(object? sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new(DebounceCallback, e, 500, Timeout.Infinite);
            }
        }

        private void DebounceCallback(object? state)
        {
            lock (_lock)
            {
                Update?.Invoke(this, (FileSystemEventArgs)state!);
                _debounceTimer!.Dispose();
                _debounceTimer = null;
            }
        }

        public bool IsEmpty()
        {
            foreach (var watcher in _watchers)
            {
                try
                {
                    if (Directory.EnumerateFileSystemEntries(watcher.Path, "$I*").Any())
                        return false;
                }
                catch (DirectoryNotFoundException) { }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
            return true;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_lock)
                {
                    _debounceTimer?.Dispose();
                    _debounceTimer = null;
                }

                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }

                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}


