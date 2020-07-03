using System.Linq;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{
    public Animator Animator { get; private set; }

    private Transform spriteTransform;
    private SpriteRenderer spriteRenderer;

    public float Speed 
    { 
        get => Animator.speed; 
        set => Animator.speed = value; 
    }

    private void Awake()
    {
        spriteTransform = transform.Find("Sprite");
        spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        Animator = spriteTransform.GetComponent<Animator>();
    }

    public void SetAnimation(string name)
    {
        if (!Animator.HasState(0, Animator.StringToHash(name)))
        {
            Debug.LogError($"Animator does not contain state: {name}.");
            return;
        }

        Animator.Play(name);
    }

    /// <summary>
    /// Rotate the sprite to align with a given normal.
    /// </summary>
    public void AlignWith(Vector2 normal)
    {
        var angle = Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;
        spriteTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    /// <summary>
    /// Reset sprite rotation.
    /// </summary>
    public void ResetAlignment()
    {
        spriteTransform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Flip the sprite to face right or left.
    /// Sprites are expected to be right-facing by default.
    /// </summary>
    /// <param name="direction"></param>
    public void FaceTowards(Vector2 direction)
    {
        var rightness = Vector2.Dot(Vector2.right, direction);
        spriteRenderer.flipX = rightness < 0f;
    }

    /// <summary>
    /// Gets the length of the given animation.
    /// If no animation name is provided, will return the current animation length.
    /// </summary>
    public float GetAnimationLength(string name = null)
    {
        // TODO: Null-coalescing assignment operator in C#8
        if (name == null)
        {
            var state = GetAnimationState();
            return state.length;
        }

        // Unity makes it that the only way we can get an animation's length
        // (in a single frame) is by looping through them all. 
        // TODO: Do this on startup and collate into a dictionary, or something.
        return Animator
            .runtimeAnimatorController
            .animationClips
            .First(clip => clip.name == name)
            .length;
    }

    public AnimatorStateInfo GetAnimationState()
    {
        return Animator.GetCurrentAnimatorStateInfo(0);
    }
}
