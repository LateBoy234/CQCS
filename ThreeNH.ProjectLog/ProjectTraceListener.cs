using System;
using System.Diagnostics;
using System.IO;

namespace ThreeNH.ProjectLog
{
    public class ProjectTraceListener : TraceListener
    {
        public string FilePath { get; private set; }


        public ProjectTraceListener(string filePath)
        {
            FilePath = filePath;
        }

        public override void Write(string message)
        {
            File.AppendAllText(FilePath, message);
        }
        public override void WriteLine(string message)
        {
            File.AppendAllText(FilePath, 
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss    ") + message + Environment.NewLine);
        }
        public override void Write(object o, string category)
        {
            string message = string.Empty;
            if (!string.IsNullOrEmpty(category))
            {
                message = category + ":";
            }
            if (o is Exception)
            {
                var ex = (Exception)o;
                message += ex.Message + Environment.NewLine;
                message += ex.StackTrace;
            }
            else if (null != o)
            {
                message += o.ToString();
            }

            WriteLine(message);
        }
    }
}
