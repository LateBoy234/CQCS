using ColorQualityControl.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using ThreeNH.ColorQualityControl.Device;
using ThreeNH.CommunicationBase;
using ThreeNH.Helper;
using ThreeNH.ProjectLog;

namespace ThreeNH.ColorQualityControl
{

    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private const int LogMaxLength = 1024 * 1024;   // 日志最大长度设为1M

        static App()
        {
#if !DEBUG
            InitLog();
#endif
            try
            {
                InitLog();
                // 设置DLL的查找目录
                var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                path = System.IO.Path.Combine(path, Environment.Is64BitOperatingSystem ? "x64" : "x86");
                SetDllDirectory(path);

                MultiplatformDllLoader.Enable = true;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"设置Dll查找目录出错:{ex.Message}");
            }



            Type clsType = typeof(App);
            Assembly assy = clsType.Assembly;

            var titleAttribute =
                (AssemblyTitleAttribute)assy.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0];
            Title = titleAttribute.Title;
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var copyrightAttribute =
                (AssemblyCopyrightAttribute)assy.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0];
            Copyright = copyrightAttribute.Copyright;
            var compnayAttribute =
                (AssemblyCompanyAttribute)assy.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0];
            Company = compnayAttribute.Company;
        }

        /// <summary>
        /// 初始化日志
        /// </summary>
        private static void InitLog()
        {

            string logfile = GetAppPath() + "\\cqc.log";
            FileInfo info = new FileInfo(logfile);
            if (info.Exists && info.Length > LogMaxLength)
            {
                try
                {
                    File.Delete(logfile);
                }
                catch (Exception)
                {
                    // do nothing
                }
            }

            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ProjectTraceListener(logfile));
        }

        public App()
        {

        }

        private void InitializeCulture(string[] args)
        {
            string culture = null;

            // 优先级1: 命令行参数（安装或重启传递）
            var langArg = args.FirstOrDefault(a => a.StartsWith("--lang=") || a.StartsWith("--culture="));
            Trace.TraceInformation(langArg);
            if (langArg != null)
            {
                var prefix = "--lang=";
                culture = langArg.Replace(prefix, "");
                Trace.TraceInformation("替换后参数：" + culture);
                culture = MapInnoSetupLanguage(culture);
                Trace.TraceInformation("转换后参数：" + culture);
                ApplyCulture(culture);
                return;
            }

            culture = AppSettings.Culture;

            //  系统默认
            if (string.IsNullOrEmpty(culture))
            {
                culture = CultureInfo.CurrentCulture.Name;
            }

            ApplyCulture(culture);
        }




        private string MapInnoSetupLanguage(string innoLang)
        {
            if (string.IsNullOrEmpty(innoLang))
                return "en-US";

            string str = innoLang.ToLower().Trim();

            switch (str)
            {
                case "en":
                case "en-us":
                    return "en-US";
                case "chs":
                case "zh-cn":
                    return "zh-CN";
                case "cht":
                case "zh-tw":
                    return "zh-TW";
                case "fr":
                    return "fr";
                case "de":
                    return "de";
                case "pt":
                    return "pt";
                case "ru":
                    return "ru";
                case "es":
                    return "es";
                case "it":
                    return "it";
                case "ja":
                    return "ja";
                default:
                    return "en-US";
            }
        }

        private void ApplyCulture(string cultureName)
        {
            try
            {
                Trace.TraceInformation("语言切换了");
                var ci = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;

                // 切换 WPF 资源
                LocalUtil.SwitchCulture(ci);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"应用语言失败: {ex.Message}");
            }
        }

        public static string LibraryPath
        {
            get => AppSettings.LibraryPath;
            set => AppSettings.LibraryPath = value;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            if (Title == "SQCX")
            {
                string splashScreenPngPath = "Images/SplashScreen.png";
                SplashScreen s = new SplashScreen(splashScreenPngPath);
                s.Show(true);
            }
            else if (Title == "HDQX")//20191015lilin add :
            {
                //string splashScreenPngPath = "Images/SplashScreen_OnShadeQC.png";
                string splashScreenPngPath = "Images/SplashScreen_HDQX.png";
                SplashScreen s = new SplashScreen(splashScreenPngPath);
                s.Show(true);
            }
            else if (Title == "OnShade QC")
            {
                string splashScreenPngPath = "Images/SplashScreen_OnShadeQC.png";
                SplashScreen s = new SplashScreen(splashScreenPngPath);
                s.Show(true);
            }
            else if (Title == "NASEN")
            {
                string splashScreenPngPath = "Images/SplashScreen_NASEN.png";
                SplashScreen s = new SplashScreen(splashScreenPngPath);
                s.Show(true);
            }
            else if (Title == "TEXFORMA")
            {

            }
            else
            {
                string splashScreenPngPath = "Images/SplashScreen_PCE.jpg";
                SplashScreen s = new SplashScreen(splashScreenPngPath);
                s.Show(true);
            }
#endif
            // 1. 捕获主线程未处理异常（原逻辑升级）
            DispatcherUnhandledException += DealUnhandledException;

            // 2. 捕获非主线程未处理异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 3. 捕获 Task 异步异常（可选）
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            // 初始化语言（在创建主窗口之前）
            InitializeCulture(e.Args);





            //只允许运行一个实例
            MutexChecker.CheckApplicationMutex();
            base.OnStartup(e);
            try
            {
                Trace.TraceInformation("正在加载SystemDefaultConfig");
                SystemDefaultConfig = SystemDefaultConfig.Load(SystemDefaultConfigPath);
                Trace.TraceInformation("加载SystemDefaultConfig完成");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("加载出现异常，设为默认置：{0}\r\n{1}", ex.Message, ex.StackTrace);
                SystemDefaultConfig = new SystemDefaultConfig();
            }

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            PortService.SerialPortService.Start();
        }


        #region 捕获WPF加载xmal文档异常代码
        /// <summary>
        /// 全局异常捕获（包含启动阶段、主线程、非主线程）
        /// </summary>

        // 日志文件路径（放到程序目录，方便客户反馈）
        private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");


        /// <summary>
        /// 主线程（UI）未处理异常（原方法升级）
        /// </summary>
        void DealUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 标记异常已处理，避免程序立即崩溃
            e.Handled = true;

            // 构建完整异常信息（包含内部异常、堆栈、目标方法）
            string exceptionDetail = GetFullExceptionInfo(e.Exception);

            // 1. 写入日志文件（核心：方便客户把日志发给你）
            WriteExceptionLog(exceptionDetail);

            // 2. Trace 输出（开发时调试）
            Trace.TraceError("主线程未处理异常：{0}", exceptionDetail);

            // 3. 区分自定义异常
            if (e.Exception is CommunicationException ex)
            {
                MessageBox.Show(S._(ex.ErrorCode.ToString()), "通信异常", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. 多语言提示兜底
            string format = null;
            try
            {
                format = S._("Format_UnhandledException_0_1");
            }
            catch
            {
                format = "发生未处理异常，程序即将退出。\n异常信息：{0}\n详细堆栈：{1}";
            }
            if (string.IsNullOrEmpty(format))
            {
                format = "发生未处理异常，程序即将退出。\n异常信息：{0}\n详细堆栈：{1}";
            }

            // 5. 弹窗提示（简化信息，完整信息写日志）
            string msg = string.Format(format, e.Exception.Message, e.Exception.StackTrace);
            MessageBox.Show(msg, "程序异常", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            // 6. 安全退出
            Current.Shutdown(-1);
        }

        /// <summary>
        /// 非主线程未处理异常
        /// </summary>
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                string exceptionDetail = GetFullExceptionInfo(ex);
                WriteExceptionLog("非主线程未处理异常：" + exceptionDetail);
                Trace.TraceError("非主线程未处理异常：{0}", exceptionDetail);

                MessageBox.Show($"后台线程发生异常：{ex.Message}\n请查看程序目录下的 error_log.txt 获取详细信息",
                    "后台异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // 非托管异常无法处理，直接退出
            Environment.Exit(-1);
        }

        /// <summary>
        /// Task 异步异常
        /// </summary>
        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // 标记异常已处理，避免程序崩溃
            string exceptionDetail = GetFullExceptionInfo(e.Exception);
            WriteExceptionLog("异步任务未处理异常：" + exceptionDetail);
            Trace.TraceError("异步任务未处理异常：{0}", exceptionDetail);
        }

        /// <summary>
        /// 获取完整的异常信息（包含内部异常、目标方法、数据）
        /// </summary>
        private string GetFullExceptionInfo(Exception ex)
        {
            if (ex == null) return "无异常信息";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===== 异常详情 =====");
            sb.AppendLine($"异常类型：{ex.GetType().FullName}");
            sb.AppendLine($"异常消息：{ex.Message}");
            sb.AppendLine($"目标方法：{ex.TargetSite?.Name ?? "未知"}（{ex.TargetSite?.DeclaringType?.FullName ?? "未知类型"}）");
            sb.AppendLine($"堆栈跟踪：{ex.StackTrace}");

            // 递归输出内部异常（关键：XAML 解析异常的根因在 InnerException）
            Exception innerEx = ex.InnerException;
            int innerCount = 1;
            while (innerEx != null)
            {
                sb.AppendLine($"\n===== 内部异常 {innerCount} =====");
                sb.AppendLine($"异常类型：{innerEx.GetType().FullName}");
                sb.AppendLine($"异常消息：{innerEx.Message}");
                sb.AppendLine($"堆栈跟踪：{innerEx.StackTrace}");
                innerEx = innerEx.InnerException;
                innerCount++;
            }

            sb.AppendLine("\n===== 结束 =====");
            return sb.ToString();
        }

        /// <summary>
        /// 写入异常日志到文件
        /// </summary>
        private void WriteExceptionLog(string content)
        {
            try
            {
                // 追加写入，避免覆盖旧日志
                File.AppendAllText(_logPath,
                    $"\n【{DateTime.Now:yyyy-MM-dd HH:mm:ss}】{content}",
                    Encoding.UTF8);
            }
            catch
            {
                // 日志写入失败不影响程序，静默忽略
            }
        }

        #endregion

        protected override void OnExit(ExitEventArgs e)
        {
            Trace.TraceInformation("保存用户配置");

            SystemDefaultConfig?.Save(SystemDefaultConfigPath);

            base.OnExit(e);
            PortService.SerialPortService.CleanUp();



            Trace.TraceInformation("程序正常退出");
        }

        public static string Text(string key)
        {
            return Current.TryFindResource(key) as string;
        }


        public static string Title { get; private set; }
        public static Version AppVersion { get; private set; }
        public static string Copyright { get; private set; }
        public static string Company { get; private set; }

        public static void Restart()
        {
            Trace.Listeners.Clear();    // 关掉日志文件，以免发生访问冲突

            Process p = new Process();
            p.StartInfo.FileName =
                Process.GetCurrentProcess().MainModule.FileName;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            Current.Shutdown();
        }

        internal SystemDefaultConfig SystemDefaultConfig
        {
            get; private set;
        }

        public static string GetAppPath()
        {
            FileInfo fileInfo = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
            return fileInfo.DirectoryName;
        }

        public static string SystemDefaultConfigPath
        {
            get
            {
                return GetAppPath() + "\\SystemDefaultConfig.xml";
            }
        }

        public static string GetUserManualPath()
        {
            //string path = GetAppPath() + "\\" + CultureInfo.CurrentCulture.Name + "\\UserManual.pdf";
            //if (!File.Exists(path))
            //{
            //    path = GetAppPath() + "\\en-US\\UserManual.pdf";
            //}//20191217 lilin change 
            string path = GetAppPath() + "\\Resources" + "\\UserManual." +
                CultureInfo.CurrentCulture.Name + ".pdf";
            if (!File.Exists(path))
            {
                path = GetAppPath() + "\\Resources" + "\\UserManual." + "en-US" + ".pdf";
            }
            return path;
        }

        public static string BackupPath
        {
            get
            {
                var path = Path.Combine(GetAppPath(), "backup");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static UIElement GetAboutInfoElement()
        {
            string path = GetAppPath() + "\\" + CultureInfo.CurrentCulture.Name + "\\about.xml";
            if (!File.Exists(path))
            {
                path = GetAppPath() + "\\en-US\\about.xml";
            }

            if (File.Exists(path))
            {
                XmlTextReader xmlreader = new XmlTextReader(path);
                object obj = XamlReader.Load(xmlreader);
                return obj as UIElement;
            }
            return null;
        }



        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);
    }
}
