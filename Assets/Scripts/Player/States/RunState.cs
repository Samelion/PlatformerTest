using Assets.Scripts.StateMachine;
using UnityEngine;

namespace Assets.Scripts.Player
{
    sealed class RunState : IState
    {
        public void Enter(PlayerController p)
        {
            p.SetPhysicsProfile(p.PhysGrounded);
        }

        public IState Execute(PlayerController p)
        {
            p.Move();
            p.ApplyGravity();
            p.DetermineFacing();
            p.SpriteManager.AlignWith(p.Surface.SurfaceNormal);

            if (p.Surface.Airborne)
            {
                return new FallState();
            }

            if (p.Surface.Unstable)
            {
                // TODO: Make this an event or observable perhaps? 
                // So it isnt running every frame.
                var animation = p.SpriteManager.GetAnimationState();
                if (animation.IsName("Run"))
                {
                    p.SpriteManager.SetAnimation("RunDifficult");
                    p.SpriteManager.Speed = 1f;
                }

                var v = p.GetSurfaceProjectedVelocity();
                var i = p.Input.MovementHorizontal;

                // We slip and fall if...
                // We are on a slope above the slope angle threshold.
                // We aren't inputting any movement.
                // We are moving backward (relative to our input).
                var aboveSlopeThreshold = p.Surface.SurfaceAngle > p.Surface.SlopeAngleThreshold;
                var noInput = i.magnitude <= 0.01f;
                var movingBackwards = Vector2.Dot(v.normalized, i.normalized) < 0f;

                if (aboveSlopeThreshold)
                {
                    if (noInput || movingBackwards)
                    {
                        return new FallSlideState();
                    }
                }
            }
            else
            {
                var v = p.GetSurfaceProjectedVelocity();
                var i = p.GetSurfaceAlignedXInput();
                var movingIntentionally = i.sqrMagnitude > 0.01f && v.sqrMagnitude > 0.01f;
                var animation = p.SpriteManager.GetAnimationState();

                /// Select an animation based on what we are doing:

                // TODO: Make this an event or observable perhaps? 
                // So it isnt running every frame.
                if (animation.IsName("RunDifficult"))
                {
                    p.SpriteManager.SetAnimation("Run");
                }

                p.SpriteManager.Animator.SetBool("MovingIntentionally", movingIntentionally);
                p.SpriteManager.Animator.SetBool("Pushing", p.TowardWall(i));

                // Scale animation with relative speed.
                if (animation.IsName("Run"))
                {
                    var aligned = Vector2.Dot(v.normalized, i) > 0;
                    var speed = aligned ? v.magnitude : 0f;
                    p.SpriteManager.Speed = Mathf.Clamp01(speed);
                }
                else
                {
                    p.SpriteManager.Speed = 1f;
                }
            }

            foreach (var input in p.Input)
            {
                if (input.Action == InputActions.JumpDown)
                {
                    return new JumpState();
                }

                if (input.Action == InputActions.AttackDown)
                {
                    return new AttackState();
                }

                p.Input.Release(input);
            }

            return null;
        }

        public void Exit(PlayerController p)
        {
            p.SpriteManager.Speed = 1f;
            p.SpriteManager.ResetAlignment();
        }
    }
}
