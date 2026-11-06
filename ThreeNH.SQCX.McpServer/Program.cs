using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace ThreeNH.SQCX.McpServer;

internal static class Program
{
    private const string PipeName = "ThreeNH.SQCX.Agent.ReadOnly.v1";
    private const string ServerName = "threenh-sqcx";
    private const string ServerVersion = "0.2.0";
    private static readonly JsonSerializerOptions CompactJson = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
    private static readonly JsonSerializerOptions DisplayJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static async Task Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = new UTF8Encoding(false);

        while (await Console.In.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? request = null;
            try
            {
                request = JsonNode.Parse(line) as JsonObject
                    ?? throw new JsonException("JSON-RPC request must be an object.");
                JsonObject? response = await HandleAsync(request);
                if (response != null)
                {
                    await Console.Out.WriteLineAsync(response.ToJsonString(CompactJson));
                    await Console.Out.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                JsonNode? id = request?["id"]?.DeepClone();
                var response = Error(id, -32603, ex.Message);
                await Console.Out.WriteLineAsync(response.ToJsonString(CompactJson));
                await Console.Out.FlushAsync();
            }
        }
    }

    private static async Task<JsonObject?> HandleAsync(JsonObject request)
    {
        string? method = request["method"]?.GetValue<string>();
        JsonNode? id = request["id"]?.DeepClone();

        // JSON-RPC notifications do not receive responses.
        if (id == null)
        {
            return null;
        }

        return method switch
        {
            "initialize" => Success(id, InitializeResult(request)),
            "ping" => Success(id, new JsonObject()),
            "tools/list" => Success(id, ToolList()),
            "tools/call" => await CallToolAsync(id, request["params"] as JsonObject),
            _ => Error(id, -32601, "Method not found: " + method)
        };
    }

    private static JsonObject InitializeResult(JsonObject request)
    {
        string protocolVersion = request["params"]?["protocolVersion"]?.GetValue<string>()
            ?? "2025-06-18";

        return new JsonObject
        {
            ["protocolVersion"] = protocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject
                {
                    ["listChanged"] = false
                }
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = ServerName,
                ["version"] = ServerVersion
            },
            ["instructions"] = "Read-only access to job data in the running ThreeNH ColorQualityControl application, " +
                               "plus user-confirmed export to a new xlsx or txt file. " +
                               "Treat returned measurements as authoritative application results. " +
                               "Never claim that this server can measure, calibrate, connect, modify, save, or delete job data."
        };
    }

    private static JsonObject ToolList()
    {
        return new JsonObject
        {
            ["tools"] = new JsonArray
            {
                Tool(
                    "sqcx_get_application_context",
                    "Get the running SQCX job, measurement context, current selection, and instrument connection status. Read-only.",
                    ObjectSchema()),
                Tool(
                    "sqcx_list_standards",
                    "List standard specimens in the current SQCX job. Optionally filter by name or material. Read-only.",
                    ObjectSchema(
                        ("query", StringProperty("Case-insensitive text matched against standard name and material.")),
                        ("limit", IntegerProperty("Maximum rows to return, from 1 to 200.", 1, 200)))),
                Tool(
                    "sqcx_list_tests",
                    "List test specimens in the current SQCX job. Optionally restrict to a standard and filter by name or material. Read-only.",
                    ObjectSchema(
                        ("standard_id", IntegerProperty("Optional parent standard ID.")),
                        ("query", StringProperty("Case-insensitive text matched against test name and material.")),
                        ("limit", IntegerProperty("Maximum rows to return, from 1 to 200.", 1, 200)))),
                Tool(
                    "sqcx_get_test_details",
                    "Get authoritative Lab, color-difference and judgement values for one test under the current illuminant, observer and SCI/SCE context. Read-only.",
                    ObjectSchema(
                        new[] { "test_id" },
                        ("test_id", IntegerProperty("Test specimen ID.")),
                        ("standard_id", IntegerProperty("Parent standard ID. Supply it when test IDs repeat between standards.")))),
                Tool(
                    "sqcx_export_data",
                    "Request an export from the current SQCX job. SQCX shows a Save dialog and writes the file only after the user confirms the destination.",
                    ObjectSchema(
                        new[] { "scope", "format" },
                        ("scope", StringEnumProperty(
                            "Data to export. standard and standard_with_tests require standard_id; test requires test_id; selected uses the current SQCX selection.",
                            "selected", "standard", "standard_with_tests", "test", "all")),
                        ("standard_id", IntegerProperty("Standard specimen ID.")),
                        ("test_id", IntegerProperty("Test specimen ID.")),
                        ("format", StringEnumProperty("Output file format.", "xlsx", "txt"))),
                    readOnly: false,
                    idempotent: false)
            }
        };
    }

    private static async Task<JsonObject> CallToolAsync(JsonNode id, JsonObject? parameters)
    {
        string? toolName = parameters?["name"]?.GetValue<string>();
        JsonObject arguments = parameters?["arguments"] as JsonObject ?? new JsonObject();
        string? bridgeMethod = toolName switch
        {
            "sqcx_get_application_context" => "get_application_context",
            "sqcx_list_standards" => "list_standards",
            "sqcx_list_tests" => "list_tests",
            "sqcx_get_test_details" => "get_test_details",
            "sqcx_export_data" => "export_data",
            _ => null
        };

        if (bridgeMethod == null)
        {
            return Error(id, -32602, "Unknown tool: " + toolName);
        }

        try
        {
            JsonNode result = await ForwardToSqcxAsync(bridgeMethod, arguments);
            string text = result.ToJsonString(DisplayJson);
            return Success(id, new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = text
                    }
                },
                ["structuredContent"] = result.DeepClone(),
                ["isError"] = false
            });
        }
        catch (Exception ex)
        {
            return Success(id, new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = ex.Message
                    }
                },
                ["isError"] = true
            });
        }
    }

    private static async Task<JsonNode> ForwardToSqcxAsync(string method, JsonObject arguments)
    {
        using var pipe = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await pipe.ConnectAsync(connectTimeout.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Cannot connect to SQCX. Start ThreeNH ColorQualityControl and wait for its main window to load.");
        }

        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true
        };
        using var reader = new StreamReader(pipe, new UTF8Encoding(false), leaveOpen: true);

        var request = new JsonObject
        {
            ["method"] = method,
            ["arguments"] = arguments.DeepClone()
        };
        await writer.WriteLineAsync(request.ToJsonString(CompactJson));

        TimeSpan responseWait = method == "export_data"
            ? TimeSpan.FromMinutes(10)
            : TimeSpan.FromSeconds(10);
        using var responseTimeout = new CancellationTokenSource(responseWait);
        string? line = await reader.ReadLineAsync(responseTimeout.Token);
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new InvalidOperationException("SQCX closed the local bridge without a response.");
        }

        JsonObject response = JsonNode.Parse(line) as JsonObject
            ?? throw new InvalidOperationException("SQCX returned an invalid bridge response.");
        if (response["ok"]?.GetValue<bool>() != true)
        {
            throw new InvalidOperationException(response["error"]?.GetValue<string>() ?? "SQCX query failed.");
        }

        return response["result"]?.DeepClone() ?? new JsonObject();
    }

    private static JsonObject Tool(
        string name,
        string description,
        JsonObject inputSchema,
        bool readOnly = true,
        bool idempotent = true)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["inputSchema"] = inputSchema,
            ["annotations"] = new JsonObject
            {
                ["title"] = name,
                ["readOnlyHint"] = readOnly,
                ["destructiveHint"] = false,
                ["idempotentHint"] = idempotent,
                ["openWorldHint"] = false
            }
        };
    }

    private static JsonObject ObjectSchema(params (string Name, JsonObject Schema)[] properties)
        => ObjectSchema(Array.Empty<string>(), properties);

    private static JsonObject ObjectSchema(string[] required, params (string Name, JsonObject Schema)[] properties)
    {
        var propertyObject = new JsonObject();
        foreach (var (name, schema) in properties)
        {
            propertyObject[name] = schema;
        }

        var result = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = propertyObject,
            ["additionalProperties"] = false
        };
        if (required.Length > 0)
        {
            var requiredItems = new JsonArray();
            foreach (string name in required)
            {
                requiredItems.Add(name);
            }
            result["required"] = requiredItems;
        }

        return result;
    }

    private static JsonObject StringProperty(string description) => new()
    {
        ["type"] = "string",
        ["description"] = description
    };

    private static JsonObject StringEnumProperty(string description, params string[] values)
    {
        var allowed = new JsonArray();
        foreach (string value in values)
        {
            allowed.Add(value);
        }

        return new JsonObject
        {
            ["type"] = "string",
            ["description"] = description,
            ["enum"] = allowed
        };
    }

    private static JsonObject IntegerProperty(string description, int? minimum = null, int? maximum = null)
    {
        var result = new JsonObject
        {
            ["type"] = "integer",
            ["description"] = description
        };
        if (minimum.HasValue)
        {
            result["minimum"] = minimum.Value;
        }
        if (maximum.HasValue)
        {
            result["maximum"] = maximum.Value;
        }
        return result;
    }

    private static JsonObject Success(JsonNode id, JsonNode result) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id.DeepClone(),
        ["result"] = result
    };

    private static JsonObject Error(JsonNode? id, int code, string message) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject
        {
            ["code"] = code,
            ["message"] = message
        }
    };
}
