using Assets.Framework.Maths;
using Assets.Scripts.Maths;
using Framework.Maths;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Interactions;
using static UnityEngine.InputSystem.InputAction;

namespace Assets.Scripts.Player
{
    enum InputActions
    {
        JumpUp,
        AttackUp,
        JumpDown,
        AttackDown,
    }
    
    /// <summary>
    /// Represents a single input event, and the timestamp of when it occurred.
    /// </summary>
    sealed class InputData
    {
        public readonly InputActions Action;
        public readonly float Timestamp;

        public bool Locked;

        public InputData(InputActions action)
        {
            Action = action;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Records input into a buffer which can be accessed through an Enumerator. 
    /// The PlayerInputProvider needs to be primed before use each step to manage the state of buffered inputs.
    /// When iterating over the buffer, all inputs are sent out 'locked'. If the consumer doesn't call [Release] on an input,
    /// is it considered consumed. As a result, it is best to break out of iteration once the first valid input is found.
    /// </summary>
    sealed class PlayerInputProvider : MonoBehaviour, IEnumerable<InputData>
    {
        [SerializeField] float bufferWindow = 0.2f;
        [SerializeField, Range(0f, 1f)] float innerDeadzone = 0.15f;
        [SerializeField, Range(0f, 1f)] float outerDeadzone = 0.85f;

        public Vector2 Movement { get; private set; }
        public Vector2 MovementHorizontal { get; private set; }

        PlayerInput inputProvider;

        // TODO: Separate buffers for each input type?
        List<InputData> buffer = new List<InputData>();

        public int BufferCount => buffer.Count;

        private void Awake()
        {
            inputProvider = new PlayerInput();

            inputProvider.Player.Move.performed += MovePerformed;
            inputProvider.Player.Move.started += MovePerformed;
            inputProvider.Player.Move.canceled += MovePerformed;

            inputProvider.Player.Jump.started += JumpPerformed;
            //inputProvider.Player.Jump.performed += JumpPerformed;

            inputProvider.Player.Attack.started += AttackPerformed;
            //inputProvider.Player.Attack.performed += AttackPerformed;
        }

        #region Event Consumers

        private void AttackPerformed(CallbackContext context)
        {
            switch (context.interaction)
            {
                case PressInteraction _:
                    var control = context.control as ButtonControl;

                    if (control.wasPressedThisFrame && control.isPressed)
                        buffer.Add(new InputData(InputActions.AttackDown));

                    if (control.wasReleasedThisFrame && !control.isPressed)
                        buffer.Add(new InputData(InputActions.AttackUp));
                    break;
                default:
                    break;
            }
        }

        private void JumpPerformed(CallbackContext context)
        {
            switch (context.interaction)
            {
                case PressInteraction _:
                    var control = context.control as ButtonControl;

                    if (control.wasPressedThisFrame && control.isPressed)
                        buffer.Add(new InputData(InputActions.JumpDown));

                    if (control.wasReleasedThisFrame && !control.isPressed)
                        buffer.Add(new InputData(InputActions.JumpUp));
                    break;
                default:
                    break;
            }
        }

        private void MovePerformed(CallbackContext context)
        {
            var raw = context.ReadValue<Vector2>();

            var m = InputFiltering.ApplyDeadzone(raw, innerDeadzone, outerDeadzone);
            var h = InputFiltering.ApplyDeadzone(raw.x0(), innerDeadzone, outerDeadzone);

            Movement = m.normalized;
            MovementHorizontal = h.normalized;
        }

        private void OnEnable()
        {
            inputProvider.Enable();
        }

        private void OnDisable()
        {
            inputProvider.Disable();
        }

        #endregion

        #region Input Buffer
        
        public void Prime()
        {
            // Remove all still-locked (and therefore consumed) inputs,
            // or inputs that have left the buffer window.
            buffer.RemoveAll(x =>
                x.Locked ||
                Utility.Elapsed(x.Timestamp, bufferWindow));
        }

        public void Release(InputData data)
        {
            data.Locked = false;
        }

        public IEnumerator<InputData> GetEnumerator()
        {
            foreach (var input in buffer)
            {
                if (input.Locked) continue;

                // All inputs requested are sent out 'locked'. If the 
                // consumer doesn't call [Release] on the input, it is
                // considered consumed, and will be removed.
                input.Locked = true;
                yield return input;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}