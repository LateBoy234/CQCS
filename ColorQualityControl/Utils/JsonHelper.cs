using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ThreeNH.ColorQualityControl.Utils
{
 public   class JsonHelper
    {
        private const string INDENT_STRING = "    ";
        private static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();

                            indent += 1;
                            for (int k = 0; k < indent; k++)
                                sb.Append(INDENT_STRING);
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            indent -= 1;
                            for (int k = 0; k < indent; k++)
                                sb.Append(INDENT_STRING);
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            for (int k = 0; k < indent; k++)
                                sb.Append(INDENT_STRING);
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        public static void Save<T>(T obj,string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                if (!File.Exists(path))
                {
                    //注意这里不能直接用FileCreate创建文件，如果这样第一次创建文件后，再写入内容时会出现当前文件正在使用，资源没有释放
                    FileStream fs = File.Create(path);
                    fs.Close();
                }
                var serilizedObj = JsonConvert.SerializeObject(obj);
              
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(FormatJson(serilizedObj));
                    sw.Close();
                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
           

        }
        public static T Load<T>(string loadPath) 
        {
            //如果加载时不存在该路径，按照默认路径进行创建
            var directory = Path.GetDirectoryName(loadPath);
            if (!Directory.Exists(directory))
            {
                return default(T);
            }
            if (!File.Exists(loadPath))
            {
                return default(T);
            }
            using (StreamReader streamReader = new StreamReader(loadPath)) 
            {
                var sr= streamReader.ReadToEnd();
                streamReader.Close();
               return JsonConvert.DeserializeObject<T>(sr);
            }
        }
    }
}
