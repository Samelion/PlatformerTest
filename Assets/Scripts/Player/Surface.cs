using UnityEngine;
#pragma warning disable 0649

namespace Assets.Scripts.Player
{
    enum GripType
    {
        Default,
        Slippery,
        Grippy,
        InstantSlide,
    }

    sealed class Surface : MonoBehaviour
    {
        /// <summary>
        /// Determines the angle at which the player will begin sliding when standing on this surface.
        /// </summary>
        public GripType Grip;

        /// <summary>
        /// If true, the player cannot make stable or unstable contact with this surface.
        /// Should probably only ever be used on walls.
        /// </summary>
        public bool IgnoreContacts;

        /// <summary>
        /// Prevents wall sliding against this surface.
        /// </summary>
        public bool DissalowWallSlide;
    }
}
