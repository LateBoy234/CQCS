using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl.Device
{
    public  class SpreadtrumDriverDetector
    {
        /// <summary>
        /// 检测展讯驱动是否已安装（不依赖设备连接）
        /// </summary>
        public static DriverInstallInfo CheckInstalledDriver()
        {
            var info = new DriverInstallInfo();

            // 1. 检查注册表中的驱动服务
            info.Services = CheckRegistryServices();

            // 2. 检查已安装的驱动包（INF文件）
            info.InfFiles = CheckInfFiles();

            // 3. 检查驱动文件目录
            info.DriverFiles = CheckDriverDirectories();

            // 4. 检查已签名驱动列表
            info.SignedDrivers = CheckSignedDrivers();

            // 5. 检查系统驱动存储
            info.DriverStore = CheckDriverStore();

            info.IsInstalled = info.Services.Any() || info.InfFiles.Any() ||
                              info.DriverFiles.Any() || info.SignedDrivers.Any();

            return info;
        }

        /// <summary>
        /// 检查注册表中的展讯驱动服务
        /// </summary>
        private static List<ServiceInfo> CheckRegistryServices()
        {
            var services = new List<ServiceInfo>();
            string[] spreadtrumPatterns = { "SPRD", "UNISOC", "SPREADTRUM", "UISOC" };

            try
            {
                using (var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                {
                    if (servicesKey == null) return services;

                    foreach (string serviceName in servicesKey.GetSubKeyNames())
                    {
                        bool isMatch = spreadtrumPatterns.Any(pattern =>
                            serviceName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (!isMatch) continue;

                        using (var serviceKey = servicesKey.OpenSubKey(serviceName))
                        {
                            if (serviceKey == null) continue;

                            string imagePath = serviceKey.GetValue("ImagePath")?.ToString();
                            bool fileExists = false;
                            string fullPath = null;

                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                fullPath = Environment.ExpandEnvironmentVariables(imagePath);
                                fileExists = File.Exists(fullPath);
                            }

                            // ========== 关键改进：只保留文件实际存在的 ==========
                            if (!fileExists)
                            {
                                // 可选：记录残留项
                                Console.WriteLine($"发现残留注册表项: {serviceName} (文件缺失: {fullPath})");
                                continue; // 跳过残留的
                            }

                            services.Add(new ServiceInfo
                            {
                                Name = serviceName,
                                DisplayName = serviceKey.GetValue("DisplayName")?.ToString(),
                                ImagePath = imagePath,
                                FullPath = fullPath,
                                FileExists = true, // 确认存在
                                StartType = serviceKey.GetValue("Start")?.ToString(),
                                FileVersion = FileVersionInfo.GetVersionInfo(fullPath).FileVersion
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { }

            return services;
        }

        /// <summary>
        /// 检查Windows驱动目录中的展讯INF文件
        /// </summary>
        private static List<InfFileInfo> CheckInfFiles()
        {
            var infFiles = new List<InfFileInfo>();
            string[] searchPaths = {
            @"C:\Windows\INF",
            @"C:\Windows\System32\DriverStore\FileRepository"
        };

            string[] patterns = { "*sprd*.inf", "*unisoc*.inf", "*spreadtrum*.inf" };

            try
            {
                foreach (var basePath in searchPaths)
                {
                    if (!Directory.Exists(basePath)) continue;

                    foreach (var pattern in patterns)
                    {
                        var files = Directory.GetFiles(basePath, pattern, SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            var info = new InfFileInfo
                            {
                                FilePath = file,
                                FileName = Path.GetFileName(file),
                                CreationTime = File.GetCreationTime(file),
                                LastWriteTime = File.GetLastWriteTime(file)
                            };

                            // 尝试读取INF文件中的版本信息
                            try
                            {
                                var lines = File.ReadLines(file).Take(50); // 只读前50行
                                foreach (var line in lines)
                                {
                                    if (line.StartsWith("DriverVer", StringComparison.OrdinalIgnoreCase))
                                    {
                                        info.DriverVersion = line.Split('=').LastOrDefault()?.Trim();
                                    }
                                    if (line.StartsWith("Provider", StringComparison.OrdinalIgnoreCase))
                                    {
                                        info.Provider = line.Split('=').LastOrDefault()?.Trim()?.Trim('"');
                                    }
                                }
                            }
                            catch { }

                            infFiles.Add(info);
                        }
                    }
                }
            }
            catch (Exception ex) { /* 权限不足时忽略 */ }

            return infFiles;
        }

        /// <summary>
        /// 检查系统驱动文件目录
        /// </summary>
        private static List<DriverFileInfo> CheckDriverDirectories()
        {
            var files = new List<DriverFileInfo>();
            string[] searchPaths = {
            @"C:\Windows\System32\drivers",
            @"C:\Windows\SysWOW64\drivers"
        };

            string[] patterns = { "*sprd*", "*unisoc*", "*spreadtrum*" };

            try
            {
                foreach (var path in searchPaths)
                {
                    if (!Directory.Exists(path)) continue;

                    var directory = new DirectoryInfo(path);
                    foreach (var pattern in patterns)
                    {
                        var matchedFiles = directory.GetFiles(pattern + ".sys")
                            .Concat(directory.GetFiles(pattern + ".dll"))
                            .Concat(directory.GetFiles(pattern + ".cat"));

                        foreach (var file in matchedFiles)
                        {
                            files.Add(new DriverFileInfo
                            {
                                FileName = file.Name,
                                FullPath = file.FullName,
                                Size = file.Length,
                                CreationTime = file.CreationTime,
                                Version = FileVersionInfo.GetVersionInfo(file.FullName).FileVersion
                            });
                        }
                    }
                }
            }
            catch { }

            return files;
        }

        /// <summary>
        /// 检查已签名的驱动程序列表（WMI）
        /// </summary>
        private static List<SignedDriverInfo> CheckSignedDrivers()
        {
            var drivers = new List<SignedDriverInfo>();

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPSignedDriver"))
                {
                    foreach (ManagementObject driver in searcher.Get())
                    {
                        string deviceName = driver["DeviceName"]?.ToString() ?? "";
                        string manufacturer = driver["Manufacturer"]?.ToString() ?? "";
                        string driverVersion = driver["DriverVersion"]?.ToString() ?? "";
                        string driverProviderName = driver["DriverProviderName"]?.ToString() ?? "";
                        string infName = driver["InfName"]?.ToString() ?? "";

                        // 检查是否包含展讯关键词
                        bool isSpreadtrum =
                            deviceName.IndexOf("SPRD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            deviceName.IndexOf("Unisoc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            deviceName.IndexOf("Spreadtrum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            manufacturer.IndexOf("Spreadtrum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            manufacturer.IndexOf("Unisoc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            driverProviderName.IndexOf("Spreadtrum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            driverProviderName.IndexOf("Unisoc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            infName.IndexOf("sprd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            infName.IndexOf("unisoc", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isSpreadtrum)
                        {
                            drivers.Add(new SignedDriverInfo
                            {
                                DeviceName = deviceName,
                                Manufacturer = manufacturer,
                                DriverVersion = driverVersion,
                                DriverProvider = driverProviderName,
                                InfName = infName,
                                DriverDate = driver["DriverDate"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch { }

            return drivers;
        }

        /// <summary>
        /// 检查驱动存储（DriverStore）
        /// </summary>
        private static List<DriverStoreInfo> CheckDriverStore()
        {
            var stores = new List<DriverStoreInfo>();
            string driverStorePath = @"C:\Windows\System32\DriverStore\FileRepository";

            if (!Directory.Exists(driverStorePath)) return stores;

            try
            {
                var directories = Directory.GetDirectories(driverStorePath);
                foreach (var dir in directories)
                {
                    string dirName = Path.GetFileName(dir).ToLower();

                    if (dirName.Contains("sprd") || dirName.Contains("unisoc") || dirName.Contains("spreadtrum"))
                    {
                        var info = new DriverStoreInfo
                        {
                            DirectoryName = Path.GetFileName(dir),
                            FullPath = dir,
                            CreationTime = Directory.GetCreationTime(dir)
                        };

                        // 查找INF文件
                        var infFiles = Directory.GetFiles(dir, "*.inf");
                        if (infFiles.Any())
                        {
                            info.InfFileName = Path.GetFileName(infFiles.First());

                            // 读取版本信息
                            try
                            {
                                var content = File.ReadAllText(infFiles.First());
                                var match = System.Text.RegularExpressions.Regex.Match(content,
                                    @"DriverVer\s*=\s*([^\r\n]+)",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (match.Success) info.DriverVersion = match.Groups[1].Value.Trim();
                            }
                            catch { }
                        }

                        stores.Add(info);
                    }
                }
            }
            catch { }

            return stores;
        }
    }

    // 数据类定义
    public class DriverInstallInfo
    {
        public bool IsInstalled { get; set; }
        public List<ServiceInfo> Services { get; set; } = new List<ServiceInfo>();
        public List<InfFileInfo> InfFiles { get; set; } = new List<InfFileInfo> ();
        public List<DriverFileInfo> DriverFiles { get; set; } = new List<DriverFileInfo> ();
        public List<SignedDriverInfo> SignedDrivers { get; set; } = new List<SignedDriverInfo>();
        public List<DriverStoreInfo> DriverStore { get; set; } = new List<DriverStoreInfo>();

        public string GetSummary()
        {
            if (!IsInstalled) return "未检测到展讯驱动安装";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("✓ 检测到展讯驱动已安装");
            sb.AppendLine();

            if (Services.Any())
            {
                sb.AppendLine($"[注册表服务] {Services.Count} 个:");
                foreach (var s in Services) sb.AppendLine($"  - {s.Name} ({(s.FileExists ? "文件存在" : "文件缺失")})");
                sb.AppendLine();
            }

            if (InfFiles.Any())
            {
                sb.AppendLine($"[INF文件] {InfFiles.Count} 个:");
                foreach (var i in InfFiles) sb.AppendLine($"  - {i.FileName} {(i.DriverVersion != null ? $"[{i.DriverVersion}]" : "")}");
                sb.AppendLine();
            }

            if (DriverFiles.Any())
            {
                sb.AppendLine($"[驱动文件] {DriverFiles.Count} 个:");
                foreach (var d in DriverFiles) sb.AppendLine($"  - {d.FileName} {(d.Version != null ? $"v{d.Version}" : "")}");
                sb.AppendLine();
            }

            if (SignedDrivers.Any())
            {
                sb.AppendLine($"[已签名驱动] {SignedDrivers.Count} 个:");
                foreach (var s in SignedDrivers) sb.AppendLine($"  - {s.DeviceName} v{s.DriverVersion}");
                sb.AppendLine();
            }

            if (DriverStore.Any())
            {
                sb.AppendLine($"[驱动存储] {DriverStore.Count} 个:");
                foreach (var d in DriverStore) sb.AppendLine($"  - {d.DirectoryName}");
            }

            return sb.ToString();
        }
    }

    public class ServiceInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ImagePath { get; set; }
        public string StartType { get; set; }
        public string Group { get; set; }
        public string FileVersion { get; set; }
        public string FullPath { get; set; }
        public bool FileExists { get; set; }
    }

    public class InfFileInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string DriverVersion { get; set; }
        public string Provider { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
    }

    public class DriverFileInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public long Size { get; set; }
        public string Version { get; set; }
        public DateTime CreationTime { get; set; }
    }

    public class SignedDriverInfo
    {
        public string DeviceName { get; set; }
        public string Manufacturer { get; set; }
        public string DriverVersion { get; set; }
        public string DriverProvider { get; set; }
        public string InfName { get; set; }
        public string DriverDate { get; set; }
    }

    public class DriverStoreInfo
    {
        public string DirectoryName { get; set; }
        public string FullPath { get; set; }
        public string InfFileName { get; set; }
        public string DriverVersion { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
