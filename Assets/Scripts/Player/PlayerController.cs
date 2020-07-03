#pragma warning disable 0649

using Assets.Framework.Maths;
using Assets.Scripts.StateMachine;
using Framework.Maths;
using System;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(PlayerInputProvider))]
    sealed class PlayerController : MonoBehaviour, IActor
    {
        public readonly SurfaceManager Surface = new SurfaceManager();

        private new Rigidbody2D rigidbody;

        [SerializeField] float AttackKnockback;

        [Header("Jump Physics")]
        public float JumpForce;
        public float JumpExtendTime;
        public float GetupJumpForce;
        public float SlideToFallDelay;
        public float CoyoteTime;
        public float JumpCapVelocityRatio;

        [Header("Wall Jump Physics")]
        public float WallJumpForce;
        public float WallFriction;
        public float WallSlideSpeed;
        public float WallDetachDelay;
        public AnimationCurve WallJumpAcclerationCurve;
        [SerializeField] float wallTestVerticalOffset;
        [SerializeField] float wallTestSpan;
        [SerializeField] float wallTestLength;
        [SerializeField] int wallTestPasses;
        [SerializeField] float TowardDotThreshold;

        [Serializable]
        public sealed class PhysicsProfile
        {
            public float Gravity;
            public float FallSpeed;
            public float Acceleration;
            public float Speed;
            public PhysicsMaterial2D PhysicsMaterial;
        }

        [Header("Physics Profiles")]
        public PhysicsProfile PhysGrounded = new PhysicsProfile();
        public PhysicsProfile PhysJumpStandard = new PhysicsProfile();
        public PhysicsProfile PhysJumpGetup = new PhysicsProfile();

        [Header("Slopes")]
        public Surface DefaultSurface;
        [SerializeField, Range(0, 90)] float defaultSlopeAngle = 30f;
        [SerializeField, Range(0, 90)] float slipperySlopeAngle = 20f;
        [SerializeField, Range(0, 90)] float grippySlopeAngle = 45f;
        [SerializeField] float slopeFactor;

        public PlayerStateMachine StateMachine { get; private set; }

        public SpriteManager SpriteManager { get; private set; }

        public GameObject LeftAttack { get; private set; }

        public GameObject RightAttack { get; private set; }

        public GameObject UpAttack { get; private set; }

        public GameObject DownAttack { get; private set; }

        public PhysicsProfile Physics { get; private set; }

        public PlayerInputProvider Input { get; private set; }

        public Vector2 Facing { get; private set; }

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody2D>();
            Input = GetComponent<PlayerInputProvider>();
            SpriteManager = GetComponent<SpriteManager>();

            LeftAttack = transform.Find("LeftAttack").gameObject;
            RightAttack = transform.Find("RightAttack").gameObject;
            UpAttack = transform.Find("UpAttack").gameObject;
            DownAttack = transform.Find("DownAttack").gameObject;

            StateMachine = new PlayerStateMachine(this, new RunState());

            SetPhysicsProfile(PhysJumpStandard);
        }

        void FixedUpdate()
        {
            Surface.Prime();
            Input.Prime();
            
            StateMachine.Tick();

            Surface.Release();
        }

        #region Motor

        public Vector2 Position => transform.position;

        public Vector2 Velocity => rigidbody.velocity;

        public bool Falling => Surface.Airborne && Velocity.y < 0;

        public Vector2 GetSurfaceAlignedXInput()
        {
            var xInput = Input.MovementHorizontal;
            var surface = Surface.SurfaceNormal;

            return Utility.ProjectVectorOnPlaneRescaled(surface, xInput);
        }

        public Vector2 GetSurfaceProjectedVelocity()
        {
            return Utility.ProjectVectorOnPlane(Surface.SurfaceNormal, Velocity);
        }

        public void SetPhysicsProfile(PhysicsProfile physicsProfile)
        {
            Physics = physicsProfile;
            rigidbody.sharedMaterial = physicsProfile.PhysicsMaterial;
        }

        public void Jump(Vector2 direction, float force)
        {
            var jump = direction * force;
            var antiGrav = Mathf.Max(-Velocity.y, 0f) * Vector2.up;

            var impulse = jump + antiGrav;
            rigidbody.AddForce(impulse, ForceMode2D.Impulse);
        }

        public void CapJumpIfRising()
        {
            if (Falling)
            {
                return;
            }
            
            var force = Vector2.down * Velocity.y * JumpCapVelocityRatio;
            rigidbody.AddForce(force, ForceMode2D.Impulse);
        }

        public void Move(float speed, float acceleration)
        {
            var i = GetSurfaceAlignedXInput();
            var v = GetSurfaceProjectedVelocity();

            var desired = i * speed;
            var delta = desired - v;
            var accel = acceleration * Time.deltaTime;

            var movement = Vector2.MoveTowards(Vector2.zero, delta, accel);
            var slope = ApplySlopeAccel();

            var force = movement + slope;
            rigidbody.AddForce(force, ForceMode2D.Impulse);
        }

        public void Move()
        {
            var i = GetSurfaceAlignedXInput();
            var v = GetSurfaceProjectedVelocity();

            var desired = i * Physics.Speed;
            var delta = desired - v;
            var accel = Physics.Acceleration * Time.deltaTime;

            var movement = Vector2.MoveTowards(Vector2.zero, delta, accel);
            var slope = ApplySlopeAccel();

            var force = movement + slope;
            rigidbody.AddForce(force, ForceMode2D.Impulse);
        }

        public void DetermineFacing()
        {
            var i = GetSurfaceAlignedXInput();
            var v = GetSurfaceProjectedVelocity();

            if (v.sqrMagnitude < 0.01f || i.sqrMagnitude < 0.01f)
            {
                return;
            }

            // If input and velocity are not aligned, don't turn around.
            var aligned = Vector2.Dot(i.normalized, v.normalized) <= 0f;
            if (aligned)
            {
                return;
            }

            // Face the left/right direction of movement
            var rightness = Vector2.Dot(v.normalized, Vector2.right) > 0;
            Facing = rightness ? Vector2.right : Vector2.left;
            SpriteManager.FaceTowards(Facing);
        }

        public void DetermineFacingOnWall()
        {
            if (!Surface.ContactingWall)
            {
                return;
            }

            SpriteManager.FaceTowards(Surface.WallNormal);
        }

        public void ApplyGravity()
        {
            var gravity = Physics.Gravity;
            var fallSpeed = Physics.FallSpeed;

            var desired = Vector2.down * fallSpeed;
            var delta = desired - Velocity._0y();
            var acceleration = gravity * Time.deltaTime;

            var force = Vector2.MoveTowards(Vector2.zero, delta, acceleration);
            rigidbody.AddForce(force, ForceMode2D.Impulse);
        }

        public void ApplyWallFriction()
        {
            var desired = Vector2.down * WallSlideSpeed;
            var wallMagnetism = -Surface.WallNormal * 1f;

            var delta = (desired + wallMagnetism) - Velocity._0y();
            var acceleration = WallFriction * Time.deltaTime;

            var force = Vector2.MoveTowards(Vector2.zero, delta, acceleration);
            rigidbody.AddForce(force, ForceMode2D.Impulse);
        }

        public bool SweepForWall(Vector2 direction)
        {
            var offset = Position + Vector2.up * wallTestVerticalOffset;
            var span   = Vector2.up * wallTestSpan;
            var step   = Vector2.down * (wallTestSpan * 2f / wallTestPasses);

            for (var i = 0; i < wallTestPasses; i++)
            {
                var origin = offset + span + step * i;
                var result = Physics2D.Raycast(origin, direction, wallTestLength);

                if (result.collider == null)
                {
                    return false;
                }
                else
                {
                    var surf = result.collider.GetComponent<Surface>();
                    if (surf && surf.DissalowWallSlide)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TowardWall(Vector2 v)
        {
            if (!Surface.ContactingWall || v.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            var align = Vector2.Dot(v.normalized, Surface.WallNormal);
            return align < -TowardDotThreshold;
        }

        public bool AwayFromWall(Vector2 v)
        {
            if (!Surface.ContactingWall || v.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            var align = Vector2.Dot(v.normalized, Surface.WallNormal);
            return align > TowardDotThreshold;
        }

        public float GripToFallAngle(GripType type)
        {
            switch (type)
            {
                default:
                    return defaultSlopeAngle;
                case GripType.Slippery:
                    return slipperySlopeAngle;
                case GripType.Grippy:
                    return grippySlopeAngle;
                case GripType.InstantSlide:
                    return 90f;
            }
        }

        private Vector2 ApplySlopeAccel()
        {
            var onSlope = !Surface.Airborne && Surface.SurfaceAngle > 0f;

            if (onSlope)
            {
                var n = Surface.SurfaceNormal;
                var angle = Surface.SurfaceAngle;
                var slope = angle / Surface.SlopeAngleThreshold;
                var accel = slopeFactor * slope;

                // TODO: Figure out how to do this in 2D.
                var slopeSideways = Vector3.Cross(Vector3.down, n);
                var righthand = Vector3.Cross(n, slopeSideways);

                var force = righthand * accel * Time.deltaTime;

                Debug.DrawRay(Position, force, Color.red);
                return force;
            }

            return Vector2.zero;
        }

        private void OnCollisionEnter2D(Collision2D collision) => Surface.EvaluateCollision(this, collision);

        private void OnCollisionStay2D(Collision2D collision) => Surface.EvaluateCollision(this, collision);

        #endregion

        #region Static Helper Functions

        public static IState WallJump(PlayerController p, Vector2 dir)
        {
            p.SpriteManager.SetAnimation("WallJump");
            p.SpriteManager.FaceTowards(dir);
            p.CapJumpIfRising();

            var direction = (Vector2.up + dir).normalized;

            return new JumpState(direction, p.WallJumpForce)
                .WithAccelerationCurve(p.WallJumpAcclerationCurve);
        }

        #endregion
    }
}