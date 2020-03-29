﻿using SK.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

namespace SK
{
    [System.Serializable]
    public class Game
    {
        public string Core;
        public string Directory;
        public string Name;
    }

    [SelectionBase]
    public class GameModelSetup : MonoBehaviour
    {
        private Renderer _renderer = null;
        private Material _originalMaterial = null;

        public Libretro.Wrapper Wrapper { get; private set; }

        public Game Game;
        private TextMeshProUGUI _infoText = null;
        private string _coreInfoText = string.Empty;

        private void Awake()
        {
            _renderer = transform.GetChild(0).GetChild(1).GetComponent<Renderer>();
            _originalMaterial = _renderer.sharedMaterial;

            GameObject uiNode = GameObject.Find("UI");
            if (uiNode != null)
            {
                _infoText = uiNode.GetComponentInChildren<TextMeshProUGUI>();
            }

            Game = FileSystem.DeserializeFromJson<Game>($"{Application.streamingAssetsPath}/Game.json");
        }

        private void Start()
        {
            StartGame();
        }

        private void OnEnable()
        {
            Libretro.Wrapper.OnCoreStartedEvent += CoreStartedCallback;
            Libretro.Wrapper.OnCoreStoppedEvent += CoreStoppedCallback;
            Libretro.Wrapper.OnGameStartedEvent += GameStartedCallback;
            Libretro.Wrapper.OnGameStoppedEvent += GameStoppedCallback;
        }

        private void OnDisable()
        {
            Libretro.Wrapper.OnCoreStartedEvent -= CoreStartedCallback;
            Libretro.Wrapper.OnCoreStoppedEvent -= CoreStoppedCallback;
            Libretro.Wrapper.OnGameStartedEvent -= GameStartedCallback;
            Libretro.Wrapper.OnGameStoppedEvent -= GameStoppedCallback;
        }

        private void OnDestroy()
        {
            StopGame();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                else if (Cursor.lockState == CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                Cursor.visible = !Cursor.visible;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopGame();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(0);
#endif
            }
        }

        private void StartGame()
        {
            if (Game != null)
            {
                Wrapper = new Libretro.Wrapper();

                string corePath = FileSystem.GetAbsolutePath($"{Libretro.Wrapper.WrapperDirectory}/cores/{Game.Core}_libretro.dll");
                if (FileSystem.FileExists(corePath))
                {
                    Wrapper.StartCore(corePath);

                    if (!string.IsNullOrEmpty(Game.Name))
                    {
                        string gamePath = Wrapper.GetGamePath(Game.Directory, Game.Name);
                        if (gamePath == null)
                        {
                            // Try Zip archive
                            string archivePath = FileSystem.GetAbsolutePath($"{Game.Directory}/{Game.Name}.zip");
                            if (File.Exists(archivePath))
                            {
                                string extractPath = FileSystem.GetAbsolutePath($"{Libretro.Wrapper.WrapperDirectory}/temp");
                                if (Directory.Exists(extractPath))
                                {
                                    Directory.Delete(extractPath, true);
                                }
                                System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, extractPath);
                                gamePath = Wrapper.GetGamePath(extractPath, Game.Name);
                            }
                        }

                        if (gamePath != null)
                        {
                            Wrapper.StartGame(gamePath);
                            InvokeRepeating("LibretroRunLoop", 0f, 1f / Wrapper.Game.Fps);
                        }
                        else
                        {
                            Log.Error($"Game '{Game.Name}' at path '{Game.Directory}' not found.", "GameModelSetup.StartGame");
                        }
                    }
                    else
                    {
                        Log.Warning($"Game not set, running core only.", "GameModelSetup.StartGame");
                    }
                }
                else
                {
                    Log.Error($"Core '{Game.Core}' at path '{corePath}' not found.", "GameModelSetup.StartGame");
                }
            }
            else
            {
                Log.Error($"Game instance is null.", "GameModelSetup.StartGame");
            }
        }

        public void StopGame()
        {
            if (Wrapper != null)
            {
                CancelInvoke();
                Wrapper.StopGame();
                Wrapper = null;
            }
        }

        [SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Called by InvokeRepeating")]
        private void LibretroRunLoop()
        {
            if (Wrapper != null)
            {
                Wrapper.Update();

                if (Wrapper.Texture != null)
                {
                    _renderer.material.mainTexture = Wrapper.Texture;
                    _renderer.material.SetTexture("_EmissionMap", Wrapper.Texture);
                }
            }
        }

        private void CoreStartedCallback(Libretro.Wrapper.LibretroCore core)
        {
            if (core != null)
            {
                if (_infoText != null)
                {
                    StringBuilder sb = new StringBuilder()
                        .AppendLine("<color=yellow>Core Info:</color>")
                        .Append("<size=12>")
                        .AppendLine($"<color=lightblue>Api Version:</color> {core.ApiVersion}")
                        .AppendLine($"<color=lightblue>Name:</color> {core.CoreName}")
                        .AppendLine($"<color=lightblue>Version:</color> {core.CoreVersion}")
                        .AppendLine($"<color=lightblue>ValidExtensions:</color> {string.Join("|", core.ValidExtensions)}")
                        .AppendLine($"<color=lightblue>RequiresFullPath:</color> {core.RequiresFullPath}")
                        .AppendLine($"<color=lightblue>BlockExtraction:</color> {core.BlockExtraction}")
                        .Append("</size>");
                    _coreInfoText = sb.ToString();
                    _infoText.SetText(_coreInfoText);
                }
            }
        }

        private void CoreStoppedCallback(Libretro.Wrapper.LibretroCore core)
        {
            if (_infoText != null)
            {
                _infoText.SetText(string.Empty);
            }
        }

        private void GameStartedCallback(Libretro.Wrapper.LibretroGame game)
        {
            if (game != null)
            {
                if (_infoText != null)
                {
                    StringBuilder sb = new StringBuilder(_infoText.text)
                        .AppendLine()
                        .AppendLine("<color=yellow>Game Info:</color>")
                        .Append("<size=12>")
                        .AppendLine($"<color=lightblue>BaseWidth:</color> {game.BaseWidth}")
                        .AppendLine($"<color=lightblue>BaseHeight:</color> {game.BaseHeight}")
                        .AppendLine($"<color=lightblue>MaxWidth:</color> {game.MaxWidth}")
                        .AppendLine($"<color=lightblue>MaxHeight:</color> {game.MaxHeight}")
                        .AppendLine($"<color=lightblue>AspectRatio:</color> {game.AspectRatio}")
                        .AppendLine($"<color=lightblue>TargetFps:</color> {game.Fps}")
                        .AppendLine($"<color=lightblue>SampleRate:</color> {game.SampleRate}")
                        .Append("</size>");

                    _infoText.SetText(sb.ToString());
                }
            }

            _renderer.material.mainTextureScale = new Vector2(1f, -1f);
            _renderer.material.EnableKeyword("_EMISSION");
            _renderer.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            _renderer.material.SetColor("_EmissionColor", Color.white);
        }

        private void GameStoppedCallback(Libretro.Wrapper.LibretroGame game)
        {
            if (_infoText != null)
            {
                _infoText.SetText(string.Empty);
            }

            _renderer.material = _originalMaterial;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}