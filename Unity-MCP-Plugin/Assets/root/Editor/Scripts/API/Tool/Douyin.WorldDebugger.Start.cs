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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Douyin
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
        public async Task<StartDouyinWorldDebuggerResponse> StartWorldDebugger
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
            [Description("Selected world data (选择世界) used when data storage enabled.")]
            string? worldData = null,
            [Description("Enable performance analysis panel (启用性能分析面板).")]
            bool enablePerformancePanel = false,
            [Description("Pack resources (是否打包资源).")]
            bool packResources = true,
            [Description("Open server side in DS mode (启动服务器端).")]
            bool openDsServer = false,
            [Description("Windowless mode in DS mode (无窗口启动).")]
            bool windowless = false,
            [Description("Menu path to open Douyin simulator settings window.")]
            string menuPath = "抖音虚拟创作SDK/抖音虚拟资产调试器",
            [Description("Timeout to wait for the settings window to be created (ms).")]
            int timeoutMs = 5000,
            [Description("How many editor frames to wait after configuring the window before invoking StartDebug.")]
            int startDelayFrames = 2
        )
        {
            if (string.IsNullOrWhiteSpace(debuggerExecutablePath))
                debuggerExecutablePath = UnityMcpPluginEditor.DouyinWorldDebuggerExecutablePath;

            if (string.IsNullOrWhiteSpace(debuggerExecutablePath))
                throw new ArgumentException("Debugger executable path is missing. Provide 'debuggerExecutablePath' or set config 'douyinWorldDebuggerExecutablePath' or env 'UNITY_MCP_DOUYIN_WORLD_DEBUGGER_EXECUTABLE_PATH'.", nameof(debuggerExecutablePath));

            var resolvedExecutablePath = ResolveExecutablePath(debuggerExecutablePath);
            if (!File.Exists(resolvedExecutablePath))
                throw new FileNotFoundException($"Debugger executable not found at path '{resolvedExecutablePath}'.", resolvedExecutablePath);

            if (timeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be greater than zero.");

            return await StartViaSimulatorSettingsWindowAsync(
                resolvedExecutablePath: resolvedExecutablePath,
                target: target,
                roomId: roomId,
                multiClientDebug: multiClientDebug,
                enableDataStorage: enableDataStorage,
                worldData: worldData,
                enablePerformancePanel: enablePerformancePanel,
                packResources: packResources,
                openDsServer: openDsServer,
                windowless: windowless,
                menuPath: menuPath,
                timeoutMs: timeoutMs,
                startDelayFrames: startDelayFrames);
        }

        static async Task<StartDouyinWorldDebuggerResponse> StartViaSimulatorSettingsWindowAsync(
            string resolvedExecutablePath,
            string? target,
            int? roomId,
            bool multiClientDebug,
            bool enableDataStorage,
            string? worldData,
            bool enablePerformancePanel,
            bool packResources,
            bool openDsServer,
            bool windowless,
            string menuPath,
            int timeoutMs,
            int startDelayFrames)
        {
            var tcs = new TaskCompletionSource<StartDouyinWorldDebuggerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cts = new System.Threading.CancellationTokenSource(timeoutMs);

            await MainThread.Instance.RunAsync(() =>
            {
                var windowType = GetSimulatorSettingsWindowType();
                if (windowType == null)
                {
                    tcs.TrySetException(new Exception("SimulatorSettingsWindow type not found in loaded assemblies."));
                    return;
                }

                var window = Resources.FindObjectsOfTypeAll(windowType).FirstOrDefault() as EditorWindow;
                if (window == null)
                {
                    var executed = EditorApplication.ExecuteMenuItem(menuPath);
                    if (!executed)
                    {
                        tcs.TrySetException(new Exception($"Failed to execute menu item '{menuPath}'."));
                        return;
                    }
                }

                var startTime = EditorApplication.timeSinceStartup;
                EditorApplication.CallbackFunction? tick = null;
                tick = () =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        EditorApplication.update -= tick;
                        tcs.TrySetException(new TimeoutException("Timed out waiting for SimulatorSettingsWindow instance."));
                        return;
                    }

                    var window = Resources.FindObjectsOfTypeAll(windowType).FirstOrDefault() as EditorWindow;
                    if (window == null)
                        return;

                    EditorApplication.update -= tick;

                    try
                    {
                        var root = window.rootVisualElement;

                        var currentSimulatorPathField = windowType.GetField("currentSimulatorPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        currentSimulatorPathField?.SetValue(window, resolvedExecutablePath);

                        TrySetValue(root, "m_path", resolvedExecutablePath);

                        if (!string.IsNullOrWhiteSpace(target))
                            TrySetValue(root, "m_SelectRes", target.Trim());

                        if (roomId.HasValue)
                        {
                            TrySetValue(root, "m_roomID", roomId.Value.ToString());

                            var roomField = windowType.GetField("RoomID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (roomField?.GetValue(window) is TextField roomTf)
                                roomTf.value = roomId.Value.ToString();
                        }

                        TrySetToggle(root, "m_Multiplayer", multiClientDebug);
                        TrySetToggle(root, "m_OpenDataStorge", enableDataStorage);
                        TrySetToggle(root, "m_ProfilerDebug", enablePerformancePanel);
                        TrySetToggle(root, "m_BuildPackage", packResources);
                        TrySetToggle(root, "m_OpenDsServer", openDsServer);
                        TrySetToggle(root, "m_Windowless", windowless);

                        if (!string.IsNullOrWhiteSpace(worldData))
                            TrySetValue(root, "m_SelectWorldData", worldData.Trim());

                        var startDebugMethod = windowType.GetMethod("StartDebug", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (startDebugMethod == null)
                            throw new MissingMethodException(windowType.FullName, "StartDebug");

                        try
                        {
                            window.Focus();
                            window.Repaint();
                        }
                        catch
                        {
                        }

                        ScheduleStartDebug(window, startDebugMethod, Math.Max(0, startDelayFrames));

                        tcs.TrySetResult(new StartDouyinWorldDebuggerResponse
                        {
                            Started = true,
                            Pid = 0,
                            ExecutablePath = resolvedExecutablePath,
                            Arguments = Array.Empty<string>(),
                            MenuPath = menuPath,
                            WindowTypeName = windowType.FullName ?? windowType.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                };

                EditorApplication.update += tick;
            });

            return await tcs.Task;
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

        static Type? GetSimulatorSettingsWindowType()
        {
            var direct = Type.GetType("SimulatorSettingsWindow, com.douyin.world.editor");
            if (direct != null && typeof(EditorWindow).IsAssignableFrom(direct))
                return direct;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }
                var t = types.FirstOrDefault(x => x.Name == "SimulatorSettingsWindow" && typeof(EditorWindow).IsAssignableFrom(x));
                if (t != null)
                    return t;
            }

            return null;
        }

        static void TrySetValue(VisualElement root, string name, string value)
        {
            var el = root.Q<TextField>(name);
            if (el != null)
                el.value = value;
        }

        static void TrySetToggle(VisualElement root, string name, bool value)
        {
            var el = root.Q<Toggle>(name);
            if (el != null)
                el.value = value;
        }

        static void ScheduleStartDebug(EditorWindow window, MethodInfo startDebugMethod, int delayFrames)
        {
            var framesLeft = delayFrames;
            EditorApplication.CallbackFunction? tick = null;
            tick = () =>
            {
                if (framesLeft > 0)
                {
                    framesLeft--;
                    return;
                }

                EditorApplication.update -= tick;
                try
                {
                    startDebugMethod.Invoke(window, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
            EditorApplication.update += tick;
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

            [Description("Menu path used to open the settings window.")]
            public string MenuPath { get; set; } = string.Empty;

            [Description("Type name of the settings window used to start debugging.")]
            public string WindowTypeName { get; set; } = string.Empty;
        }
    }
}
