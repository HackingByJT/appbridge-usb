using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AppBridgeUsb
{
    internal sealed class RuntimeManager
    {
        private const string VersionDirectory = "scrcpy-win64-v5.0.1";
        private const string DownloadUrl = "https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip";
        private const string ExpectedHash = "b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5";
        private const long MaximumDownload = 80L * 1024 * 1024;
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static readonly string[] RequiredFiles = {
            "scrcpy.exe", "scrcpy-server", "SDL3.dll", "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll",
            "avcodec-63.dll", "avformat-63.dll", "avutil-61.dll", "swresample-7.dll", "libusb-1.0.dll", "LICENSE.txt"
        };
        private readonly string root;

        public RuntimeManager()
        {
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppBridgeUSB", "runtime");
        }

        public string ToolsDirectory { get { return Path.Combine(root, VersionDirectory); } }
        public string AdbPath { get { return Path.Combine(ToolsDirectory, "adb.exe"); } }
        public string ScrcpyPath { get { return Path.Combine(ToolsDirectory, "scrcpy.exe"); } }
        public bool IsReady
        {
            get
            {
                try
                {
                    foreach (string name in RequiredFiles)
                        if (!File.Exists(Path.Combine(ToolsDirectory, name))) return false;
                    string marker = Path.Combine(ToolsDirectory, ".appbridge-runtime.txt");
                    return File.Exists(marker) && File.ReadAllText(marker).Trim() == ExpectedHash;
                }
                catch (IOException) { return false; }
                catch (UnauthorizedAccessException) { return false; }
            }
        }

        public async Task EnsureAsync(IProgress<int> progress, CancellationToken cancellationToken)
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            string archive = null, staging = null;
            bool archiveOwned = false, stagingOwned = false;
            try
            {
                if (IsReady) { Report(progress, 100); return; }
                Directory.CreateDirectory(root);
                string unique = Guid.NewGuid().ToString("N");
                archive = Path.Combine(root, ".download-" + unique + ".zip");
                staging = Path.Combine(root, ".staging-" + unique);
                using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                using (HttpClient client = new HttpClient())
                {
                    timeout.CancelAfter(TimeSpan.FromMinutes(3));
                    client.Timeout = TimeSpan.FromMinutes(3);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("AppBridgeUSB/0.1.0");
                    try
                    {
                        using (HttpResponseMessage response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                        // Framework streams may ignore cancellation during an active read.
                        // Closing the response also interrupts a stalled response body.
                        using (CancellationTokenRegistration abort = timeout.Token.Register(delegate { response.Dispose(); }))
                        {
                            response.EnsureSuccessStatusCode();
                            long? expectedSize = response.Content.Headers.ContentLength;
                            if (expectedSize.HasValue && expectedSize.Value > MaximumDownload)
                                throw new InvalidDataException("The component download is larger than expected.");
                            using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (FileStream output = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                            {
                                archiveOwned = true;
                                byte[] buffer = new byte[81920];
                                long total = 0;
                                int count;
                                while ((count = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                                {
                                    total += count;
                                    if (total > MaximumDownload) throw new InvalidDataException("The component download is larger than expected.");
                                    await output.WriteAsync(buffer, 0, count, timeout.Token).ConfigureAwait(false);
                                    Report(progress, expectedSize.HasValue && expectedSize.Value > 0 ? (int)Math.Min(65, total * 65 / expectedSize.Value) : 20);
                                }
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        if (!timeout.IsCancellationRequested ||
                            !(error is IOException || error is ObjectDisposedException || error is HttpRequestException)) throw;
                        throw new OperationCanceledException("Component preparation was cancelled or timed out.", error, timeout.Token);
                    }
                    timeout.Token.ThrowIfCancellationRequested();
                    string actualHash;
                    using (FileStream input = File.OpenRead(archive))
                    using (SHA256 sha = SHA256.Create())
                        actualHash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                    if (!String.Equals(actualHash, ExpectedHash, StringComparison.Ordinal))
                        throw new InvalidDataException("The download did not match the published SHA-256. No components were installed.");
                    Report(progress, 75);
                    if (Directory.Exists(staging)) throw new IOException("The temporary component folder already exists.");
                    Directory.CreateDirectory(staging);
                    stagingOwned = true;
                    ExtractKnownFiles(archive, staging, timeout.Token);
                    File.WriteAllText(Path.Combine(staging, ".appbridge-runtime.txt"), ExpectedHash);
                    timeout.Token.ThrowIfCancellationRequested();
                    if (IsReady) { Report(progress, 100); return; }
                    if (Directory.Exists(ToolsDirectory))
                    {
                        // Keep an incomplete old runtime as a recoverable sibling rather than deleting it.
                        string previous = Path.Combine(root, ".previous-" + Guid.NewGuid().ToString("N"));
                        Directory.Move(ToolsDirectory, previous);
                    }
                    Directory.Move(staging, ToolsDirectory);
                    stagingOwned = false;
                    Report(progress, 100);
                }
            }
            finally
            {
                if (archiveOwned) TryRemoveTemporary(archive, false);
                if (stagingOwned) TryRemoveTemporary(staging, true);
                Gate.Release();
            }
        }

        private static void ExtractKnownFiles(string archivePath, string destination, CancellationToken token)
        {
            HashSet<string> wanted = new HashSet<string>(RequiredFiles, StringComparer.Ordinal);
            wanted.Add("scrcpy.png");
            wanted.Add("disconnected.png");
            HashSet<string> extracted = new HashSet<string>(StringComparer.Ordinal);
            long expanded = 0;
            using (ZipArchive zip = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    string prefix = VersionDirectory + "/";
                    if (!entry.FullName.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    string name = entry.FullName.Substring(prefix.Length);
                    if (!wanted.Contains(name)) continue;
                    if (!extracted.Add(name)) throw new InvalidDataException("The component archive contains duplicate files.");
                    expanded += entry.Length;
                    if (entry.Length < 0 || expanded > 120L * 1024 * 1024)
                        throw new InvalidDataException("The component archive is larger than expected.");
                    // The allowlist contains only basenames: archive paths never select a destination.
                    string target = Path.Combine(destination, name);
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
                        input.CopyTo(output);
                }
            }
            foreach (string name in RequiredFiles)
                if (!extracted.Contains(name)) throw new InvalidDataException("A required component is missing: " + name);
        }

        private void TryRemoveTemporary(string path, bool directory)
        {
            if (String.IsNullOrEmpty(path)) return;
            string fullPath = Path.GetFullPath(path);
            string parent = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileName(fullPath);
            // Cleanup is restricted to our unique, direct-child temporary paths.
            if (!String.Equals(parent, Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) return;
            if (directory ? !name.StartsWith(".staging-", StringComparison.Ordinal) : !name.StartsWith(".download-", StringComparison.Ordinal)) return;
            try
            {
                if (directory && Directory.Exists(fullPath)) Directory.Delete(fullPath, true);
                else if (!directory && File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Report(IProgress<int> progress, int value)
        {
            if (progress != null) progress.Report(value);
        }
    }
}
