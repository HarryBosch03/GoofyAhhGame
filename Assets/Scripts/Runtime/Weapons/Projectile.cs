using System;
using UnityEngine;

namespace Runtime.Weapons
{
    public class Projectile : MonoBehaviour
    {
        public float gravityScale = 1f;
        public float maxDistance = 100f;
        public float lerpDistance = 0.1f;
        public TrailRenderer trail;
        
        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public Vector3 velocity;
        [HideInInspector]
        public Vector3 displacement;

        private float distanceTraveled;

        public Vector3? interpolatePosition;

        public event Action<Projectile, RaycastHit> HitEvent;

        private void Awake()
        {
            position = transform.position;
        }

        private void Start()
        {
            if (interpolatePosition.HasValue) transform.position = interpolatePosition.Value;
            if (trail != null) trail.AddPosition(interpolatePosition ?? position);
        }

        private void FixedUpdate()
        {
            var ray = new Ray(position, velocity);
            if (Physics.Raycast(ray, out var hit, velocity.magnitude * Time.fixedDeltaTime * 1.02f))
            {
                HitEvent?.Invoke(this, hit);
                if (trail != null)
                {
                    trail.transform.SetParent(null);
                    Destroy(trail.gameObject, trail.time + 1f);
                }
                Destroy(gameObject);
            }

            displacement += velocity * Time.fixedDeltaTime;
            position += velocity * Time.fixedDeltaTime;
            velocity += Physics.gravity * gravityScale * Time.fixedDeltaTime;


            distanceTraveled += velocity.magnitude * Time.fixedDeltaTime;
            if (distanceTraveled > maxDistance) Destroy(gameObject);
        }

        private void LateUpdate()
        {
            transform.position = interpolatePosition.HasValue ? Vector3.Lerp(interpolatePosition.Value + displacement, position, distanceTraveled / lerpDistance) : position;
            transform.position += velocity * (Time.time - Time.fixedTime);
        }
    }
}