---
name: douyin-world-debugger-start
description: Opens the Douyin simulator settings window, applies parameters, and triggers "Start Debug" to build resources (optional) and launch DouyinWorldDebugger.
---

# Douyin / World Debugger / Start

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool douyin-world-debugger-start --input '{
  "debuggerExecutablePath": "/Users/_work/unity_workspace/DouyinWorldDebuggerEditor/DouyinWorldDebugger.app/Contents/MacOS/DouyinWorldDebugger",
  "target": "SampleScene",
  "roomId": 25001,
  "packResources": true,
  "multiClientDebug": false,
  "enableDataStorage": false,
  "enablePerformancePanel": false,
  "openDsServer": false,
  "windowless": false,
  "menuPath": "抖音虚拟创作SDK/抖音虚拟资产调试器",
  "timeoutMs": 5000,
  "startDelayFrames": 2
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `debuggerExecutablePath` | `string` | No | Debugger executable path. If omitted, it must be configured via Unity MCP config `douyinWorldDebuggerExecutablePath` or env `UNITY_MCP_DOUYIN_WORLD_DEBUGGER_EXECUTABLE_PATH`. |
| `target` | `string` | No | 调试对象 (written into the simulator settings window). |
| `roomId` | `integer` | No | 房间ID (written into the simulator settings window). |
| `multiClientDebug` | `boolean` | No | 多客户端调试. |
| `enableDataStorage` | `boolean` | No | 启用数据存储. |
| `worldData` | `string` | No | 选择世界 (used when data storage is enabled). |
| `enablePerformancePanel` | `boolean` | No | 启用性能分析面板. |
| `packResources` | `boolean` | No | 是否打包资源. If enabled, Unity may show a build/switch-platform progress dialog. |
| `openDsServer` | `boolean` | No | 启动服务器端 (DS mode). |
| `windowless` | `boolean` | No | 无窗口启动 (DS mode). |
| `menuPath` | `string` | No | Unity menu path that opens the simulator settings window. Default: `抖音虚拟创作SDK/抖音虚拟资产调试器`. |
| `timeoutMs` | `integer` | No | Timeout waiting for the simulator settings window to appear. |
| `startDelayFrames` | `integer` | No | Delay (in editor frames) between setting UI values and invoking StartDebug. Helps keep UI order stable (panel first, then build dialog). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "debuggerExecutablePath": { "type": "string" },
    "target": { "type": "string" },
    "roomId": { "type": "integer" },
    "multiClientDebug": { "type": "boolean" },
    "enableDataStorage": { "type": "boolean" },
    "worldData": { "type": "string" },
    "enablePerformancePanel": { "type": "boolean" },
    "packResources": { "type": "boolean" },
    "openDsServer": { "type": "boolean" },
    "windowless": { "type": "boolean" },
    "menuPath": { "type": "string" },
    "timeoutMs": { "type": "integer" },
    "startDelayFrames": { "type": "integer" }
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/StartDouyinWorldDebuggerResponse"
    }
  },
  "$defs": {
    "StartDouyinWorldDebuggerResponse": {
      "type": "object",
      "properties": {
        "Started": { "type": "boolean" },
        "Pid": { "type": "integer" },
        "ExecutablePath": { "type": "string" },
        "Arguments": {
          "type": "array",
          "items": { "type": "string" }
        },
        "MenuPath": { "type": "string" },
        "WindowTypeName": { "type": "string" }
      },
      "required": [
        "Started",
        "Pid",
        "ExecutablePath",
        "Arguments",
        "MenuPath",
        "WindowTypeName"
      ]
    }
  },
  "required": ["result"]
}
```
