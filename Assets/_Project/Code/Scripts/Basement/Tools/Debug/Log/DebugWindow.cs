using System;
using System.Collections.Generic;
using UnityEngine;
using Basement.Utils;
using UnityEditor;

namespace Basement.Logging
{
    public class DebugWindow : Singleton<DebugWindow>, ILogOutput
    {
        private LogLevel _logLevel = LogLevel.Info;
        private readonly List<string> _logLines = new List<string>();
        private Vector2 _scrollPosition;
        private bool _isVisible = false;
        private bool _autoScroll = true;
        private LogLevel _filterLevel = LogLevel.Debug;
        private string _searchText = "";
        private GUIStyle _logStyle;
        private GUIStyle _debugStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _fatalStyle;
        private int _maxLines = 1000;
        private Rect _windowRect = new Rect(10, 10, 800, 600);

        public int MaxLines
        {
            get { return _maxLines; }
            set { _maxLines = Math.Max(100, value); }
        }

        private void OnGUI()
        {
            if (!_isVisible)
                return;

            if (_logStyle == null)
                InitializeStyles();

            _windowRect = GUILayout.Window(0, _windowRect, DrawDebugWindow, "调试日志");
        }

        private void DrawDebugWindow(int windowId)
        {
            GUILayout.BeginVertical();

            DrawToolbar();
            GUILayout.Space(5);
            DrawSearchBox();
            GUILayout.Space(5);
            DrawLogContent();

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUILayout.Space(10);

            string[] levelNames = Enum.GetNames(typeof(LogLevel));
            _filterLevel = (LogLevel)GUILayout.Toolbar((int)_filterLevel, levelNames, EditorStyles.toolbarButton, GUILayout.Width(300));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(60)))
                ClearLogs();

            if (GUILayout.Button("关闭", EditorStyles.toolbarButton, GUILayout.Width(60)))
                _isVisible = false;

            GUILayout.EndHorizontal();
        }

        private void DrawSearchBox()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(50));
            _searchText = GUILayout.TextField(_searchText);
            GUILayout.EndHorizontal();
        }

        private void DrawLogContent()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

            foreach (var line in _logLines)
            {
                if (!string.IsNullOrEmpty(_searchText) || !line.Contains(_searchText))
                    continue;

                LogLevel lineLevel = GetLogLevel(line);
                if (lineLevel < _filterLevel)
                    continue;

                GUIStyle style = GetLogStyle(line);
                GUILayout.Label(line, style);
            }

            GUILayout.EndScrollView();

            if (_autoScroll)
                _scrollPosition.y = float.MaxValue;
        }

        private GUIStyle GetLogStyle(string logLine)
        {
            if (logLine.Contains("[DEBUG]"))
                return _debugStyle;
            if (logLine.Contains("[INFO]"))
                return _infoStyle;
            if (logLine.Contains("[WARNING]"))
                return _warningStyle;
            if (logLine.Contains("[ERROR]"))
                return _errorStyle;
            if (logLine.Contains("[FATAL]"))
                return _fatalStyle;
            return _logStyle;
        }

        private LogLevel GetLogLevel(string logLine)
        {
            if (logLine.Contains("[DEBUG]"))
                return LogLevel.Debug;
            if (logLine.Contains("[INFO]"))
                return LogLevel.Info;
            if (logLine.Contains("[WARNING]"))
                return LogLevel.Warning;
            if (logLine.Contains("[ERROR]"))
                return LogLevel.Error;
            if (logLine.Contains("[FATAL]"))
                return LogLevel.Fatal;
            return LogLevel.Debug;
        }

        private void InitializeStyles()
        {
            _logStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 12,
                padding = new RectOffset(5, 2, 5, 2),
                margin = new RectOffset(2, 1, 2, 1)
            };

            _debugStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _infoStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.white }
            };

            _warningStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.yellow }
            };

            _errorStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.red }
            };

            _fatalStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.magenta }
            };
        }

        private void ClearLogs()
        {
            _logLines.Clear();
            _scrollPosition = Vector2.zero;
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _logLevel)
                return;

            _logLines.Add(message);

            if (_logLines.Count > _maxLines)
                _logLines.RemoveAt(0);
        }

        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
                _isVisible = !_isVisible;
        }

        public void ToggleWindow()
        {
            _isVisible = !_isVisible;
        }

        public void ShowWindow()
        {
            _isVisible = true;
        }

        public void HideWindow()
        {
            _isVisible = false;
        }
    }
}
