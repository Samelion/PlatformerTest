using Framework.Maths;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Handles all active contacts with surfaces.
    /// </summary>
    sealed class SurfaceManager
    {
        const float FloorThreshold = 0.1f;
        const float CeilThreshold = -0.1f;

        public int StableGroundContactCount;
        public Vector2 StableSurfaceNormal;
        public float StableSurfaceAngle;
        public float StableSlopeAngleThreshold;

        public int UnstableGroundContactCount;
        public Vector2 UnstableSurfaceNormal;
        public float UnstableSurfaceAngle;
        public float UnstableSlopeAngleThreshold;

        public Vector2 WallNormal;
        public int WallContactCount;

        /// <summary>
        /// Returns whether the character is grounded.
        /// Being grounded implies we have made at least one stable contact with ground we can stand on.
        /// </summary>
        public bool Grounded => StableGroundContactCount > 0;

        /// <summary>
        /// Returns whether the character is standing on unstable ground.
        /// If so, we have contacts with a surface below us that we cannot stand on. 
        /// </summary>
        public bool Unstable => !Grounded && UnstableGroundContactCount > 0;

        /// <summary>
        /// Are we contacting ground of some sort, be it stable or unstable?
        /// </summary>
        public bool ContactingSurface => Grounded || Unstable;

        /// <summary>
        /// Are we free of any ground contacts, stable or unstable?
        /// </summary>
        public bool Airborne => !Grounded && !Unstable;

        /// <summary>
        /// Are we contacting a wall?
        /// </summary>
        public bool ContactingWall => WallContactCount > 0;

        /// <summary>
        /// The normal of the current surface.
        /// If only unstable ground is being made, the normal of that contact will be returned.
        /// If in the air, this will return the world Up-Vector.
        /// </summary>
        public Vector2 SurfaceNormal =>
            Grounded ? StableSurfaceNormal : UnstableSurfaceNormal;

        /// <summary>
        /// The angle threshold of the current surface.
        /// If only unstable ground is being made, the threshold of that contact will be returned.
        /// </summary>
        public float SlopeAngleThreshold =>
            Grounded ? StableSlopeAngleThreshold : UnstableSlopeAngleThreshold;

        /// <summary>
        /// Angle of the current surface (relative to the world up vector).
        /// If only unstable ground is being made, the angle of that contact will be returned.
        /// </summary>
        public float SurfaceAngle =>
            Grounded ? StableSurfaceAngle : UnstableSurfaceAngle;

        /// <summary>
        /// Evalutates a collision for wall, floor and ceiling contacts.
        /// </summary>
        public void EvaluateCollision(PlayerController p, Collision2D collision)
        {
            for (var i = 0; i < collision.contactCount; i++)
            {
                var normal = collision.GetContact(i).normal;
                var surface = collision.gameObject.GetComponent<Surface>() ?? p.DefaultSurface;

                if (surface.IgnoreContacts)
                {
                    continue;
                }

                // Floor
                if (normal.y > FloorThreshold)
                {
                    var angle = p.GripToFallAngle(surface.Grip);
                    var threshold = Mathf.Cos(angle * Mathf.Deg2Rad);

                    if (normal.y >= threshold)
                    {
                        StableGroundContactCount++;
                        StableSurfaceNormal += normal;
                        StableSlopeAngleThreshold += angle;
                    }
                    else
                    {
                        UnstableGroundContactCount++;
                        UnstableSurfaceNormal += normal;
                        UnstableSlopeAngleThreshold += angle;
                    }

                    continue;
                }

                // Wall
                if (normal.y <= FloorThreshold && normal.y >= CeilThreshold)
                {
                    WallContactCount++;
                    WallNormal += normal;
                    continue;
                }

                // Ceiling
                //if (normal.y < CeilThreshold)
                //{
                //continue;
                //}
            }
        }

        /// <summary>
        /// Projects the given vector onto the surface.
        /// The output is not normalized.
        /// </summary>
        public Vector2 GetSurfaceProjectedVector(Vector2 vector) =>
            Utility.ProjectVectorOnPlane(SurfaceNormal, vector);

        /// <summary>
        /// Projects a vector onto the surface and rescales it to its pre-projection magnitude.
        /// </summary>
        public Vector2 GetSurfaceAlignedVector(Vector2 vector) =>
            Utility.ProjectVectorOnPlaneRescaled(SurfaceNormal, vector);

        /// <summary>
        /// Call at the start of each FixedUpdate to calculate contacts.
        /// </summary>
        public void Prime()
        {
            if (StableSurfaceNormal == Vector2.zero)
                StableSurfaceNormal = Vector2.up;
            else
                StableSurfaceNormal.Normalize();

            if (UnstableSurfaceNormal == Vector2.zero)
                UnstableSurfaceNormal = Vector2.up;
            else
                UnstableSurfaceNormal.Normalize();

            if (WallContactCount > 0)
                WallNormal.Normalize();

            StableSlopeAngleThreshold /= StableGroundContactCount;
            UnstableSlopeAngleThreshold /= UnstableGroundContactCount;

            StableSurfaceAngle = Vector2.Angle(StableSurfaceNormal, Vector2.up);
            UnstableSurfaceAngle = Vector2.Angle(UnstableSurfaceNormal, Vector2.up);
        }

        /// <summary>
        /// Call at the end of each FixedUpdate to clear residual contact information.
        /// </summary>
        public void Release()
        {
            StableGroundContactCount = 0;
            StableSurfaceNormal = Vector2.zero;
            StableSlopeAngleThreshold = 0f;
            StableSurfaceAngle = 0f;

            UnstableGroundContactCount = 0;
            UnstableSurfaceNormal = Vector2.zero;
            UnstableSlopeAngleThreshold = 0f;
            UnstableSurfaceAngle = 0f;

            WallContactCount = 0;
            WallNormal = Vector2.zero;
        }
    }
}
