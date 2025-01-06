using PurrNet;
using UnityEngine;

namespace Runtime.Player
{
    public class PlayerMotor : NetworkBehaviour
    {
        public float maxMoveSpeed;
        public float moveAccelerationTime;
        [Range(0f, 1f)]
        public float moveAccelerationAirPenalty;

        [Space]
        public float jumpHeight = 2f;

        [Space]
        public float height;
        public float radius;
        public float bottomGap = 0.3f;
        public float cameraPivot;
        public float cameraHeight;

        private new CapsuleCollider collider;
        private float lastJumpTimer;
        private Vector3 lastPosition;

        public Vector3 velocity { get; set; }
        public Vector3 headPosition => lerpPosition + Vector3.up * cameraPivot + headRotation * Vector3.up * (cameraHeight - cameraPivot);
        public Vector3 rawHeadPosition => transform.position + Vector3.up * cameraPivot + headRotation * Vector3.up * (cameraHeight - cameraPivot);
        public Quaternion headRotation => Quaternion.Euler(-rotation.y, rotation.x, 0f);
        public Vector3 lerpPosition => Vector3.Lerp(lastPosition, transform.position, (Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        
        // --- Input ---
        public Vector3 moveDirection { get; set; }
        public bool jump { get; set; }
        public Vector2 rotation { get; set; }

        // --- State ---
        public bool onGround { get; private set; }

        private void Awake()
        {
            collider = new GameObject("Collision").AddComponent<CapsuleCollider>();
            collider.transform.SetParent(transform);
            collider.transform.SetLocalPositionAndRotation(Vector3.up * (height + bottomGap) / 2f, Quaternion.identity);

            collider.height = height - bottomGap;
            collider.radius = radius;
        }

        private void LateUpdate()
        {
            Rotate();
        }

        private void Rotate()
        {
            var rotation = this.rotation;
            
            rotation.x %= 360f;
            rotation.y = Mathf.Clamp(rotation.y, -90f, 90f);

            transform.rotation = Quaternion.Euler(0f, rotation.x, 0f);

            this.rotation = rotation;
        }

        private void FixedUpdate()
        {
            lastPosition = transform.position;
            
            CheckForGround();
            Move();
            Jump();
            Rotate();
            Iterate();
            Collide();
            
            if (isOwner)
            {
                if (!isServer) SendNetStateServer(GetNetState());
                else SendNetStateClients(GetNetState());
            }
        }

        private void Jump()
        {
            if (jump && onGround)
            {
                velocity = new Vector3
                {
                    x = velocity.x,
                    z = velocity.z,
                    y = Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight)
                };
                lastJumpTimer = 0f;
            }

            lastJumpTimer += Time.fixedDeltaTime;
        }

        private void CheckForGround()
        {
            if (lastJumpTimer < 0.1f)
            {
                onGround = false;
                return;
            }

            var rayLength = height / 2f;
            var ray = new Ray(transform.position + Vector3.up * rayLength, Vector3.down);
            rayLength *= 1.02f;
            onGround = false;
            if (Physics.SphereCast(ray, radius, out var hit, rayLength) && Mathf.Abs(ray.origin.y - hit.point.y) <= rayLength)
            {
                transform.position = new Vector3()
                {
                    x = transform.position.x,
                    y = hit.point.y,
                    z = transform.position.z,
                };
                velocity = new Vector3()
                {
                    x = velocity.x,
                    y = Mathf.Max(0f, velocity.y),
                    z = velocity.z,
                };
                onGround = true;
            }
        }

        private void Move()
        {
            moveDirection = Vector3.ClampMagnitude(new Vector3(moveDirection.x, 0f, moveDirection.z), 1f);
            var target = moveDirection * maxMoveSpeed;
            var velocity = this.velocity;

            var acceleration = maxMoveSpeed * Time.fixedDeltaTime / moveAccelerationTime;
            if (!onGround) acceleration *= 1f - moveAccelerationAirPenalty;
            velocity = Vector3.MoveTowards(velocity, target, acceleration);

            this.velocity = new Vector3()
            {
                x = velocity.x,
                y = this.velocity.y,
                z = velocity.z,
            };
        }
        
        private void Collide()
        {
            var board = Physics.OverlapBox(collider.bounds.center, collider.bounds.extents);
            foreach (var other in board)
            {
                if (other.transform.IsChildOf(transform)) continue;
                if (other.isTrigger) continue;

                if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, other, other.transform.position, other.transform.rotation, out var normal, out var depth))
                {
                    transform.position += normal * depth;
                    velocity += normal * Mathf.Max(0f, Vector3.Dot(normal, -velocity));
                }
            }
        }

        private void Iterate()
        {
            transform.position += velocity * Time.deltaTime;
            velocity += Physics.gravity * Time.deltaTime;
        }

        private void OnValidate()
        {
            if (collider != null)
            {
                collider.height = height;
                collider.radius = radius;
                collider.transform.SetLocalPositionAndRotation(Vector3.up * height / 2f, Quaternion.identity);
            }
        }
        
        [ServerRpc]
        private void SendNetStateServer(NetState netState)
        {
            if (!isOwner) ApplyNetState(netState);
            SendNetStateClients(netState);
        }

        [ObserversRpc]
        private void SendNetStateClients(NetState netState)
        {
            if (!isOwner) ApplyNetState(netState);
        }
        
        private NetState GetNetState()
        {
            return new NetState()
            {
                position = transform.position,
                velocity = velocity,
                rotation = rotation,
            };
        }

        private void ApplyNetState(NetState state)
        {
            transform.position = state.position;
            velocity = state.velocity;
            rotation = state.rotation;
        }

        public struct NetState
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector2 rotation;
        }
    }
}