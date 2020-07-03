using Assets.Scripts.StateMachine;
using Framework.Maths;
using UnityEngine;

namespace Assets.Scripts.Player
{
    enum CardinalDirections
    {
        Up,
        Down,
        Left,
        Right,
    }

    sealed class AttackState : IState
    {
        private float startTime;
        private float duration;
        private GameObject attackObject;

        public void Enter(PlayerController player)
        {
            var direction = player.Input.Movement;

            // If not giving any significant input
            // attack in direction of facing.
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = player.Facing;
            }

            var cardinal = direction.ToCardinalDirection();
            string animation;
            
            // TODO: Replace this with scriptableobject dictionary or something.
            switch (cardinal)
            {
                case Utility.Direction.Up:
                    attackObject = player.UpAttack;
                    animation = "AttackUp";
                    break;
                case Utility.Direction.Down:
                    attackObject = player.DownAttack;
                    animation = "AttackDown";
                    break;
                case Utility.Direction.Left:
                    attackObject = player.LeftAttack;
                    animation = "AttackSide";
                    break;
                case Utility.Direction.Right:
                    attackObject = player.RightAttack;
                    animation = "AttackSide";
                    break;
                default:
                    Debug.Log($"Unusable attack direction: {cardinal}");
                    return;
            }

            // If airborne, use the air version of the attack.
            if (player.Surface.Airborne)
            {
                animation += "Air";
            }
            // We can't attack down while grounded
            else if (cardinal == Utility.Direction.Down)
            {
                return;
            }

            attackObject.SetActive(true);
            player.SpriteManager.SetAnimation(animation);
            duration = player.SpriteManager.GetAnimationLength(animation);
            startTime = Time.time;
        }

        public IState Execute(PlayerController player)
        {
            player.ApplyGravity();

            if (Utility.Elapsed(startTime, duration))
            {
                return player.Surface.Grounded
                    ? (IState)new RunState()
                    : (IState)new FallState();
            }

            return null;
        }

        public void Exit(PlayerController p)
        {
            attackObject?.SetActive(false);
            p.SpriteManager.SetAnimation(p.Surface.Grounded ? "Run" : "Fall");
        }
    }
}
