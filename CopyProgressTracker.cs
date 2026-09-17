using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace GoProImport
{
    internal class DeviceCopyStatus
    {
        public string DeviceName { get; set; } = string.Empty;
        public int CurrentFileIndex { get; set; }
        public int TotalFiles { get; set; }
        public string CurrentFileName { get; set; } = string.Empty;
        public long CurrentFileBytesCopied { get; set; }
        public long CurrentFileTotalBytes { get; set; }
        public long DeviceBytesCopied { get; set; }
        public long DeviceTotalBytes { get; set; }
        public bool IsCompleted { get; set; }
    }

    internal class CopyProgressTracker : IDisposable
    {
        private readonly long _totalBytes;
        private readonly int _totalFiles;
        private long _totalBytesCopied;
        private int _completedFiles;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private readonly ConcurrentDictionary<object, DeviceCopyStatus> _deviceStatuses = new();
        private readonly object _renderLock = new object();
        private readonly Timer _renderTimer;

        private long _lastSampleBytes = 0;
        private double _lastSampleTimeSeconds = 0;
        private double _instantaneousSpeedMBps = 0;
        private int _renderedLinesCount = 0;
        private bool _isFinished = false;

        public CopyProgressTracker(long totalBytes, int totalFiles)
        {
            _totalBytes = totalBytes;
            _totalFiles = totalFiles;
            _stopwatch.Start();

            if (!Console.IsOutputRedirected)
            {
                _renderTimer = new Timer(_ => RenderInteractive(), null, 150, 150);
            }
        }

        public void RegisterDevice(object deviceKey, string deviceName, int totalFiles, long deviceTotalBytes)
        {
            _deviceStatuses[deviceKey] = new DeviceCopyStatus
            {
                DeviceName = deviceName,
                TotalFiles = totalFiles,
                DeviceTotalBytes = deviceTotalBytes
            };
        }

        public void OnFileStart(object deviceKey, string fileName, long fileSizeBytes, int fileIndex)
        {
            if (_deviceStatuses.TryGetValue(deviceKey, out var status))
            {
                lock (status)
                {
                    status.CurrentFileName = fileName;
                    status.CurrentFileIndex = fileIndex;
                    status.CurrentFileTotalBytes = fileSizeBytes;
                    status.CurrentFileBytesCopied = 0;
                }
            }

            if (Console.IsOutputRedirected)
            {
                var devName = status?.DeviceName ?? "Device";
                Console.WriteLine($"[{devName}] Starting file {fileIndex}/{(status?.TotalFiles ?? 0)}: {fileName} ({FormatSize(fileSizeBytes)})...");
            }
        }

        public void OnBytesCopied(object deviceKey, long bytesRead)
        {
            Interlocked.Add(ref _totalBytesCopied, bytesRead);

            if (_deviceStatuses.TryGetValue(deviceKey, out var status))
            {
                lock (status)
                {
                    status.CurrentFileBytesCopied += bytesRead;
                    status.DeviceBytesCopied += bytesRead;
                }
            }
        }

        public void OnFileCompleted(object deviceKey, string fileName, bool success)
        {
            Interlocked.Increment(ref _completedFiles);

            if (Console.IsOutputRedirected)
            {
                var totalCopied = Interlocked.Read(ref _totalBytesCopied);
                var percent = _totalBytes > 0 ? (int)(totalCopied * 100 / _totalBytes) : 100;
                var statusStr = success ? "Finished" : "FAILED";
                var devName = _deviceStatuses.TryGetValue(deviceKey, out var s) ? s.DeviceName : "Device";
                Console.WriteLine($"[{devName}] {statusStr} {fileName}. Overall: {percent}% ({FormatSize(totalCopied)} / {FormatSize(_totalBytes)})");
            }
        }

        public void OnDeviceCompleted(object deviceKey)
        {
            if (_deviceStatuses.TryGetValue(deviceKey, out var status))
            {
                lock (status)
                {
                    status.IsCompleted = true;
                }
            }

            if (Console.IsOutputRedirected)
            {
                var devName = status?.DeviceName ?? "Device";
                Console.WriteLine($"[{devName}] Completed all files ({FormatSize(status?.DeviceTotalBytes ?? 0)}).");
            }
        }

        public void Finish()
        {
            _isFinished = true;
            _renderTimer?.Dispose();
            _stopwatch.Stop();

            lock (_renderLock)
            {
                var totalBytesCopied = Interlocked.Read(ref _totalBytesCopied);
                var elapsed = _stopwatch.Elapsed;
                var avgSpeedMBps = elapsed.TotalSeconds > 0
                    ? (totalBytesCopied / (1024.0 * 1024.0)) / elapsed.TotalSeconds
                    : 0;

                if (!Console.IsOutputRedirected)
                {
                    var lines = new List<string>
                    {
                        $"Overall: {GenerateProgressBar(100, 40)} 100% ({FormatSize(totalBytesCopied)} / {FormatSize(_totalBytes)})",
                        $"Copying done in {elapsed:mm\\:ss} (average speed: {avgSpeedMBps.ToString("0.0", CultureInfo.InvariantCulture)} MB/s).",
                        string.Empty
                    };
                    DrawLines(lines);
                    _renderedLinesCount = 0;
                }
                else
                {
                    Console.WriteLine($"Copying done! Total copied: {FormatSize(totalBytesCopied)} in {elapsed:mm\\:ss} ({avgSpeedMBps.ToString("0.0", CultureInfo.InvariantCulture)} MB/s).");
                }
            }
        }

        private void RenderInteractive()
        {
            if (_isFinished || Console.IsOutputRedirected) return;

            if (!Monitor.TryEnter(_renderLock)) return;
            try
            {
                if (_isFinished) return;

                var totalBytesCopied = Interlocked.Read(ref _totalBytesCopied);
                var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

                // Update sample
                if (elapsedSeconds - _lastSampleTimeSeconds >= 0.5)
                {
                    var deltaBytes = totalBytesCopied - _lastSampleBytes;
                    var deltaTime = elapsedSeconds - _lastSampleTimeSeconds;
                    if (deltaTime > 0)
                    {
                        _instantaneousSpeedMBps = (deltaBytes / (1024.0 * 1024.0)) / deltaTime;
                    }
                    _lastSampleBytes = totalBytesCopied;
                    _lastSampleTimeSeconds = elapsedSeconds;
                }

                var avgSpeedMBps = elapsedSeconds > 0 ? (totalBytesCopied / (1024.0 * 1024.0)) / elapsedSeconds : 0;
                var effectiveSpeed = _instantaneousSpeedMBps > 0.05 ? _instantaneousSpeedMBps : avgSpeedMBps;
                var remainingBytes = Math.Max(0, _totalBytes - totalBytesCopied);
                var etaSeconds = effectiveSpeed > 0 ? (remainingBytes / (1024.0 * 1024.0)) / effectiveSpeed : 0;
                var etaStr = effectiveSpeed > 0.05 && remainingBytes > 0
                    ? (etaSeconds >= 3600
                        ? TimeSpan.FromSeconds(etaSeconds).ToString(@"hh\:mm\:ss")
                        : TimeSpan.FromSeconds(etaSeconds).ToString(@"mm\:ss"))
                    : "--:--";

                int percent = _totalBytes > 0 ? (int)(totalBytesCopied * 100 / _totalBytes) : 100;
                percent = Math.Clamp(percent, 0, 100);

                var lines = new List<string>
                {
                    $"Overall: {GenerateProgressBar(percent, 40)} {percent}% ({FormatSize(totalBytesCopied)} / {FormatSize(_totalBytes)})",
                    $"Speed: {avgSpeedMBps.ToString("0.0", CultureInfo.InvariantCulture)} MB/s (live: {_instantaneousSpeedMBps.ToString("0.0", CultureInfo.InvariantCulture)} MB/s) | ETA: {etaStr}"
                };

                if (_deviceStatuses.Count > 1)
                {
                    foreach (var kvp in _deviceStatuses.OrderBy(k => k.Value.DeviceName))
                    {
                        var s = kvp.Value;
                        lock (s)
                        {
                            if (s.IsCompleted)
                            {
                                lines.Add($"  [{s.DeviceName}] Done ({s.TotalFiles} files, {FormatSize(s.DeviceTotalBytes)})");
                            }
                            else
                            {
                                var filePercent = s.CurrentFileTotalBytes > 0
                                    ? (int)(s.CurrentFileBytesCopied * 100 / s.CurrentFileTotalBytes)
                                    : 100;
                                filePercent = Math.Clamp(filePercent, 0, 100);
                                lines.Add($"  [{s.DeviceName}] File {s.CurrentFileIndex}/{s.TotalFiles}: {s.CurrentFileName} {GenerateProgressBar(filePercent, 15)} {filePercent}%");
                            }
                        }
                    }
                }
                else if (_deviceStatuses.Count == 1)
                {
                    var s = _deviceStatuses.Values.First();
                    lock (s)
                    {
                        if (!s.IsCompleted && !string.IsNullOrEmpty(s.CurrentFileName))
                        {
                            var filePercent = s.CurrentFileTotalBytes > 0
                                ? (int)(s.CurrentFileBytesCopied * 100 / s.CurrentFileTotalBytes)
                                : 100;
                            filePercent = Math.Clamp(filePercent, 0, 100);
                            lines.Add($"  Copying file {s.CurrentFileIndex} of {s.TotalFiles}: {s.CurrentFileName} {GenerateProgressBar(filePercent, 20)} {filePercent}% ({FormatSize(s.CurrentFileBytesCopied)} / {FormatSize(s.CurrentFileTotalBytes)})");
                        }
                    }
                }

                DrawLines(lines);
            }
            finally
            {
                Monitor.Exit(_renderLock);
            }
        }

        private void DrawLines(List<string> lines)
        {
            if (Console.IsOutputRedirected) return;

            try
            {
                int windowWidth = 80;
                try
                {
                    windowWidth = Console.WindowWidth;
                }
                catch
                {
                    windowWidth = 80;
                }

                int linesToRender = Math.Max(_renderedLinesCount, lines.Count);

                if (_renderedLinesCount > 0)
                {
                    int targetTop = Math.Max(0, Console.CursorTop - _renderedLinesCount);
                    Console.SetCursorPosition(0, targetTop);
                }

                for (int i = 0; i < linesToRender; i++)
                {
                    var line = i < lines.Count ? lines[i] : string.Empty;
                    var truncated = line.Length >= windowWidth ? line.Substring(0, Math.Max(0, windowWidth - 1)) : line;
                    var padded = truncated.PadRight(Math.Max(0, windowWidth - 1));
                    Console.WriteLine(padded);
                }

                _renderedLinesCount = lines.Count;
            }
            catch
            {
                // Ignore cursor movement failures in unusual console environments
            }
        }

        public static string GenerateProgressBar(int percent, int totalChars = 40)
        {
            percent = Math.Clamp(percent, 0, 100);
            int filled = (int)Math.Round(percent * totalChars / 100.0);
            filled = Math.Clamp(filled, 0, totalChars);
            return "[" + new string('#', filled) + new string('-', totalChars - filled) + "]";
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
            {
                return (bytes / (double)(1024L * 1024L * 1024L)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " GB";
            }
            if (bytes >= 1024L * 1024L)
            {
                return (bytes / (double)(1024L * 1024L)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " MB";
            }
            if (bytes >= 1024L)
            {
                return (bytes / (double)1024L).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " KB";
            }
            return $"{bytes} B";
        }

        public static void SafeSetCursorVisibility(bool visible)
        {
            if (!Console.IsOutputRedirected)
            {
                try
                {
                    Console.CursorVisible = visible;
                }
                catch { }
            }
        }

        public void Dispose()
        {
            _renderTimer?.Dispose();
        }
    }
}
