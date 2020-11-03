﻿/* MIT License

 * Copyright (c) 2020 Skurdt
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. */

using SK.Libretro.Unity;
using SK.UnityUtilities;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SK.Examples
{
    [SelectionBase, DisallowMultipleComponent]
    internal abstract class GameModelSetup : MonoBehaviour
    {
        [Serializable]
        private struct ContentSettings
        {
            public string Core;
            public string Directory;
            public string Name;
            public bool AnalogDirectionsToDigital;
        }

        [SerializeField] private bool _analogDirectionsToDigital = false;

        public string CoreName { get; set; }
        public string GameDirectory { get; set; }
        public string GameName { get; set; }

        protected Transform _viewer        = null;
        protected LibretroBridge _libretro = null;

        private void Awake() => _viewer = Camera.main.transform;

        private void Start()
        {
            LoadConfig();
            StartGame();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StopGame();
                ApplicationUtils.ExitApp();
                return;
            }

            OnUpdate();

            _libretro?.Update();
        }

        private void OnEnable() => Application.focusChanged += OnApplicationFocusChanged;

        private void OnDisable()
        {
            Application.focusChanged -= OnApplicationFocusChanged;
            StopGame();
        }

        protected virtual void OnUpdate()
        {
        }

        public void Pause() => _libretro?.Pause();

        public void Resume() => _libretro?.Resume();

        public bool SaveState(int index, bool saveScreenshot = true) => _libretro != null && _libretro.SaveState(index, saveScreenshot);

        public bool LoadState(int index) => _libretro != null && _libretro.LoadState(index);

        private static readonly string _configFilePath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "config.json"));

        protected void StartGame()
        {
            if (string.IsNullOrEmpty(CoreName))
            {
                Debug.LogError("Core not set");
                return;
            }

            ScreenNode screen = GetComponentInChildren<ScreenNode>();
            if (screen == null)
            {
                Debug.LogWarning($"ScreenNode not found, adding ScreenNode component to the same node this script is attached to ({name})");
                screen = gameObject.AddComponent<ScreenNode>();
            }

            if (screen.GetComponent<Renderer>() == null)
            {
                Debug.LogError("Component of type Renderer not found");
                return;
            }

            LibretroBridgeSettings settings = new LibretroBridgeSettings
            {
                AnalogDirectionsToDigital = _analogDirectionsToDigital
            };
            _libretro = new LibretroBridge(screen, _viewer, settings);
            if (!_libretro.Start(CoreName, GameDirectory, GameName))
            {
                StopGame();
                return;
            }
        }

        protected void StopGame()
        {
            _libretro?.Stop();
            _libretro = null;
        }

        [ContextMenu("Load configuration")]
        protected void LoadConfig()
        {
            if (!File.Exists(_configFilePath))
                return;

            string json = File.ReadAllText(_configFilePath);
            if (string.IsNullOrEmpty(json))
                return;

            ContentSettings game     = JsonUtility.FromJson<ContentSettings>(json);
            CoreName      = game.Core;
            GameDirectory = game.Directory;
            GameName      = game.Name;
            _analogDirectionsToDigital = game.AnalogDirectionsToDigital;
        }

        [ContextMenu("Save configuration")]
        protected void SaveConfig()
        {
            ContentSettings game = new ContentSettings
            {
                Core      = CoreName,
                Directory = GameDirectory,
                Name      = GameName,
                AnalogDirectionsToDigital = _analogDirectionsToDigital
            };
            string json = JsonUtility.ToJson(game, true);
            File.WriteAllText(_configFilePath, json);
        }

        private void OnApplicationFocusChanged(bool focus)
        {
            if (!focus)
                _libretro?.Pause();
            else
                _libretro?.Resume();
        }
    }
}
