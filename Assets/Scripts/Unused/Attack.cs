using UnityEngine;

namespace Assets.Scripts
{
    public struct Attack
    {
        public readonly Vector2 Knockback;
        public readonly float Damage;

        public Attack(Vector2 knockback, float damage)
        {
            Knockback = knockback;
            Damage = damage;
        }
    }
}
