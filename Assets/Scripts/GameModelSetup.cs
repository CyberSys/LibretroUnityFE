﻿using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [HideInInspector] public Game Game;

        public Libretro.Wrapper Wrapper { get; private set; }

        private Renderer _rendererComponent = null;
        private Material _originalMaterial = null;

        private void OnEnable()
        {
            StartGame();
        }

        private void OnDisable()
        {
            StopGame();
        }

        public void StartGame()
        {
            if (Game != null && !string.IsNullOrEmpty(Game.Core))
            {
                if (transform.childCount > 0)
                {
                    Transform modelTransform = transform.GetChild(0);
                    if (modelTransform != null && modelTransform.childCount > 1)
                    {
                        Transform screenTransform = modelTransform.GetChild(1);
                        if (screenTransform.TryGetComponent(out _rendererComponent))
                        {
                            Wrapper = new Libretro.Wrapper();
                            if (Wrapper.StartGame(Game.Core, Game.Directory, Game.Name))
                            {
                                ActivateGraphics();
                                ActivateAudio();
                                ActivateInput();

                                _originalMaterial = _rendererComponent.sharedMaterial;
                                _rendererComponent.material.mainTextureScale = new Vector2(1f, -1f);
                                _rendererComponent.material.color = Color.black;
                                _rendererComponent.material.EnableKeyword("_EMISSION");
                                _rendererComponent.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                                _rendererComponent.material.SetColor("_EmissionColor", Color.white);

                                InvokeRepeating("LibretroRunLoop", 0f, 1f / (float)Wrapper.Game.SystemAVInfo.timing.fps);
                            }
                        }
                    }
                }
            }
        }

        public void StopGame()
        {
            if (_rendererComponent != null && _rendererComponent.material != null && _originalMaterial != null)
            {
                _rendererComponent.material = _originalMaterial;
            }

            CancelInvoke();
            Wrapper?.StopGame();
            Wrapper = null;
        }

        public void ActivateGraphics()
        {
            Wrapper?.ActivateGraphics(new Libretro.UnityGraphicsProcessor());
        }

        public void DeactivateGraphics()
        {
            Wrapper?.DeactivateGraphics();
        }

        public void ActivateAudio()
        {
            Libretro.UnityAudioProcessorComponent unityAudio = GetComponentInChildren<Libretro.UnityAudioProcessorComponent>();
            if (unityAudio != null)
            {
                Wrapper?.ActivateAudio(unityAudio);
            }
            else
            {
                Wrapper?.ActivateAudio(new Libretro.NAudioAudioProcessor());
            }
        }

        public void DeactivateAudio()
        {
            Wrapper?.DeactivateAudio();
        }

        public void ActivateInput()
        {
            Wrapper?.ActivateInput(FindObjectOfType<PlayerInputManager>().GetComponent<Libretro.IInputProcessor>());
        }

        public void DeactivateInput()
        {
            Wrapper?.DeactivateInput();
        }

        [SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Called by InvokeRepeating")]
        private void LibretroRunLoop()
        {
            if (Wrapper != null)
            {
                Wrapper.Update();

                if (Wrapper.GraphicsProcessor != null && Wrapper.GraphicsProcessor is Libretro.UnityGraphicsProcessor unityGraphics)
                {
                    if (unityGraphics.Texture != null)
                    {
                        _rendererComponent.material.SetTexture("_EmissionMap", unityGraphics.Texture);
                    }
                }
            }
        }
    }
}
