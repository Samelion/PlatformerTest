using Assets.Scripts.Player;
using Assets.Scripts.StateMachine;
using UnityEngine;

sealed class WallSlideState : IState
{
    float detachHoldTime = 0f;

    public void Enter(PlayerController p)
    {
        p.SpriteManager.SetAnimation("WallSlide");
        p.SpriteManager.ResetAlignment();
    }

    public IState Execute(PlayerController p)
    {
        p.ApplyGravity();
        p.ApplyWallFriction();
        p.DetermineFacingOnWall();

        if (!p.Surface.ContactingWall || !p.SweepForWall(-p.Surface.WallNormal))
        {
            return new FallState();
        }

        if (p.Surface.Grounded)
        {
            p.SpriteManager.SetAnimation("LandStationary");
            return new RunState();
        }

        if (p.Surface.Unstable)
        {
            return new FallSlideState();
        }

        foreach (var input in p.Input)
        {
            if (input.Action == InputActions.JumpDown)
            {
                return PlayerController.WallJump(p, p.Surface.WallNormal);
            }

            p.Input.Release(input);
        }

        // We introduce a delay between holding away from the wall and detaching.
        // This is so players trying to walljump don't accidentally detach 
        // before hitting the jump button.
        var i = p.GetSurfaceAlignedXInput();
        if (p.AwayFromWall(i))
            detachHoldTime += Time.deltaTime;
        else
            detachHoldTime = 0f;

        if (detachHoldTime > p.WallDetachDelay)
        {
            return new FallState();
        }

        return null;
    }

    public void Exit(PlayerController p)
    {
    }
}
