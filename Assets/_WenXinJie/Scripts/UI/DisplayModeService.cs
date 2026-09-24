using System;
using System.Collections.Generic;
using UnityEngine;

namespace WenXinJie.UI
{
    internal sealed class DisplayModeService
    {
        private const string FullScreenKey = "WenXinJie.Settings.FullScreen";
        private const string WindowHeightKey = "WenXinJie.Settings.WindowHeight";
        private const string WindowWidthKey = "WenXinJie.Settings.WindowWidth";

        private static readonly WindowResolution[] DesktopResolutions =
        {
            new(1280, 720),
            new(1366, 768),
            new(1600, 900),
            new(1920, 1080),
            new(2560, 1440),
            new(3840, 2160)
        };

        private WindowResolution[] _windowResolutions = Array.Empty<WindowResolution>();

        public bool IsFullScreen { get; private set; }
        public int SelectedResolutionIndex { get; private set; }
        public bool SupportsWindowMode { get; private set; }
        public IReadOnlyList<WindowResolution> WindowResolutions => _windowResolutions;

        public void Initialize()
        {
            SupportsWindowMode = IsDesktopPlatform(Application.platform);
            if (!SupportsWindowMode)
            {
                IsFullScreen = true;
                Screen.fullScreen = true;
                return;
            }

            _windowResolutions = FindAvailableWindowResolutions();
            IsFullScreen = PlayerPrefs.GetInt(FullScreenKey, Screen.fullScreen ? 1 : 0) == 1;
            var savedWidth = PlayerPrefs.GetInt(WindowWidthKey, Mathf.Min(1600, Screen.currentResolution.width));
            var savedHeight = PlayerPrefs.GetInt(WindowHeightKey, Mathf.RoundToInt(savedWidth * 9f / 16f));
            SelectedResolutionIndex = FindClosestResolution(savedWidth, savedHeight);
            ApplyDisplayMode();
        }

        public void SetFullScreen(bool enabled)
        {
            if (!SupportsWindowMode)
            {
                IsFullScreen = true;
                return;
            }

            IsFullScreen = enabled;
            PlayerPrefs.SetInt(FullScreenKey, enabled ? 1 : 0);
            ApplyDisplayMode();
        }

        public void SetWindowResolution(int index)
        {
            if (!SupportsWindowMode || _windowResolutions.Length == 0)
            {
                return;
            }

            SelectedResolutionIndex = Mathf.Clamp(index, 0, _windowResolutions.Length - 1);
            var resolution = _windowResolutions[SelectedResolutionIndex];
            PlayerPrefs.SetInt(WindowWidthKey, resolution.Width);
            PlayerPrefs.SetInt(WindowHeightKey, resolution.Height);
            if (!IsFullScreen)
            {
                ApplyDisplayMode();
            }
        }

        private void ApplyDisplayMode()
        {
            if (IsFullScreen)
            {
                var displayResolution = Screen.currentResolution;
                Screen.SetResolution(
                    displayResolution.width,
                    displayResolution.height,
                    FullScreenMode.FullScreenWindow);
                return;
            }

            var resolution = _windowResolutions[SelectedResolutionIndex];
            Screen.SetResolution(resolution.Width, resolution.Height, FullScreenMode.Windowed);
        }

        private static WindowResolution[] FindAvailableWindowResolutions()
        {
            var maximumWidth = Mathf.Max(Screen.currentResolution.width, Screen.width);
            var maximumHeight = Mathf.Max(Screen.currentResolution.height, Screen.height);
            var available = new List<WindowResolution>();
            foreach (var resolution in DesktopResolutions)
            {
                if (resolution.Width <= maximumWidth && resolution.Height <= maximumHeight)
                {
                    available.Add(resolution);
                }
            }

            if (available.Count == 0)
            {
                var width = Mathf.Max(960, maximumWidth);
                var height = Mathf.Min(maximumHeight, Mathf.RoundToInt(width * 9f / 16f));
                available.Add(new(width, height));
            }

            return available.ToArray();
        }

        private int FindClosestResolution(int width, int height)
        {
            var closestIndex = 0;
            var closestDistance = int.MaxValue;
            for (var index = 0; index < _windowResolutions.Length; index++)
            {
                var resolution = _windowResolutions[index];
                var distance = Math.Abs(resolution.Width - width) + Math.Abs(resolution.Height - height);
                if (distance < closestDistance)
                {
                    closestIndex = index;
                    closestDistance = distance;
                }
            }

            return closestIndex;
        }

        private static bool IsDesktopPlatform(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.WindowsPlayer
                   || platform == RuntimePlatform.WindowsEditor
                   || platform == RuntimePlatform.OSXPlayer
                   || platform == RuntimePlatform.OSXEditor
                   || platform == RuntimePlatform.LinuxPlayer
                   || platform == RuntimePlatform.LinuxEditor;
        }
    }

    internal readonly struct WindowResolution
    {
        public WindowResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
        public string Label => Width + " × " + Height;
    }
}