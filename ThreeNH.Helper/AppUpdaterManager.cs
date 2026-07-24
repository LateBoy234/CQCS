using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeNH.Helper.Plugin.Services;

namespace ThreeNH.Helper
{
    public class AppUpdaterManager
    {
        private readonly string _serverUrl;
        private readonly string _appId;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 初始化应用更新管理器。
        /// </summary>
        /// <param name="serverUrl">更新服务器地址。</param>
        /// <param name="appId">服务器端登记的应用标识。</param>
        public AppUpdaterManager(string serverUrl, string appId)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _appId = appId;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        }

        /// <summary>
        /// 向更新服务器查询当前应用是否存在新版本。
        /// </summary>
        /// <param name="currentVersion">当前本地应用版本号。</param>
        /// <returns>更新检查结果，包含是否有更新、本地版本和远端版本。</returns>
        public async Task<UpdateCheckResult> CheckUpdateAsync(string currentVersion)
        {
            try
            {
                var requestBody = new
                {
                    appInfoId = _appId,
                    platformType = 1,
                    curVersion = currentVersion
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serverUrl}/api/app/appversions/publish", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var updateInfo = JsonConvert.DeserializeObject<AppVersionResponse>(responseJson);

                if (updateInfo == null || string.IsNullOrEmpty(updateInfo.Version))
                    return new UpdateCheckResult { HasUpdate = false };

                var remoteVersion = new Version(updateInfo.Version);
                var localVersion = new Version(currentVersion);

                return new UpdateCheckResult
                {
                    HasUpdate = remoteVersion > localVersion,
                    UpdateInfo = updateInfo,
                    RemoteVersion = remoteVersion,
                    LocalVersion = localVersion
                };
            }
            catch (Exception ex)
            {
                Trace.TraceError($"检查更新失败: {ex.Message}");
                return new UpdateCheckResult { HasUpdate = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 下载 ZIP 更新包并启动覆盖更新流程。
        /// </summary>
        /// <param name="updateInfo">服务器返回的新版本信息。</param>
        /// <param name="loadingParams">下载进度窗口显示参数。</param>
        /// <returns>下载并启动更新流程成功返回 true；用户取消下载返回 false。</returns>
        public async Task<bool> DownloadAndInstallAsync(AppVersionResponse updateInfo, LoadingManageParams loadingParams)
        {
            string packagePath = Path.Combine(Path.GetTempPath(), GetTempPackageFileName(updateInfo));
            CancellationTokenSource cts = null; // 提到外面，让取消能控制

            try
            {
                using (var response = await _httpClient.GetAsync(updateInfo.Url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.FileSize;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(packagePath, FileMode.Create, FileAccess.Write, FileShare.None, 256 * 1024, FileOptions.Asynchronous))
                    {
                        var buffer = new byte[256 * 1024];
                        int read;
                        cts = new CancellationTokenSource(); // 全局取消令牌

                        using (var loading = new LoadingService())
                        {
                            // 绑定取消事件
                            loading.Cancelled += () =>
                            {
                                cts.Cancel(); // 真正触发取消
                            };

                            loading.Show();
                            loading.Caption = loadingParams.DownloadCaption;
                            loading.CancelContent = loadingParams.DownloadCancelText;

                            try
                            {
                                // 把 cts.Token 传给下载任务
                                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                                {
                                    // 检查是否取消
                                    cts.Token.ThrowIfCancellationRequested();

                                    await fileStream.WriteAsync(buffer, 0, read, cts.Token);
                                    downloadedBytes += read;

                                    loading.Progress = (int)((downloadedBytes * 100) / totalBytes);
                                    loading.Message = loadingParams.DownloadMessage;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // 点击取消 → 正常进入这里
                                Trace.TraceError("已取消更新");
                                return false;
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError($"下载更新失败: {ex.Message}"); 
                                return false;
                            }
                            finally
                            {
                                loading.Close();
                            }
                        }
                    }
                }

                if (!IsZipPackage(packagePath))
                {
                    throw new InvalidOperationException($"更新包不是 ZIP 文件: {Path.GetFileName(packagePath)}");
                }

                // ZIP 覆盖更新不会触发安装器，因此需要把版本号传给批处理去同步控制面板。
                ExecuteUpdate(packagePath, updateInfo.Version);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"下载更新失败: {ex.Message}");
                try { File.Delete(packagePath); } catch { }
                throw;
            }
        }

        /// <summary>
        /// 创建并启动自更新批处理脚本。
        /// </summary>
        /// <param name="zipPath">已下载到本地临时目录的 ZIP 更新包路径。</param>
        /// <param name="version">新版本号，用于同步控制面板显示版本。</param>
        private void ExecuteUpdate(string zipPath, string version)
        {
            // 获取当前运行程序的完整路径
            string exePath = Assembly.GetEntryAssembly().Location;
            // 获取程序所在目录
            string appDir = Path.GetDirectoryName(exePath);
            // 获取程序文件名
            string exeName = Path.GetFileName(exePath);
            // 生成唯一的临时批处理文件路径
            string batchPath = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid()}.bat");

            // 创建批处理脚本
            var sb = new StringBuilder();

            sb.AppendLine("@echo off");// 关闭批处理命令回显（执行时不显示命令本身）
            sb.AppendLine("chcp 65001 >nul"); // UTF-8 编码
            sb.AppendLine("echo 等待程序关闭...");// 提示用户
            sb.AppendLine("timeout /t 2 /nobreak >nul");// 等待2秒，给程序退出留时间
            sb.AppendLine($"taskkill /f /im \"{exeName}\" 2>nul");// 强制杀死当前程序进程（确保程序完全关闭，避免文件被占用）
            sb.AppendLine("timeout /t 1 /nobreak >nul");// 再等1秒，确保进程彻底退出
            sb.AppendLine();
            sb.AppendLine("echo 正在解压更新...");
            // 方式1：用PowerShell解压ZIP包（Windows 10+自带，优先推荐）
            sb.AppendLine($"powershell -Command \"Expand-Archive -Path '{zipPath}' -DestinationPath '{appDir}' -Force\"");
            sb.AppendLine("if %errorlevel% neq 0 ("); // 检查解压是否失败（%errorlevel% 是非0表示失败）
            sb.AppendLine("    echo 解压失败，使用备用方式...");
            // 方式2：用tar命令解压（兼容旧系统，2>nul 屏蔽错误输出）
            sb.AppendLine($"    tar -xf \"{zipPath}\" -C \"{appDir}\" 2>nul || exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine();
            sb.AppendLine("echo 正在更新控制面板版本...");
            // 控制面板显示的版本来自卸载注册表项，不会随着 EXE 文件覆盖自动变化。
            AppendUninstallRegistryUpdateCommands(sb, exePath, appDir, version);
            sb.AppendLine();
            sb.AppendLine("echo 启动程序...");
            sb.AppendLine($"start \"\" \"{exePath}\"");// 重启更新后的程序（"" 是窗口标题，避免路径含空格报错）
            sb.AppendLine();
            sb.AppendLine("echo 清理临时文件...");
            sb.AppendLine($"del \"{zipPath}\" 2>nul");// 删除更新包（ZIP文件）
            sb.AppendLine($"del \"%~f0\" 2>nul");// 删除批处理文件自身（% ~f0 是当前bat文件路径）

            File.WriteAllText(batchPath, sb.ToString(), Encoding.UTF8);
            // 启动批处理并退出
            try
            {
                // Inno 安装时通常写 HKLM，修改控制面板版本也可能需要管理员权限。
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch
            {
                // 用户取消 UAC 时仍尝试普通权限更新；此时文件可能能覆盖，但 HKLM 版本号可能写不进去。
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    CreateNoWindow = true,// 不显示批处理窗口（静默更新）
                    UseShellExecute = false// 无壳执行，更稳定
                });
            }
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// 根据服务端文件名或下载 URL 生成临时更新包文件名。
        /// </summary>
        /// <param name="updateInfo">服务器返回的新版本信息。</param>
        /// <returns>带唯一 GUID 的临时文件名，默认扩展名为 .zip。</returns>
        private static string GetTempPackageFileName(AppVersionResponse updateInfo)
        {
            string extension = ".zip";
            string fileName = updateInfo?.FileName;

            if (string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(updateInfo?.Url))
            {
                Uri uri;
                if (Uri.TryCreate(updateInfo.Url, UriKind.Absolute, out uri))
                {
                    fileName = Path.GetFileName(uri.LocalPath);
                }
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string parsedExtension = Path.GetExtension(fileName);
                if (!string.IsNullOrWhiteSpace(parsedExtension))
                {
                    extension = parsedExtension;
                }
            }

            return $"update_{Guid.NewGuid()}{extension}";
        }

        /// <summary>
        /// 判断指定文件是否为 ZIP 更新包。
        /// </summary>
        /// <param name="fileName">需要判断的文件名或路径。</param>
        /// <returns>扩展名为 .zip 时返回 true。</returns>
        private static bool IsZipPackage(string fileName)
        {
            return string.Equals(Path.GetExtension(fileName), ".zip", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 向批处理脚本追加控制面板卸载项的版本同步命令。
        /// </summary>
        /// <param name="sb">批处理脚本文本构造器。</param>
        /// <param name="exePath">当前应用主程序路径。</param>
        /// <param name="appDir">当前应用安装目录。</param>
        /// <param name="version">需要写入控制面板的新版本号。</param>
        private static void AppendUninstallRegistryUpdateCommands(StringBuilder sb, string exePath, string appDir, string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return;

            // 当前程序有多个品牌包，不能硬编码 AppId；按安装目录/EXE 路径反查对应卸载项。
            var entries = FindUninstallRegistryEntries(exePath, appDir);
            foreach (var entry in entries)
            {
                string keyPath = $"{entry.RootName}\\{entry.SubKeyPath}";
                string viewArg = string.IsNullOrWhiteSpace(entry.RegViewArgument) ? "" : " " + entry.RegViewArgument;
                string displayName = MakeUpdatedDisplayName(entry.DisplayName, entry.DisplayVersion, version);

                sb.AppendLine($"reg add \"{keyPath}\" /v DisplayVersion /t REG_SZ /d \"{EscapeBatchValue(version)}\" /f{viewArg} >nul 2>nul");
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    sb.AppendLine($"reg add \"{keyPath}\" /v DisplayName /t REG_SZ /d \"{EscapeBatchValue(displayName)}\" /f{viewArg} >nul 2>nul");
                }
                sb.AppendLine($"reg add \"{keyPath}\" /v InstallDate /t REG_SZ /d \"{DateTime.Now:yyyyMMdd}\" /f{viewArg} >nul 2>nul");
            }
        }

        /// <summary>
        /// 查找当前应用对应的 Windows 卸载注册表项。
        /// </summary>
        /// <param name="exePath">当前应用主程序路径。</param>
        /// <param name="appDir">当前应用安装目录。</param>
        /// <returns>匹配到的卸载注册表项列表，可能同时包含 HKCU/HKLM 和不同注册表视图。</returns>
        private static List<UninstallRegistryEntry> FindUninstallRegistryEntries(string exePath, string appDir)
        {
            var result = new List<UninstallRegistryEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hives = new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine };
            // 同时扫描 32/64 位注册表视图，兼容 x86/x64 安装包写入位置不同的情况。
            var views = new[] { RegistryView.Default, RegistryView.Registry32, RegistryView.Registry64 };

            foreach (var hive in hives)
            {
                foreach (var view in views)
                {
                    try
                    {
                        using (var root = RegistryKey.OpenBaseKey(hive, view))
                        using (var uninstallRoot = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                        {
                            if (uninstallRoot == null)
                                continue;

                            foreach (string subKeyName in uninstallRoot.GetSubKeyNames())
                            {
                                using (var subKey = uninstallRoot.OpenSubKey(subKeyName))
                                {
                                    if (subKey == null || !IsMatchingUninstallEntry(subKey, exePath, appDir))
                                        continue;

                                    string rootName = hive == RegistryHive.CurrentUser ? "HKCU" : "HKLM";
                                    string subKeyPath = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{subKeyName}";
                                    string viewArg = GetRegViewArgument(view);
                                    string seenKey = $"{rootName}|{subKeyPath}|{viewArg}";
                                    if (!seen.Add(seenKey))
                                        continue;

                                    result.Add(new UninstallRegistryEntry
                                    {
                                        RootName = rootName,
                                        SubKeyPath = subKeyPath,
                                        RegViewArgument = viewArg,
                                        DisplayName = subKey.GetValue("DisplayName") as string,
                                        DisplayVersion = subKey.GetValue("DisplayVersion") as string
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        // HKLM 或某些视图可能没有权限读取，忽略后继续扫描其他位置。
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 判断一个卸载注册表项是否属于当前运行的应用。
        /// </summary>
        /// <param name="subKey">待检查的卸载注册表项。</param>
        /// <param name="exePath">当前应用主程序路径。</param>
        /// <param name="appDir">当前应用安装目录。</param>
        /// <returns>卸载项中的路径指向当前应用时返回 true。</returns>
        private static bool IsMatchingUninstallEntry(RegistryKey subKey, string exePath, string appDir)
        {
            // Inno 脚本可能写 DisplayIcon、InstallLocation 或 UninstallString，命中任一项即可认为是当前程序。
            return IsSamePath(GetRegistryPathValue(subKey, "DisplayIcon"), exePath)
                || IsSamePath(GetRegistryPathValue(subKey, "InstallLocation"), appDir)
                || IsPathInDirectory(GetRegistryPathValue(subKey, "UninstallString"), appDir);
        }

        /// <summary>
        /// 从注册表字符串值中提取可用于路径比较的真实路径。
        /// </summary>
        /// <param name="subKey">包含目标值的注册表项。</param>
        /// <param name="valueName">注册表值名称。</param>
        /// <returns>裁剪参数、图标索引和引号后的路径；没有有效值时返回 null。</returns>
        private static string GetRegistryPathValue(RegistryKey subKey, string valueName)
        {
            var value = subKey.GetValue(valueName) as string;
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // 注册表值可能形如 `"xxx.exe",0` 或 `"unins000.exe" /SILENT`，先裁出真实路径再比较。
            value = value.Trim().Trim('"');
            int exeIndex = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex >= 0)
                value = value.Substring(0, exeIndex + 4);

            int iconIndex = value.IndexOf(",", StringComparison.Ordinal);
            if (iconIndex >= 0)
                value = value.Substring(0, iconIndex);

            return value.Trim().Trim('"');
        }

        /// <summary>
        /// 比较两个路径是否指向同一文件或目录。
        /// </summary>
        /// <param name="left">左侧路径。</param>
        /// <param name="right">右侧路径。</param>
        /// <returns>规范化后路径相同时返回 true；路径无效时返回 false。</returns>
        private static bool IsSamePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            try
            {
                return string.Equals(
                    Path.GetFullPath(left).TrimEnd('\\'),
                    Path.GetFullPath(right).TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断指定路径是否位于目标目录下。
        /// </summary>
        /// <param name="path">待判断的文件路径。</param>
        /// <param name="directory">目标目录。</param>
        /// <returns>文件路径位于目录内时返回 true；路径无效时返回 false。</returns>
        private static bool IsPathInDirectory(string path, string directory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
                return false;

            try
            {
                string fullPath = Path.GetFullPath(path).TrimEnd('\\');
                string fullDirectory = Path.GetFullPath(directory).TrimEnd('\\') + "\\";
                return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将 .NET 注册表视图转换为 reg.exe 的视图参数。
        /// </summary>
        /// <param name="view">注册表视图。</param>
        /// <returns>/reg:32、/reg:64 或空字符串。</returns>
        private static string GetRegViewArgument(RegistryView view)
        {
            if (view == RegistryView.Registry32)
                return "/reg:32";

            if (view == RegistryView.Registry64)
                return "/reg:64";

            return "";
        }

        /// <summary>
        /// 根据旧显示名生成新的控制面板显示名。
        /// </summary>
        /// <param name="displayName">当前注册表中的 DisplayName。</param>
        /// <param name="oldVersion">当前注册表中的 DisplayVersion。</param>
        /// <param name="newVersion">需要写入的新版本号。</param>
        /// <returns>替换末尾版本号后的显示名；原显示名无效时返回 null。</returns>
        private static string MakeUpdatedDisplayName(string displayName, string oldVersion, string newVersion)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            // 保留产品名，只替换末尾版本号，避免生成 "SQCX 1.26.3.3 1.26.3.5"。
            string name = displayName.Trim();
            if (!string.IsNullOrWhiteSpace(oldVersion) && name.EndsWith(oldVersion, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - oldVersion.Length).Trim();
            }
            else
            {
                int lastSpace = name.LastIndexOf(' ');
                if (lastSpace > 0)
                {
                    string lastPart = name.Substring(lastSpace + 1);
                    Version ignored;
                    if (Version.TryParse(lastPart, out ignored))
                    {
                        name = name.Substring(0, lastSpace).Trim();
                    }
                }
            }

            return $"{name} {newVersion}";
        }

        /// <summary>
        /// 转义将要写入批处理命令的字符串值。
        /// </summary>
        /// <param name="value">原始字符串。</param>
        /// <returns>可安全拼接到批处理命令中的字符串。</returns>
        private static string EscapeBatchValue(string value)
        {
            return value?.Replace("%", "%%").Replace("\"", "\\\"") ?? "";
        }

        /// <summary>
        /// 记录需要更新的卸载注册表项信息。
        /// </summary>
        private class UninstallRegistryEntry
        {
            public string RootName { get; set; }
            public string SubKeyPath { get; set; }
            public string RegViewArgument { get; set; }
            public string DisplayName { get; set; }
            public string DisplayVersion { get; set; }
        }
    }

    // 数据模型
    public class AppVersionResponse
    {
        public AppManageInfoDto AppInfo { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }
        public string PackageType { get; set; }
        public int PlatformType { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsStablePublish { get; set; }
        public bool IsSilently { get; set; }
        public long FileSize { get; set; }
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public string MD5 { get; set; }
        public string BackupUrl { get; set; }
    }

    public class AppManageInfoDto
    {
        public Guid? ApplicationId { get; set; }

        public string AppName { get; set; }

        /// <summary>
        /// OSS空间文件夹路径名称(预留)
        /// </summary>
        public string FolderName { get; set; }

        public string IconUrl { get; set; }

        public string Introduction { get; set; }

        public string Description { get; set; }

        public string ScreenShot { get; set; }
    }

    public class LoadingManageParams
    {
        /// <summary>
        /// 下载进度窗口标题
        /// </summary>
        public string DownloadCaption { get; set; } = "正在更新";

        /// <summary>
        /// 下载取消按钮文本
        /// </summary>
        public string DownloadCancelText { get; set; } = "取消";

        /// <summary>
        /// 下载中提示文本
        /// </summary>
        public string DownloadMessage { get; set; } = "正在下载更新包，请稍候...";
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public AppVersionResponse UpdateInfo { get; set; }
        public Version RemoteVersion { get; set; }
        public Version LocalVersion { get; set; }
        public string Error { get; set; }
    }

}
