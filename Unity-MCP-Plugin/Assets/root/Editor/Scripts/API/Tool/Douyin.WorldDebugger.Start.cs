/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public static partial class Tool_Douyin
    {
        public const string DouyinWorldDebuggerStartToolId = "douyin-world-debugger-start";

        [McpPluginTool
        (
            DouyinWorldDebuggerStartToolId,
            Title = "Douyin / World Debugger / Start",
            OpenWorldHint = true
        )]
        [Description("Starts Douyin World Debugger app with provided parameters. " +
            "You can provide the executable path per call, or configure a default path via config field 'douyinWorldDebuggerExecutablePath' " +
            "or environment variable 'UNITY_MCP_DOUYIN_WORLD_DEBUGGER_EXECUTABLE_PATH'.")]
        public static StartDouyinWorldDebuggerResponse StartWorldDebugger
        (
            [Description("Debugger executable path. If null, uses configured default path from config or environment override.")]
            string? debuggerExecutablePath = null,
            [Description("Target name shown in the debugger UI (调试对象).")]
            string? target = null,
            [Description("Room ID shown in the debugger UI (房间ID).")]
            int? roomId = null,
            [Description("Enable multi-client debug (多客户端调试).")]
            bool multiClientDebug = false,
            [Description("Enable data storage (启用数据存储).")]
            bool enableDataStorage = false,
            [Description("Enable performance analysis panel (启用性能分析面板).")]
            bool enablePerformancePanel = false,
            [Description("Pack resources (是否打包资源).")]
            bool packResources = true,
            [Description("Attempt to auto-start debugging after launching the app.")]
            bool autoStart = true,
            [Description("Additional raw command-line arguments passed to the debugger executable.")]
            string[]? additionalArguments = null
        )
        {
            if (string.IsNullOrWhiteSpace(debuggerExecutablePath))
                debuggerExecutablePath = UnityMcpPluginEditor.DouyinWorldDebuggerExecutablePath;

            if (string.IsNullOrWhiteSpace(debuggerExecutablePath))
                throw new ArgumentException("Debugger executable path is missing. Provide 'debuggerExecutablePath' or set config 'douyinWorldDebuggerExecutablePath' or env 'UNITY_MCP_DOUYIN_WORLD_DEBUGGER_EXECUTABLE_PATH'.", nameof(debuggerExecutablePath));

            var resolvedExecutablePath = ResolveExecutablePath(debuggerExecutablePath);
            if (!File.Exists(resolvedExecutablePath))
                throw new FileNotFoundException($"Debugger executable not found at path '{resolvedExecutablePath}'.", resolvedExecutablePath);

            var args = BuildArguments(
                target: target,
                roomId: roomId,
                multiClientDebug: multiClientDebug,
                enableDataStorage: enableDataStorage,
                enablePerformancePanel: enablePerformancePanel,
                packResources: packResources,
                autoStart: autoStart,
                additionalArguments: additionalArguments);

            return MainThread.Instance.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = resolvedExecutablePath,
                    Arguments = string.Join(" ", args.Select(QuoteArgIfNeeded)),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                var process = Process.Start(psi);
                if (process == null)
                    throw new Exception("Failed to start Douyin World Debugger process.");

                return new StartDouyinWorldDebuggerResponse
                {
                    Started = true,
                    Pid = process.Id,
                    ExecutablePath = resolvedExecutablePath,
                    Arguments = args.ToArray()
                };
            });
        }

        static string ResolveExecutablePath(string inputPath)
        {
            var trimmed = inputPath.Trim().Trim('"');
            if (trimmed.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                var appName = Path.GetFileNameWithoutExtension(trimmed);
                return Path.Combine(trimmed, "Contents", "MacOS", appName);
            }
            return trimmed;
        }

        static IEnumerable<string> BuildArguments(
            string? target,
            int? roomId,
            bool multiClientDebug,
            bool enableDataStorage,
            bool enablePerformancePanel,
            bool packResources,
            bool autoStart,
            string[]? additionalArguments)
        {
            var args = new List<string>();

            if (!string.IsNullOrWhiteSpace(target))
            {
                args.Add("--target");
                args.Add(target.Trim());
            }

            if (roomId.HasValue)
            {
                args.Add("--roomId");
                args.Add(roomId.Value.ToString());
            }

            if (multiClientDebug)
                args.Add("--multiClientDebug");

            if (enableDataStorage)
                args.Add("--enableDataStorage");

            if (enablePerformancePanel)
                args.Add("--enablePerformancePanel");

            args.Add("--packResources");
            args.Add(packResources ? "true" : "false");

            if (autoStart)
                args.Add("--start");

            if (additionalArguments != null && additionalArguments.Length > 0)
                args.AddRange(additionalArguments.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()));

            return args;
        }

        static string QuoteArgIfNeeded(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";
            if (arg.IndexOfAny(new[] { ' ', '\t', '\n', '\r', '"' }) == -1)
                return arg;
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        }

        public class StartDouyinWorldDebuggerResponse
        {
            [Description("Whether the debugger process was started.")]
            public bool Started { get; set; }

            [Description("Process ID of the started debugger.")]
            public int Pid { get; set; }

            [Description("Resolved debugger executable path.")]
            public string ExecutablePath { get; set; } = string.Empty;

            [Description("Arguments passed to the debugger executable.")]
            public string[] Arguments { get; set; } = Array.Empty<string>();
        }
    }
}
