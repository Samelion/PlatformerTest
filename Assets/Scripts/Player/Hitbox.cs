using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Collider2D))]
    class Hitbox : MonoBehaviour
    {
        [SerializeField] private float hitForce;

        private HashSet<Transform> hits = new HashSet<Transform>();
        private Transform owner;
        private new Collider2D collider;

        private void OnEnable()
        {
            owner = transform.root;
            collider = GetComponent<Collider2D>();
            hits.Clear();

            TestOverlaps();
        }

        private void OnDisable()
        {
            hits.Clear();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandleOverlap(collision);
        }
        
        private void TestOverlaps()
        {
            var filter = new ContactFilter2D();
            var colliders = new Collider2D[10];
            var overlaps = Physics2D.OverlapCollider(collider, filter, colliders);

            for (var i = 0; i < overlaps; i++)
            {
                HandleOverlap(colliders[i]);
            }
        }

        private void HandleOverlap(Collider2D col)
        {
            var hit = col.gameObject.transform;
            if (hit == owner || hits.Contains(hit))
            {
                return;
            }

            hits.Add(hit);

            if (hit.TryGetComponent<Rigidbody2D>(out var rb))
            {
                var fromOwnerPosition = hit.position - owner.position;
                var force = fromOwnerPosition.normalized * hitForce;
                var point = col.ClosestPoint(transform.position);
                rb.AddForceAtPosition(force, point, ForceMode2D.Impulse);
            }
        }
    }
}
