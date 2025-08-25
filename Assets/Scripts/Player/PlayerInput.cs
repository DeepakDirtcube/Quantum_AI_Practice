using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Deterministic;
using Quantum;

namespace SimpleFPS
{
	/// <summary>
	/// Handles player input.
	/// </summary>
	[DefaultExecutionOrder(-10)]
	public sealed class PlayerInput : MonoBehaviour
	{
		public static float LookSensitivity;

		[SerializeField]
		private QuantumEntityViewUpdater _entityViewUpdater;
		[SerializeField]
		private RectTransform _fireButton;

		private Quantum.Input _accumulatedInput;
		private bool          _resetAccumulatedInput;
		private int           _lastAccumulateFrame;
		private InputTouches  _inputTouches = new InputTouches();
		private InputTouch    _moveTouch;
		private InputTouch    _lookTouch;
		private bool          _jumpTouch;
		private float         _jumpTime;

		private void OnEnable()
		{
			QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));

			_inputTouches.TouchStarted  = OnTouchStarted;
			_inputTouches.TouchFinished = OnTouchFinished;
		}

		private void OnDisable()
		{
			_inputTouches.TouchStarted  = null;
			_inputTouches.TouchFinished = null;
		}

		private void Update()
		{
			AccumulateInput();
		}

		private void AccumulateInput()
		{
			if (_lastAccumulateFrame == Time.frameCount)
				return;

			_lastAccumulateFrame = Time.frameCount;

			if (_resetAccumulatedInput)
			{
				_resetAccumulatedInput = false;
				_accumulatedInput = default;
			}

			if (Application.isMobilePlatform && Application.isEditor == false)
			{
				_inputTouches.Update();

				ProcessMobileInput();
			}
			else
			{
				ProcessStandaloneInput();
			}
		}

		private void ProcessStandaloneInput()
		{
			// Enter key is used for locking/unlocking cursor in game view.
			Keyboard keyboard = Keyboard.current;
			if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
			{
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
				else
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}

			// Accumulate input only if the cursor is locked.
			if (Cursor.lockState != CursorLockMode.Locked)
				return;

			Mouse mouse = Mouse.current;
			if (mouse != null)
			{
				Vector2 mouseDelta = mouse.delta.ReadValue();

				Vector2 lookRotationDelta = new Vector2(-mouseDelta.y, mouseDelta.x);
				lookRotationDelta *= LookSensitivity / 60f;
				_accumulatedInput.LookRotationDelta += lookRotationDelta.ToFPVector2();

				_accumulatedInput.Fire |= mouse.leftButton.isPressed;
			}

			if (keyboard != null)
			{
				Vector2 moveDirection = Vector2.zero;

				if (keyboard.wKey.isPressed) { moveDirection += Vector2.up;    }
				if (keyboard.sKey.isPressed) { moveDirection += Vector2.down;  }
				if (keyboard.aKey.isPressed) { moveDirection += Vector2.left;  }
				if (keyboard.dKey.isPressed) { moveDirection += Vector2.right; }

				_accumulatedInput.MoveDirection = moveDirection.normalized.ToFPVector2();

				_accumulatedInput.Jump    |= keyboard.spaceKey.isPressed;
				_accumulatedInput.Reload  |= keyboard.rKey.isPressed;
				_accumulatedInput.Spray   |= keyboard.fKey.isPressed;

				for (int i = (int)Key.Digit1; i <= (int)Key.Digit9; i++)
				{
					if (keyboard[(Key)i].isPressed)
					{
						_accumulatedInput.Weapon = (byte)(i - Key.Digit1 + 1);
						break;
					}
				}
			}
		}

		private void ProcessMobileInput()
		{
			if (_lookTouch != null && _lookTouch.IsActive)
			{
				Vector2 lookRotationDelta = new Vector2(-_lookTouch.Delta.Position.y, _lookTouch.Delta.Position.x);
				lookRotationDelta *= LookSensitivity / 15f;

				_accumulatedInput.LookRotationDelta += lookRotationDelta.ToFPVector2();
			}

			if (_moveTouch != null && _moveTouch.IsActive && _moveTouch.GetDelta().Position.Equals(default) == false)
			{
				float screenSizeFactor = 8.0f / Mathf.Min(Screen.width, Screen.height);

				Vector2 moveDirection = new Vector2(_moveTouch.GetDelta().Position.x, _moveTouch.GetDelta().Position.y) * screenSizeFactor;
				if (moveDirection.sqrMagnitude > 1.0f)
				{
					moveDirection.Normalize();
				}

				_accumulatedInput.MoveDirection = moveDirection.ToFPVector2();
			}

			_accumulatedInput.Jump |= _jumpTouch;
		}

		private void OnTouchStarted(InputTouch touch)
		{
			if (IsTouchInsideRect(_fireButton, touch.Start.Position) == true)
			{
				_accumulatedInput.Fire |= true;
				return;
			}

			if (_moveTouch == null && touch.Start.Position.x < Screen.width * 0.5f)
			{
				_moveTouch = touch;
			}

			if (_lookTouch == null && touch.Start.Position.x > Screen.width * 0.5f)
			{
				_lookTouch = touch;
				_jumpTouch = default;

				if (_jumpTime > Time.realtimeSinceStartup - 0.25f)
				{
					_jumpTouch = true;
				}

				_jumpTime = Time.realtimeSinceStartup;
			}
		}

		private void OnTouchFinished(InputTouch touch)
		{
			if (_moveTouch == touch) { _moveTouch = default; }
			if (_lookTouch == touch) { _lookTouch = default; _jumpTouch = default; }
		}

		public void PollInput(CallbackPollInput callback)
		{
			AccumulateInput();

			_accumulatedInput.InterpolationOffset = (byte)Mathf.Clamp(callback.Frame - _entityViewUpdater.SnapshotInterpolation.CurrentFrom, 0, 255);
			_accumulatedInput.InterpolationAlpha  = _entityViewUpdater.SnapshotInterpolation.Alpha.ToFP();

			callback.SetInput(_accumulatedInput, DeterministicInputFlags.Repeatable);

			_resetAccumulatedInput = true;
			_accumulatedInput.LookRotationDelta = default;
		}

		private static bool IsTouchInsideRect(RectTransform rectTransform, Vector2 touchPosition)
		{
			Vector2 rectPositionMin = new Vector2(rectTransform.anchorMin.x * Screen.width, rectTransform.anchorMin.y * Screen.height);
			Vector2 rectPositionMax = new Vector2(rectTransform.anchorMax.x * Screen.width, rectTransform.anchorMax.y * Screen.height);

			return touchPosition.x >= rectPositionMin.x && touchPosition.x <= rectPositionMax.x && touchPosition.y >= rectPositionMin.y && touchPosition.y <= rectPositionMax.y;
		}
	}
}
