using Assets.Scripts.StateMachine;
using Framework.Maths;
using UnityEngine;

namespace Assets.Scripts.Player
{
    sealed class FallState : IState
    {
        private float startTime;
        private float atTime;
        private AnimationCurve accelerationCurve;

        public FallState WithAccelerationCurve(AnimationCurve accelerationCurve, float atTime)
        {
            this.accelerationCurve = accelerationCurve;
            this.atTime = atTime;
            return this;
        }

        public void Enter(PlayerController p)
        {
            startTime = Time.time;
            p.SpriteManager.SetAnimation("Fall");
        }

        public IState Execute(PlayerController p)
        {
            p.ApplyGravity();
            p.DetermineFacing();

            if (accelerationCurve != null)
            {
                var accel = p.Physics.Acceleration * accelerationCurve.Evaluate(Time.time - atTime);
                p.Move(p.Physics.Speed, accel);
            }
            else
            {
                p.Move();
            }
            
            // Wall Jumping / Attacking
            foreach (var input in p.Input)
            {
                if (input.Action == InputActions.AttackDown)
                {
                    return new AttackState();
                }

                if (input.Action == InputActions.JumpDown)
                {
                    if (p.SweepForWall(Vector2.right))
                        return PlayerController.WallJump(p, Vector2.left);
                    if (p.SweepForWall(Vector2.left))
                        return PlayerController.WallJump(p, Vector2.right);

                    // Coyote Time
                    if (p.StateMachine.PreviousState is RunState && !Utility.Elapsed(startTime, p.CoyoteTime))
                    {
                        return new JumpState();
                    }
                }

                p.Input.Release(input);
            }

            if (p.Surface.Grounded)
            {
                var moving = p.GetSurfaceProjectedVelocity().sqrMagnitude > 0.01f;
                p.SpriteManager.SetAnimation(moving ? "LandMoving" : "LandStationary");
                return new RunState();
            }

            if (p.Surface.ContactingWall)
            {
                // Wallslide.
                var i = p.GetSurfaceAlignedXInput();
                if (!p.AwayFromWall(i) && p.SweepForWall(-p.Surface.WallNormal))
                {
                    return new WallSlideState();
                }
            }

            if (p.Surface.Unstable)
            {
                return new FallSlideState();
            }

            return null;
        }

        public void Exit(PlayerController p) {}
    }
}