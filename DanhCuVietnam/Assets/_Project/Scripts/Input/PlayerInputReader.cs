using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace DanhCu.Input
{
    /// <summary>
    /// Thin wrapper over the Gameplay action map. Gameplay code reads intent from here and
    /// never touches devices directly, so AI and network-replicated players can supply the
    /// same intent through other sources (see TECHNICAL_DESIGN.md §7).
    ///
    /// Works on an instantiated copy of the asset so play-mode state never leaks into the
    /// project asset.
    /// </summary>
    public sealed class PlayerInputReader : IDisposable
    {
        private readonly InputActionAsset _asset;
        private readonly InputActionMap _gameplay;
        private readonly InputAction _pointerPosition;
        private readonly InputAction _pointerPress;
        private readonly InputAction _aim;
        private readonly InputAction _charge;
        private readonly InputAction _cameraOrbit;
        private readonly InputAction _pause;
        private bool _disposed;

        public PlayerInputReader(InputActionAsset sourceAsset)
        {
            if (sourceAsset == null)
            {
                throw new ArgumentNullException(nameof(sourceAsset));
            }

            _asset = Object.Instantiate(sourceAsset);
            _gameplay = _asset.FindActionMap(InputActionNames.GameplayMap, true);
            _pointerPosition = _gameplay.FindAction(InputActionNames.PointerPosition, true);
            _pointerPress = _gameplay.FindAction(InputActionNames.PointerPress, true);
            _aim = _gameplay.FindAction(InputActionNames.Aim, true);
            _charge = _gameplay.FindAction(InputActionNames.Charge, true);
            _cameraOrbit = _gameplay.FindAction(InputActionNames.CameraOrbit, true);
            _pause = _gameplay.FindAction(InputActionNames.Pause, true);

            _pointerPress.started += OnPressStarted;
            _pointerPress.canceled += OnPressCanceled;
            _pause.performed += OnPausePerformed;
        }

        /// <summary>Pause button pressed (any device).</summary>
        public event Action PausePressed;

        /// <summary>Pointer / touch went down: start winding.</summary>
        public event Action PressStarted;

        /// <summary>Pointer / touch released: throw.</summary>
        public event Action PressReleased;

        public bool IsGameplayEnabled => _gameplay.enabled;

        public Vector2 PointerPosition => _pointerPosition.ReadValue<Vector2>();

        public bool IsPressing => _pointerPress.IsPressed();

        public Vector2 Aim => _aim.ReadValue<Vector2>();

        public float Charge => _charge.ReadValue<float>();

        public Vector2 CameraOrbit => _cameraOrbit.ReadValue<Vector2>();

        public void EnableGameplay()
        {
            if (!_disposed)
            {
                _gameplay.Enable();
            }
        }

        public void DisableGameplay()
        {
            if (!_disposed)
            {
                _gameplay.Disable();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _pointerPress.started -= OnPressStarted;
            _pointerPress.canceled -= OnPressCanceled;
            _pause.performed -= OnPausePerformed;
            _gameplay.Disable();
            Object.Destroy(_asset);
        }

        private void OnPressStarted(InputAction.CallbackContext context) => PressStarted?.Invoke();

        private void OnPressCanceled(InputAction.CallbackContext context) => PressReleased?.Invoke();

        private void OnPausePerformed(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
