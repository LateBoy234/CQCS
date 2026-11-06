using ColorQualityControl.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    public static class AppSettings
    {
        /// <summary>
        /// 语言
        /// </summary>
        public static string Culture
        {
            get => AppSettingsHelper.Get("Culture");
            set => AppSettingsHelper.Set("Culture", value);
        }

        public static string LibraryPath
        {
            get
            {
                var libraryPath = AppSettingsHelper.Get("LibraryPath");
                FileInfo fileInfo = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
                return string.IsNullOrEmpty(libraryPath) ? Path.Combine(fileInfo.DirectoryName, "LibraryPath") : libraryPath;
            }
            set => AppSettingsHelper.Set("LibraryPath", value);
        }

        public static string LastUsedLibrary
        {
            get => AppSettingsHelper.Get("LastUsedLibrary");
            set => AppSettingsHelper.Set("LastUsedLibrary", value);
        }

        public static bool LibraryAutoClear
        {
            get
            {
                var autoClear = AppSettingsHelper.Get("LibraryAutoClear");
                return string.IsNullOrEmpty(autoClear) ? false : Convert.ToBoolean(autoClear);
            }
            set => AppSettingsHelper.Set("LibraryAutoClear", value.ToString());
        }
        public static string Connection_PortName
        {
            get => AppSettingsHelper.Get("Connection_PortName");
            set => AppSettingsHelper.Set("Connection_PortName", value);
        }
        public static int Connection_BaudRate
        {
            get
            {
                var baudRate = AppSettingsHelper.Get("Connection_BaudRate");
                if (int.TryParse(baudRate, out int connect_BaudRate))
                    return connect_BaudRate;
                return 9600;
            }
            set => AppSettingsHelper.Set("Connection_BaudRate", value.ToString());
        }
        public static int Connection_StopBits
        {
            get
            {
                var stopBit = AppSettingsHelper.Get("Connection_StopBits");
                if (int.TryParse(stopBit, out int connection_StopBit))
                    return connection_StopBit;
                return (int)StopBits.One;
            }
            set => AppSettingsHelper.Set("Connection_StopBits", value.ToString());
        }
        public static int Connection_Parity
        {
            get
            {
                var parity = AppSettingsHelper.Get("Connection_Parity");
                if (int.TryParse(parity, out int connection_Parity))
                    return connection_Parity;
                return (int)Parity.None;
            }
            set => AppSettingsHelper.Set("Connection_Parity", value.ToString());
        }

        /// <summary>Agent 助手开关</summary>
        public static bool Agent_Enabled
        {
            get
            {
                var enabled = AppSettingsHelper.Get("Agent_Enabled");
                return string.IsNullOrEmpty(enabled) ? false : Convert.ToBoolean(enabled);
            }
            set => AppSettingsHelper.Set("Agent_Enabled", value.ToString());
        }

        /// <summary>LLM 兼容端点，例如 https://your-llm/v1/chat/completions</summary>
        public static string Agent_LlmEndpoint
        {
            get => AppSettingsHelper.Get("Agent_LlmEndpoint");
            set => AppSettingsHelper.Set("Agent_LlmEndpoint", value);
        }

        /// <summary>LLM API Key（运行时由配置提供，不进代码库）</summary>
        public static string Agent_LlmApiKey
        {
            get => AppSettingsHelper.Get("Agent_LlmApiKey");
            set => AppSettingsHelper.Set("Agent_LlmApiKey", value);
        }

        /// <summary>LLM 模型名，例如 gpt-4o-mini</summary>
        public static string Agent_LlmModel
        {
            get => AppSettingsHelper.Get("Agent_LlmModel");
            set => AppSettingsHelper.Set("Agent_LlmModel", value);
        }
    }
}
