using System.Runtime.InteropServices;
using System.Security.Principal;
using Timer = System.Threading.Timer;

namespace SimpleBin
{
    static partial class NativeBinMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct RecycleBinInfo
        {
            internal uint Size;
            internal long Bytes;
            internal long Items;
        }

        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SHQueryRecycleBinW(string? rootPath, ref RecycleBinInfo info);

        [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SHEmptyRecycleBinW(
             IntPtr hwnd,
             string? pszRootPath,
             uint dwFlags);
    }

    internal sealed class BinHelper : IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = [];
        public event EventHandler? Update;
        private Timer? _debounceTimer;
        private readonly Lock _lock = new();
        private bool _isDisposed;

        public BinHelper()
        {
            using var identity = WindowsIdentity.GetCurrent();
            string sid = identity.User?.Value
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
                    InternalBufferSize = 4096,
                    Filters = { "$I*", "$R*" },
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                };

                watcher.Created += OnBinChanged;
                watcher.Deleted += OnBinChanged;
                watcher.Renamed += OnBinChanged;
                watcher.Error += (_, _) => OnBinChanged(null, null);
                _watchers.Add(watcher);
                watcher.EnableRaisingEvents = true;
            }
        }

        internal static void ClearBin(nint parentWindow)
        {
            const string? pszRootPath = null; //need to clear data from all disks

            const uint flags = 0; // Let Windows confirm before permanently deleting anything.

            int resultCode = NativeBinMethods.SHEmptyRecycleBinW(parentWindow, pszRootPath, flags);

            if (resultCode < 0 && resultCode != unchecked((int)0x800704C7))
                MessageBox.Show(Marshal.GetExceptionForHR(resultCode)?.Message ?? "无法清空回收站。",
                    "SimpleBin", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        private void OnBinChanged(object? sender, FileSystemEventArgs? e)
        {
            lock (_lock)
            {
                if (_isDisposed) return;
                _debounceTimer ??= new(DebounceCallback);
                _debounceTimer.Change(500, Timeout.Infinite);
            }
        }

        private void DebounceCallback(object? state)
        {
            lock (_lock)
            {
                if (!_isDisposed)
                    Update?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsEmpty()
        {
            var info = new NativeBinMethods.RecycleBinInfo
            {
                Size = (uint)Marshal.SizeOf<NativeBinMethods.RecycleBinInfo>()
            };
            // Orphaned $I metadata can remain after Windows has emptied the bin.
            Marshal.ThrowExceptionForHR(NativeBinMethods.SHQueryRecycleBinW(null, ref info));
            return info.Items == 0;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_lock)
                {
                    _isDisposed = true;
                    _debounceTimer?.Dispose();
                    _debounceTimer = null;
                }

                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }


            }
        }
    }
}
