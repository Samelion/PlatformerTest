using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Spit : MonoBehaviour
{
    public void Fire(Vector2 direction, float speed)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.AddForce(direction.normalized * speed, ForceMode2D.Impulse);
    }
}
