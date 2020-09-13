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

using SK.Libretro.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SK.Libretro.Unity
{
    public enum ManagerControlScheme
    {
        KeyboardAndMouse,
        Gamepad
    }

    [RequireComponent(typeof(PlayerInputManager))]
    public class InputProcessorManager : MonoBehaviour, IInputProcessor
    {
        [SerializeField] private bool _joinAtStartup                 = default;
        [SerializeField] private ManagerControlScheme _controlScheme = default;

        private readonly Dictionary<int, InputProcessor> _controls = new Dictionary<int, InputProcessor>();

        private void Start()
        {
            if (_joinAtStartup)
            {
                PlayerInputManager playerInputManager = GetComponent<PlayerInputManager>();

                switch (_controlScheme)
                {
                    case ManagerControlScheme.KeyboardAndMouse:
                        _ = playerInputManager.JoinPlayer(0, 0, "Keyboard & Mouse");
                        break;
                    case ManagerControlScheme.Gamepad:
                        _ = playerInputManager.JoinPlayer(0, 0, "Gamepad");
                        break;
                }
            }
        }

#pragma warning disable IDE0051 // Remove unused private members, Callbacks for the PlayerInputManager component
        private void OnPlayerJoined(PlayerInput player)
        {
            Log.Info($"Player #{player.playerIndex} joined ({player.currentControlScheme}).");
            _controls.Add(player.playerIndex, player.gameObject.GetComponent<InputProcessor>());
        }

        private void OnPlayerLeft(PlayerInput player)
        {
            Log.Info($"Player #{player.playerIndex} left ({player.currentControlScheme}).");
            _ = _controls.Remove(player.playerIndex);
        }
#pragma warning restore IDE0051 // Remove unused private members

        public bool JoypadButton(int port, int button) => _controls.ContainsKey(port) && _controls[port].JoypadButtons[button];

        public float MouseDelta(int port, int axis)      => _controls.ContainsKey(port) ? (axis == 0 ? _controls[port].MousePositionDelta.x : -_controls[port].MousePositionDelta.y) : 0f;
        public float MouseWheelDelta(int port, int axis) => _controls.ContainsKey(port) ? (axis == 0 ? _controls[port].MouseWheelDelta.y : _controls[port].MouseWheelDelta.x) : 0f;
        public bool MouseButton(int port, int button)    => _controls.ContainsKey(port) && _controls[port].MouseButtons[button];

        public bool KeyboardKey(int port, int key) => _controls.ContainsKey(port) && Input.GetKey((KeyCode)key);
    }
}
