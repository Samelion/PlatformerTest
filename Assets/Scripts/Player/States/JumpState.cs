using Assets.Scripts.StateMachine;
using Framework.Maths;
using UnityEngine;

namespace Assets.Scripts.Player
{
    sealed class JumpState : IState
    {
        const string WallJumpAnim = "WallJump";
        const string JumpAnim = "Jump";

        private readonly bool wallJump;
        private readonly bool cappable;
        private readonly Vector2? direction;
        private readonly float? force;

        private float startTime;
        private AnimationCurve accelerationCurve;

        /// <summary>
        /// Default constructor for typical jumps.
        /// </summary>
        public JumpState()
        {
            wallJump = false;
            cappable = true;
        }

        /// <summary>
        /// Constructor for wall jumps.
        /// </summary>
        public JumpState(Vector2 direction, float force)
        {
            wallJump = true;
            cappable = false;
            this.direction = direction;
            this.force = force;
        }

        public JumpState WithAccelerationCurve(AnimationCurve accelerationCurve)
        {
            this.accelerationCurve = accelerationCurve;
            return this;
        }

        public void Enter(PlayerController p)
        {
            p.SpriteManager.SetAnimation(wallJump ? WallJumpAnim : JumpAnim);

            startTime = Time.time;
            p.SetPhysicsProfile(p.PhysJumpStandard);

            // Jump, using passed values if extant.
            var d = direction ?? p.Surface.SurfaceNormal;
            var f = force ?? p.JumpForce;
            p.Jump(d, f);
        }

        public IState Execute(PlayerController p)
        {
            p.DetermineFacing();

            if (accelerationCurve != null)
            {
                var accel = p.Physics.Acceleration * accelerationCurve.Evaluate(Time.time - startTime);
                p.Move(p.Physics.Speed, accel);
            }
            else
            {
                p.Move();
            }

            // While inside the jump extend window, we don't apply gravity.
            var extendTime = p.JumpExtendTime;
            var outsideExtendWindow = Utility.Elapsed(startTime, extendTime);
            if (outsideExtendWindow)
            {
                p.ApplyGravity();
            }

            foreach (var input in p.Input)
            {
                if (input.Action == InputActions.AttackDown)
                {
                    return new AttackState();
                }

                // If we release the button mid-jump, cap our upward momentum and start to fall.
                if (cappable && input.Action == InputActions.JumpUp && !outsideExtendWindow)
                {
                    p.CapJumpIfRising();
                    return CreateFallState();
                }

                // Walljumping.
                if (input.Action == InputActions.JumpDown)
                {
                    if (p.SweepForWall(Vector2.right))
                        return PlayerController.WallJump(p, Vector2.left);
                    if (p.SweepForWall(Vector2.left))
                        return PlayerController.WallJump(p, Vector2.right);
                }

                p.Input.Release(input);
            }

            if (p.Falling)
            {
                return CreateFallState();
            }

            if (p.Surface.Unstable)
            {
                return new FallSlideState();
            }

            return null;
        }

        public void Exit(PlayerController p) {}

        private FallState CreateFallState()
        {
            if (accelerationCurve != null)
                return new FallState().WithAccelerationCurve(accelerationCurve, startTime);
            else
                return new FallState();
        }
    }
}