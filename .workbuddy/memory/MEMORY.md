# ColorQualityControl / SQCX 项目速记

## 架构总览
- `ColorQualityControl` 是 WPF 表现层（.NET Framework 4.8）。`ThreeNH.SQCX.{Application,Domain,Infrastructure,McpServer}` 是正在增量迁移的 Clean Architecture 三层 + MCP 服务。详见 `ARCHITECTURE.md`。
- 仪器适配器已合并到 `ThreeNH.SQCX.Infrastructure/Instruments/`（PTS / ColorReader / LedSpectrometer / GratingSpectrometer / CR400 / BTS / NRColorimeter / YRColorimeter / YL30 / ThreeNH.YL30 / Spectrophotometer）。共享项目 ColorAlgorithm / OpticalDefines / ThreeNH.Bluetooth / ThreeNH.Helper / ThreeNH.StructHelper / ThreeNH.Instruments.Abstractions 保留独立 dll。

## SQCX MCP（只读上下文）
- 服务：`ThreeNH.SQCX.McpServer`（.NET 8 stdio）→ 通过命名管道 `ThreeNH.SQCX.Agent.ReadOnly.v1` 与 WPF 端 `ColorQualityControl/Agent/Mcp/SqcxMcpBridgeHost.cs` 通信；查询在 `SqcxAgentQueryService.cs`。
- MCP 注册位置：`~/.workbuddy/mcp.json` → `mcpServers.threenh-sqcx`（`dotnet … ThreeNH.SQCX.McpServer.dll`）。本会话未自动加载其工具。
- 暴露的 4 个只读工具：`sqcx_get_application_context`（应用/作业/测量上下文/选样/仪器/安全声明）、`sqcx_list_standards`、`sqcx_list_tests`、`sqcx_get_test_details`。
- **必须先启动 `ColorQualityControl.exe`，否则命名管道不存在，MCP 调用会报 “Cannot connect to SQCX”**。

## 关键约定
- WPF 端 `MainWindow.Database / Colorimeter / CurrentStandard / CurrentTest / SpecularComponent / Illuminant / Observer / Session.Instrument.ConnectionState / InstrumentType / DeviceSN` 是所有只读快照的根。
- 序列号经 `RedactSerialNumber` 遮蔽（保留末 4 位）。NaN/Infinity 由 `Finite()` 转为 `null`。
- 测量命令编排走 `Domain/Measurement/MeasurementTarget` → `Application/Measurement/ExecuteMeasurementUseCase` → `ColorQualityControl/Adapter/LegacyMeasurementGateway`。
