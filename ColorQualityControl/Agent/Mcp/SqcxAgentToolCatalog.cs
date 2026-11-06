using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ThreeNH.ColorQualityControl.Agent.Mcp
{
    /// <summary>
    /// 工具清单的单一来源。独立 MCP 服务（ThreeNH.SQCX.McpServer）目前在其
    /// Program.cs 中镜像了这些定义；在把目录提升为共享程序集前，请保持两边同步。
    /// 工具名即桥方法名（get_application_context 等），in-app 助手直接调用
    /// SqcxAgentQueryService.Execute(method, args)，无需经过命名管道。
    /// </summary>
    public sealed class SqcxAgentToolDescriptor
    {
        public string Name { get; }
        public string Description { get; }
        public JObject InputSchema { get; }
        public bool ReadOnly { get; }

        public SqcxAgentToolDescriptor(string name, string description, JObject inputSchema, bool readOnly = true)
        {
            Name = name;
            Description = description;
            InputSchema = inputSchema;
            ReadOnly = readOnly;
        }
    }

    public static class SqcxAgentToolCatalog
    {
        public static IReadOnlyList<SqcxAgentToolDescriptor> Tools { get; } = new[]
        {
            new SqcxAgentToolDescriptor(
                "get_application_context",
                "Get the running SQCX job, measurement context, current selection, and instrument connection status. Read-only.",
                ObjectSchema()),

            new SqcxAgentToolDescriptor(
                "list_standards",
                "List standard specimens in the current SQCX job. Optionally filter by name or material. Read-only.",
                ObjectSchema(
                    ("query", StringProperty("Case-insensitive text matched against standard name and material.")),
                    ("limit", IntegerProperty("Maximum rows to return, from 1 to 200.", 1, 200)))),

            new SqcxAgentToolDescriptor(
                "list_tests",
                "List test specimens in the current SQCX job. Optionally restrict to a standard and filter by name or material. Read-only.",
                ObjectSchema(
                    ("standard_id", IntegerProperty("Optional parent standard ID.")),
                    ("query", StringProperty("Case-insensitive text matched against test name and material.")),
                    ("limit", IntegerProperty("Maximum rows to return, from 1 to 200.", 1, 200)))),

            new SqcxAgentToolDescriptor(
                "get_test_details",
                "Get authoritative Lab, color-difference and judgement values for one test under the current illuminant, observer and SCI/SCE context. Read-only.",
                ObjectSchema(
                    new[] { "test_id" },
                    ("test_id", IntegerProperty("Test specimen ID.")),
                    ("standard_id", IntegerProperty("Parent standard ID. Supply it when test IDs repeat between standards.")))),

            new SqcxAgentToolDescriptor(
                "export_data",
                "Request an export from the current SQCX job. SQCX shows a Save dialog and writes the file only after the user confirms the destination.",
                ObjectSchema(
                    new[] { "scope", "format" },
                    ("scope", StringEnumProperty(
                        "Data to export. standard and standard_with_tests require standard_id; test requires test_id; selected uses the current SQCX selection.",
                        "selected", "standard", "standard_with_tests", "test", "all")),
                    ("standard_id", IntegerProperty("Standard specimen ID.")),
                    ("test_id", IntegerProperty("Test specimen ID.")),
                    ("format", StringEnumProperty("Output file format.", "xlsx", "txt"))),
                readOnly: false)
        };

        // ---- JSON-schema 构造器（兼容 OpenAI function-calling）----

        private static JObject ObjectSchema(params (string Name, JObject Schema)[] properties)
            => ObjectSchema(System.Array.Empty<string>(), properties);

        private static JObject ObjectSchema(string[] required, params (string Name, JObject Schema)[] properties)
        {
            var propertyObject = new JObject();
            foreach (var (name, schema) in properties)
            {
                propertyObject[name] = schema;
            }

            var result = new JObject
            {
                ["type"] = "object",
                ["properties"] = propertyObject,
                ["additionalProperties"] = false
            };
            if (required.Length > 0)
            {
                var requiredItems = new JArray();
                foreach (string name in required)
                {
                    requiredItems.Add(name);
                }
                result["required"] = requiredItems;
            }
            return result;
        }

        private static JObject StringProperty(string description) => new JObject
        {
            ["type"] = "string",
            ["description"] = description
        };

        private static JObject StringEnumProperty(string description, params string[] values)
        {
            var allowed = new JArray();
            foreach (string value in values)
            {
                allowed.Add(value);
            }
            return new JObject
            {
                ["type"] = "string",
                ["description"] = description,
                ["enum"] = allowed
            };
        }

        private static JObject IntegerProperty(string description, int? minimum = null, int? maximum = null)
        {
            var result = new JObject
            {
                ["type"] = "integer",
                ["description"] = description
            };
            if (minimum.HasValue) result["minimum"] = minimum.Value;
            if (maximum.HasValue) result["maximum"] = maximum.Value;
            return result;
        }
    }
}
