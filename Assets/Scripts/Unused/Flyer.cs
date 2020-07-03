#pragma warning disable 0649

using Assets.Framework.Maths;
using Assets.Scripts;
using Framework.Maths;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Flyer : MonoBehaviour, IActor
{
    [SerializeField] Transform target;
    [SerializeField] float desiredDistance;

    [SerializeField] float maxSpeed = 1f;
    [SerializeField] float maxForce = 10f;

    [SerializeField] float wanderRadius;
    [SerializeField] float angleChangeSpeed;
    [SerializeField] float floatHeightOffset;

    [SerializeField] GameObject spitPrefab;
    [SerializeField] float spitForce;

    new Rigidbody2D rigidbody;
    Animator animator;
    Transform spitParticleTransform;
    ParticleSystem spitParticleSystem;
    bool facingRight;
    float wanderAngle;

    public Vector2 Position => transform.position;

    public Vector2 Velocity => rigidbody.velocity;

    public void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spitParticleTransform = transform.Find("Spit Particle");
        spitParticleSystem = spitParticleTransform.GetComponent<ParticleSystem>();
    }

    public void Update()
    {
        var origin = transform.position;
        var target = this.target.position;

        // We are aiming to hover at apoint slightly above 
        // the target's y position.
        var t = (target - origin);
        var y = (t.y * 0.5f) - floatHeightOffset;
        t = t.WithY(y);

        Debug.DrawRay(Position, t);

        var toTarg = t.normalized;
        var attackPosition = toTarg * desiredDistance;

        var wanderDisplacement = (Vector2.right * wanderRadius).Rotate(wanderAngle);
            //RotateVector(Vector2.right * wanderRadius, wanderAngle);
        wanderAngle += (Random.value * angleChangeSpeed) - (angleChangeSpeed * .5f);
        var dest = target - attackPosition + wanderDisplacement.xy0();

        var toDest = dest - origin;
        var desiredVelocity = (toDest.normalized * maxSpeed).xy();

        var steering = desiredVelocity - rigidbody.velocity;
        steering = Vector3.ClampMagnitude(steering, maxForce);

        rigidbody.AddForce(steering);

        if ((facingRight && target.x < origin.x) ||
           (!facingRight && target.x > origin.x))
        {
            animator.SetTrigger("TurnAround");
        }
    }

    public void Turn()
    {
        facingRight = !facingRight;

        var currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;

        animator.ResetTrigger("TurnAround");
    }

    public void Spit()
    {
        var origin = transform.position;
        var target = this.target.position;
        var toTarget = target - origin;

        var spit = Instantiate(spitPrefab, transform.position, Quaternion.identity)
            .GetComponent<Spit>();
        spit.Fire(toTarget, spitForce);

        var angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        spitParticleTransform.rotation = Quaternion.Euler(0, 0, angle - 15f);
        spitParticleSystem.Play();
    }

    public void Attack(Attack a)
    {
        rigidbody.AddForce(a.Knockback, ForceMode2D.Impulse);

        // TODO: Take damage, make particles, and die.
        StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        // Enable gravity on rigidbody.
        rigidbody.gravityScale = 1f;

        var collider = GetComponent<CircleCollider2D>();
        collider.radius /= 4f;

        animator.Play("Fly_Death");

        // We have to wait one frame in order
        // for the death animation to kick in.
        yield return 0;

        Destroy(this);
        Destroy(animator);
    }
}
