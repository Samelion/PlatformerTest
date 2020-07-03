using Assets.Scripts.StateMachine;
using Framework.Maths;
using UnityEngine;

namespace Assets.Scripts.Player
{
    class GetupStandState : IState
    {
        const string Animation = "Getup";

        private float startTime;
        private float duration; 

        public void Enter(PlayerController p)
        {
            startTime = Time.time;
            p.SpriteManager.SetAnimation(Animation);
            duration = p.SpriteManager.GetAnimationLength(Animation);

            p.SetPhysicsProfile(p.PhysGrounded);
        }

        public IState Execute(PlayerController p)
        {
            p.ApplyGravity();
            p.SpriteManager.AlignWith(p.Surface.SurfaceNormal);

            if (p.Falling)
            {
                return new FallState();
            }

            if (p.Surface.Unstable)
            {
                return new FallSlideState();
            }

            if (Utility.Elapsed(startTime, duration))
            {
                p.SpriteManager.SetAnimation("Stand");
                return new RunState();
            }

            return null;
        }

        public void Exit(PlayerController p)
        {
            p.SpriteManager.ResetAlignment();
        }
    }

    class GetupJumpState : IState
    {
        public void Enter(PlayerController p)
        {
            p.SetPhysicsProfile(p.PhysJumpGetup);
            p.Jump(p.Surface.SurfaceNormal, p.JumpForce);

            p.SpriteManager.SetAnimation("Jump");
        }

        public IState Execute(PlayerController p)
        {
            p.Move();
            p.ApplyGravity();

            if (p.Falling)
            {
                p.SetPhysicsProfile(p.PhysJumpGetup);
                return new FallState();
            }

            // Only re-slide if moving down, otherwise we
            // risk regrounding immediately after jumping.
            if (p.Surface.Unstable && p.Velocity.y < 0)
            {
                return new FallSlideState();
            }

            return null;
        }

        public void Exit(PlayerController p) {}
    }
}
