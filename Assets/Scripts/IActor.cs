using UnityEngine;

namespace Assets.Scripts
{
    interface IActor
    {
        Vector2 Position { get; }

        Vector2 Velocity { get; }
    }
}
