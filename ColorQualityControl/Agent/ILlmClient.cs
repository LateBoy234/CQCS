using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl.Agent
{
    /// <summary>LLM 请求的一个工具调用。</summary>
    public sealed class ToolCall
    {
        public string Id { get; }
        public string Name { get; }
        public string Arguments { get; }

        public ToolCall(string id, string name, string arguments)
        {
            Id = id;
            Name = name;
            Arguments = arguments;
        }
    }

    /// <summary>发给 LLM 的工具描述（OpenAI function-calling 形态）。</summary>
    public sealed class LlmTool
    {
        public string Name { get; }
        public string Description { get; }
        public JObject Parameters { get; }

        public LlmTool(string name, string description, JObject parameters)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }
    }

    public sealed class LlmResponse
    {
        public string Content { get; set; } = string.Empty;
        public List<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();
    }

    /// <summary>对话中的一条消息。工具结果使用 role="tool" 并附带 ToolCallId。</summary>
    public sealed class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }
        public string ToolCallId { get; }
        public IReadOnlyList<ToolCall> ToolCalls { get; }

        public ChatMessage(string role, string content, string toolCallId = null, IReadOnlyList<ToolCall> toolCalls = null)
        {
            Role = role;
            Content = content;
            ToolCallId = toolCallId;
            ToolCalls = toolCalls ?? new List<ToolCall>();
        }
    }

    public interface ILlmClient
    {
        Task<LlmResponse> CompleteAsync(IEnumerable<ChatMessage> messages, IEnumerable<LlmTool> tools, CancellationToken ct = default);
    }
}
