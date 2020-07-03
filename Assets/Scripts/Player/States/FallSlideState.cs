using Assets.Scripts.Player;
using Assets.Scripts.StateMachine;
using Framework.Maths;
using UnityEngine;

class FallSlideState : IState
{
    private const float StandThreshold = 0.15f;
    private const float JumpWaitTime = 0.5f;
    
    private float startTime;
    private float timeOfLastGround;

    public void Enter(PlayerController p)
    {
        startTime = Time.time;  
        p.SpriteManager.SetAnimation("Slide");
        p.SetPhysicsProfile(p.PhysGrounded);
    }

    public IState Execute(PlayerController p)
    {
        p.ApplyGravity();

        if (p.Surface.ContactingSurface)
        {
            p.SpriteManager.AlignWith(p.Surface.SurfaceNormal);
            timeOfLastGround = Time.time;
        }

        // If aren't moving relative to the surface, we stand up.
        var notMoving = p.GetSurfaceProjectedVelocity().magnitude <= StandThreshold;
        Debug.Log($"NotMoving: {notMoving}, Mag: {p.GetSurfaceProjectedVelocity().magnitude}.");
        if (notMoving && p.Surface.Grounded)
        {
            return new GetupStandState();
        }

        var outsideWaitWindow = Utility.Elapsed(startTime, JumpWaitTime);
        if (outsideWaitWindow)
        {
            // Once outside the wait window, we can try jumping.
            foreach (var input in p.Input)
            {
                if (input.Action == InputActions.JumpDown)
                {
                    return new GetupJumpState();
                }

                p.Input.Release(input);
            }
        }

        // If airborne, and have been for [SlideToFallDelay] seconds, start falling.
        // Adding this small delay prevents state 'flickering' on steep surfaces.
        if (p.Surface.Airborne && Utility.Elapsed(timeOfLastGround, p.SlideToFallDelay))
        {
            return new FallState();
        }

        return null;
    }

    public void Exit(PlayerController p) {}
}
