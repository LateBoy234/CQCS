using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace ThreeNH.Helper.Plugin.Services
{
    /// <summary>
    /// 日志服务全局选项。
    /// </summary>
    public sealed class LogServiceOptions
    {
        /// <summary>日志根目录名称，位于程序运行目录下，默认 Log。</summary>
        public string LogFolderName { get; set; } = "Log";

        /// <summary>保留最近多少天的日志目录，超出后整目录删除。</summary>
        public int MaxRetentionDays { get; set; } = 30;
    }

    /// <summary>
    /// 单个日志通道配置。
    /// </summary>
    public sealed class LogChannelOptions
    {
        public LogChannelOptions()
        {
        }

        public LogChannelOptions(string channelKey, string fileName, long maxFileSizeBytes =6* 1024 * 1024)
        {
            ChannelKey = channelKey;
            FileName = fileName;
            MaxFileSizeBytes = maxFileSizeBytes;
        }

        /// <summary>通道标识，写入日志时使用。</summary>
        public string ChannelKey { get; set; } = string.Empty;

        /// <summary>日志文件名，例如 serial.log、PLCMonitor.log。</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>单文件最大字节数，超出后删除该文件再重新写入。</summary>
        public long MaxFileSizeBytes { get; set; } =6* 1024 * 1024;
    }

    /// <summary>
    /// 常用日志通道键名，便于统一引用。
    /// </summary>
    public static class LogChannels
    {
        public const string Serial = "Serial";
        public const string PlcMonitor = "PlcMonitor";
    }

    /// <summary>
    /// 按天、按通道写入日志文件。目录结构：{运行目录}/Log/{yyyy-MM-dd}/{fileName}。
    /// </summary>
    public sealed class LogService
    {
        private static readonly object SyncRoot = new object();
        private static LogService _default;

        private readonly object _writeLock = new object();
        private LogServiceOptions _options = new LogServiceOptions();
        private readonly Dictionary<string, LogChannelOptions> _channels =
            new Dictionary<string, LogChannelOptions>(StringComparer.OrdinalIgnoreCase);

        private string _lastCleanupDate = string.Empty;

        /// <summary>默认实例，首次访问时使用常见通道初始化。</summary>
        public static LogService Default
        {
            get
            {
                if (_default != null)
                    return _default;

                lock (SyncRoot)
                {
                    if (_default != null)
                        return _default;

                    var service = new LogService();
                    service.Initialize(
                        new LogServiceOptions(),
                        new LogChannelOptions(LogChannels.Serial, "serial.log"),
                        new LogChannelOptions(LogChannels.PlcMonitor, "PLCMonitor.log"));
                    _default = service;
                    return _default;
                }
            }
        }

        /// <summary>
        /// 使用自定义选项与通道配置初始化日志服务。
        /// </summary>
        public void Initialize(LogServiceOptions options, params LogChannelOptions[] channels)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (channels == null || channels.Length == 0)
                throw new ArgumentException("至少注册一个日志通道。", nameof(channels));

            lock (_writeLock)
            {
                _options = options;
                _channels.Clear();

                foreach (var channel in channels)
                {
                    ValidateChannel(channel);
                    _channels[channel.ChannelKey] = channel;
                }

                EnsureLogRootExists();
                CleanExpiredLogDirectories();
            }
        }

        /// <summary>注册或更新单个通道。</summary>
        public void RegisterChannel(LogChannelOptions channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            lock (_writeLock)
            {
                ValidateChannel(channel);
                _channels[channel.ChannelKey] = channel;
                EnsureLogRootExists();
            }
        }

        /// <summary>写入一行日志（自动附加时间戳）。</summary>
        public void WriteLine(string channelKey, string message)
        {
            WriteCore(channelKey, BuildLine(message));
        }

        /// <summary>写入原始文本（不附加时间戳与换行）。</summary>
        public void Write(string channelKey, string message)
        {
            WriteCore(channelKey, message ?? string.Empty);
        }

        /// <summary>写入异常信息。</summary>
        public void WriteException(string channelKey, Exception exception, string message = null)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message))
                builder.AppendLine(message);

            if (exception != null)
            {
                builder.AppendLine(exception.Message);
                builder.Append(exception.StackTrace);
            }

            WriteCore(channelKey, BuildLine(builder.ToString()));
        }

        /// <summary>获取指定通道当天日志文件的完整路径。</summary>
        public string GetTodayLogFilePath(string channelKey)
        {
            lock (_writeLock)
            {
                var channel = GetChannel(channelKey);
                return BuildLogFilePath(channel, DateTime.Now);
            }
        }

        /// <summary>获取日志根目录完整路径。</summary>
        public string GetLogRootPath()
        {
            return Path.Combine(GetAppPath(), _options.LogFolderName ?? "Log");
        }

        private void WriteCore(string channelKey, string content)
        {
            if (string.IsNullOrEmpty(channelKey))
                throw new ArgumentNullException(nameof(channelKey));

            lock (_writeLock)
            {
                var channel = GetChannel(channelKey);
                var now = DateTime.Now;
                TryCleanExpiredLogs(now);

                var directory = BuildDayDirectory(now);
                Directory.CreateDirectory(directory);

                var filePath = BuildLogFilePath(channel, now);
                TrimOversizedFile(filePath, channel.MaxFileSizeBytes);
                File.AppendAllText(filePath, content, Encoding.UTF8);
            }
        }

        private LogChannelOptions GetChannel(string channelKey)
        {
            LogChannelOptions channel;
            if (!_channels.TryGetValue(channelKey, out channel))
                throw new KeyNotFoundException("未注册的日志通道：" + channelKey);
            return channel;
        }

        private static void ValidateChannel(LogChannelOptions channel)
        {
            if (string.IsNullOrWhiteSpace(channel.ChannelKey))
                throw new ArgumentException("ChannelKey 不能为空。");
            if (string.IsNullOrWhiteSpace(channel.FileName))
                throw new ArgumentException("FileName 不能为空。");
            if (channel.MaxFileSizeBytes <= 0)
                throw new ArgumentException("MaxFileSizeBytes 必须大于 0。");
        }

        private string BuildDayDirectory(DateTime date)
        {
            return Path.Combine(GetLogRootPath(), date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private string BuildLogFilePath(LogChannelOptions channel, DateTime date)
        {
            return Path.Combine(BuildDayDirectory(date), channel.FileName);
        }

        private static string BuildLine(string message)
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss    ", CultureInfo.InvariantCulture)
                   + (message ?? string.Empty)
                   + Environment.NewLine;
        }

        private void TryCleanExpiredLogs(DateTime now)
        {
            var todayKey = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (string.Equals(_lastCleanupDate, todayKey, StringComparison.Ordinal))
                return;

            CleanExpiredLogDirectories();
            _lastCleanupDate = todayKey;
        }

        private void CleanExpiredLogDirectories()
        {
            if (_options.MaxRetentionDays <= 0)
                return;

            var root = GetLogRootPath();
            if (!Directory.Exists(root))
                return;

            var cutoff = DateTime.Today.AddDays(-(_options.MaxRetentionDays - 1));
            foreach (var directory in Directory.GetDirectories(root))
            {
                var folderName = Path.GetFileName(directory);
                DateTime folderDate;
                if (!DateTime.TryParseExact(
                        folderName,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out folderDate))
                {
                    continue;
                }

                if (folderDate < cutoff)
                    TryDeleteDirectory(directory);
            }
        }

        private void EnsureLogRootExists()
        {
            Directory.CreateDirectory(GetLogRootPath());
        }

        private static void TrimOversizedFile(string filePath, long maxFileSizeBytes)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (info.Exists && info.Length > maxFileSizeBytes)
                    File.Delete(filePath);
            }
            catch (Exception)
            {
                // 删除失败时不阻断后续写入
            }
        }

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (Exception)
            {
                // 目录占用等情况忽略
            }
        }

        private static string GetAppPath()
        {
            try
            {
                var module = Process.GetCurrentProcess().MainModule;
                if (module != null && !string.IsNullOrEmpty(module.FileName))
                {
                    var directory = Path.GetDirectoryName(module.FileName);
                    if (!string.IsNullOrEmpty(directory))
                        return directory;
                }
            }
            catch (Exception)
            {
                // ignore and fallback
            }

            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        }
    }
}
