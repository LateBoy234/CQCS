using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThreeNH.ColorQualityControl.Agent.Mcp
{
    /// <summary>
    /// Local bridge used by the standalone MCP stdio process. Job access is
    /// read-only; the only write operation creates a user-confirmed export file.
    /// The pipe is deliberately not a general command channel: only methods in
    /// SqcxAgentQueryService are accepted.
    /// </summary>
    internal sealed class SqcxMcpBridgeHost : IDisposable
    {
        internal const string PipeName = "ThreeNH.SQCX.Agent.ReadOnly.v1";

        private readonly SqcxAgentQueryService _queries = new SqcxAgentQueryService();
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private Thread _worker;

        public void Start()
        {
            if (_worker != null)
            {
                return;
            }

            _worker = new Thread(RunMCPPipeServer)
            {
                IsBackground = true,
                Name = "SQCX MCP read-only bridge"
            };
            _worker.Start();
            Trace.TraceInformation("SQCX MCP bridge started on pipe {0}.", PipeName);
        }

        private void RunMCPPipeServer()
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None))
                    {
                        pipe.WaitForConnection();
                        HandleConnection(pipe);
                    }
                }
                catch (Exception ex)
                {
                    if (!_stop.IsCancellationRequested)
                    {
                        Trace.TraceError("SQCX MCP bridge error: {0}", ex);
                        Thread.Sleep(250);
                    }
                }
            }
        }

        private void HandleConnection(Stream stream)
        {
            using (var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
            {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                JObject response;
                try
                {
                    var request = JObject.Parse(line);
                    string method = request.Value<string>("method");
                    var arguments = request["arguments"] as JObject ?? new JObject();
                    response = new JObject
                    {
                        ["ok"] = true,
                        ["result"] = _queries.Execute(method, arguments)
                    };
                }
                catch (Exception ex)
                {
                    response = new JObject
                    {
                        ["ok"] = false,
                        ["error"] = ex.Message
                    };
                }

                writer.WriteLine(response.ToString(Formatting.None));
            }
        }

        public void Dispose()
        {
            if (_stop.IsCancellationRequested)
            {
                return;
            }

            _stop.Cancel();
            try
            {
                // Wake WaitForConnection so the background thread can observe cancellation.
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(100);
                }
            }
            catch
            {
            }

            if (_worker != null && _worker.IsAlive)
            {
                _worker.Join(500);
            }

            _stop.Dispose();
            Trace.TraceInformation("SQCX MCP bridge stopped.");
        }
    }
}
