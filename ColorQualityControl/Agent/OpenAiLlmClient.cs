using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.ColorQualityControl;

namespace ThreeNH.ColorQualityControl.Agent
{
    /// <summary>
    /// OpenAI 兼容的对话客户端（支持 tools / function calling）。
    /// 目标框架 net462：使用已引用的 System.Net.Http 与 Newtonsoft.Json。
    /// </summary>
    public sealed class OpenAiLlmClient : ILlmClient
    {
        // net462 下 HttpClient 必须复用，避免 socket 耗尽。
        private static readonly HttpClient SharedClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        private readonly HttpClient _http;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _model;

        public OpenAiLlmClient(string endpoint, string apiKey, string model, HttpClient httpClient = null)
        {
            _endpoint = endpoint;
            _apiKey = apiKey;
            _model = model;
            _http = httpClient ?? SharedClient;
        }

        /// <summary>从 AppSettings 读取配置；未配置 endpoint 时返回 null。</summary>
        public static OpenAiLlmClient CreateFromAppSettings()
        {
            var endpoint = AppSettings.Agent_LlmEndpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }
            return new OpenAiLlmClient(endpoint, AppSettings.Agent_LlmApiKey, AppSettings.Agent_LlmModel);
        }

        public async Task<LlmResponse> CompleteAsync(IEnumerable<ChatMessage> messages, IEnumerable<LlmTool> tools, CancellationToken ct = default)
        {
            var requestBody = new JObject
            {
                ["model"] = _model,
                ["messages"] = BuildMessages(messages),
                ["tools"] = BuildTools(tools),
                ["tool_choice"] = "auto",
                ["temperature"] = 0.2
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }
            request.Content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");

            using (var response = await _http.SendAsync(request, ct).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(BuildFriendlyError(response.StatusCode, body));
                }
                return ParseResponse(body);
            }
        }

        /// <summary>把 LLM 服务端的 HTTP 错误翻译成中文友好提示，并附上服务方原始信息。</summary>
        private static string BuildFriendlyError(HttpStatusCode status, string body)
        {
            int code = (int)status;
            string hint;
            switch (code)
            {
                case 400: hint = "请求格式有误，请检查端点与参数。"; break;
                case 401: hint = "API Key 无效或已过期，请检查 Agent_LlmApiKey。"; break;
                case 402: hint = "账户余额不足，请充值或更换 API Key。"; break;
                case 403: hint = "无权访问该模型或端点，请检查账户权限。"; break;
                case 404: hint = "端点地址不存在，请检查 Agent_LlmEndpoint。"; break;
                case 408: hint = "请求超时，请稍后重试。"; break;
                case 429: hint = "请求过于频繁，请稍后再试。"; break;
                default: hint = code >= 500 ? "服务端暂时不可用，请稍后重试。" : "请求未成功，请稍后重试。"; break;
            }

            var message = "LLM 请求失败（" + code + "，" + GetStatusName(status) + "）。" + hint;
            var detail = ExtractProviderMessage(body);
            if (!string.IsNullOrWhiteSpace(detail))
            {
                message += "\n服务方信息：" + detail;
            }
            return message;
        }

        private static string GetStatusName(HttpStatusCode status)
        {
            switch ((int)status)
            {
                case 400: return "BadRequest";
                case 401: return "Unauthorized";
                case 402: return "PaymentRequired";
                case 403: return "Forbidden";
                case 404: return "NotFound";
                case 408: return "RequestTimeout";
                case 429: return "TooManyRequests";
                case 500: return "InternalServerError";
                case 502: return "BadGateway";
                case 503: return "ServiceUnavailable";
                case 504: return "GatewayTimeout";
                default: return "Error";
            }
        }

        private static string ExtractProviderMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                var json = JObject.Parse(body);
                var msg = json["error"]?["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(msg)) return msg;
                return json["message"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static JArray BuildMessages(IEnumerable<ChatMessage> messages)
        {
            var array = new JArray();
            foreach (var m in messages)
            {
                if (m.Role == "tool")
                {
                    array.Add(new JObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = m.ToolCallId,
                        ["content"] = m.Content
                    });
                }
                else if (m.Role == "assistant" && m.ToolCalls.Count > 0)
                {
                    var toolCalls = new JArray();
                    foreach (var tc in m.ToolCalls)
                    {
                        toolCalls.Add(new JObject
                        {
                            ["id"] = tc.Id,
                            ["type"] = "function",
                            ["function"] = new JObject
                            {
                                ["name"] = tc.Name,
                                ["arguments"] = tc.Arguments
                            }
                        });
                    }
                    array.Add(new JObject
                    {
                        ["role"] = "assistant",
                        ["content"] = m.Content,
                        ["tool_calls"] = toolCalls
                    });
                }
                else
                {
                    array.Add(new JObject
                    {
                        ["role"] = m.Role,
                        ["content"] = m.Content
                    });
                }
            }
            return array;
        }

        private static JArray BuildTools(IEnumerable<LlmTool> tools)
        {
            var array = new JArray();
            foreach (var t in tools)
            {
                array.Add(new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = t.Name,
                        ["description"] = t.Description,
                        ["parameters"] = t.Parameters
                    }
                });
            }
            return array;
        }

        private static LlmResponse ParseResponse(string body)
        {
            var root = JObject.Parse(body);
            var message = root["choices"]?[0]?["message"] as JObject;
            if (message == null)
            {
                throw new InvalidOperationException("LLM 返回格式异常: " + body);
            }

            var response = new LlmResponse
            {
                Content = message["content"]?.ToString() ?? string.Empty
            };

            if (message["tool_calls"] is JArray toolCalls)
            {
                foreach (var tc in toolCalls)
                {
                    var function = tc["function"] as JObject;
                    response.ToolCalls.Add(new ToolCall(
                        tc["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                        function?["name"]?.ToString() ?? string.Empty,
                        function?["arguments"]?.ToString() ?? "{}"));
                }
            }
            return response;
        }
    }
}
