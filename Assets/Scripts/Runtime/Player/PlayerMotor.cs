using Unity.Netcode;
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
        public float crouchSpeedMulti = 0.6f;
        public float crouchHeight = 1.2f;

        [Space]
        public float jumpHeight = 2f;
        public float gravityScale = 2f;

        [Space]
        public float height = 1.8f;
        public float radius = 0.3f;
        public float bottomGap = 0.3f;
        public float cameraPivot;
        public float cameraHeight;

        private new CapsuleCollider collider;
        private float lastJumpTimer;
        private Vector3 lastPosition;
        private float crouchPercent;

        public Vector3 velocity { get; set; }
        public Vector3 gravity => Physics.gravity * gravityScale;
        public Vector3 headPosition
        {
            get
            {
                var height = cameraHeight;
                height = Mathf.Lerp(height, crouchHeight - (this.height - cameraHeight), crouchPercent);
                
                var pivot = cameraHeight - cameraPivot;
                return lerpPosition + Vector3.up * (height - pivot) + headRotation * Vector3.up * pivot;
            }
        }
        public Vector3 rawHeadPosition => transform.position + Vector3.up * cameraPivot + headRotation * Vector3.up * (cameraHeight - cameraPivot);
        public Quaternion headRotation => Quaternion.Euler(-rotation.y, rotation.x, 0f);
        public Vector3 lerpPosition => Vector3.Lerp(lastPosition, transform.position, (Time.time - Time.fixedTime) / Time.fixedDeltaTime);

        // --- Input ---
        public Vector3 moveDirection { get; set; }
        public bool jump { get; set; }
        public Vector2 rotation { get; set; }
        public bool crouching { get; set; }

        // --- State ---
        public bool onGround { get; private set; }

        private void Awake()
        {
            collider = new GameObject("Collision").AddComponent<CapsuleCollider>();
            collider.transform.SetParent(transform);
        }

        private void Update() { Rotate(); }

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
            Crouch();
            Jump();
            UpdateCollider();
            Rotate();
            Iterate();
            Collide();

            if (IsOwner)
            {
                if (!IsServer) SendNetStateServerRpc(GetNetState());
                else SendNetStateRpc(GetNetState());
            }
        }

        private void Crouch()
        {
            crouchPercent = Mathf.MoveTowards(crouchPercent, crouching ? 1f : 0f, Time.deltaTime / 0.1f);
        }

        private void UpdateCollider()
        {
            var height = this.height;
            height = Mathf.Lerp(height, crouchHeight, crouchPercent);

            collider.transform.SetLocalPositionAndRotation(Vector3.up * (height + bottomGap) / 2f, Quaternion.identity);
            collider.height = height - bottomGap;
            collider.radius = radius;
        }

        private void Jump()
        {
            if (jump && onGround)
            {
                velocity = new Vector3
                {
                    x = velocity.x,
                    z = velocity.z,
                    y = Mathf.Sqrt(2f * -gravity.y * jumpHeight)
                };
                lastJumpTimer = 0f;
            }

            lastJumpTimer += Time.fixedDeltaTime;
        }

        private void CheckForGround()
        {
            onGround = false;
            if (lastJumpTimer > 0.1f)
            {
                var rayLength = height / 2f;
                var ray = new Ray(transform.position + Vector3.up * rayLength, Vector3.down);
                rayLength *= 1.02f;
                onGround = false;
                if (Physics.SphereCast(ray, radius * 0.8f, out var hit, rayLength) && Mathf.Abs(ray.origin.y - hit.point.y) <= rayLength)
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
        }

        private void Move()
        {
            var moveSpeed = maxMoveSpeed;
            moveSpeed = Mathf.Lerp(moveSpeed, moveSpeed * crouchSpeedMulti, crouchPercent);

            moveDirection = Vector3.ClampMagnitude(new Vector3(moveDirection.x, 0f, moveDirection.z), 1f);
            var target = moveDirection * moveSpeed;
            var velocity = this.velocity;

            var acceleration = moveSpeed * Time.fixedDeltaTime / moveAccelerationTime;
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
            velocity += gravity * Time.deltaTime;
        }

        [ServerRpc]
        private void SendNetStateServerRpc(NetState netState)
        {
            if (!IsOwner) ApplyNetState(netState);
            SendNetStateRpc(netState);
        }

        [Rpc(SendTo.Everyone)]
        private void SendNetStateRpc(NetState netState)
        {
            if (!IsOwner) ApplyNetState(netState);
        }

        private NetState GetNetState()
        {
            return new NetState()
            {
                position = transform.position,
                velocity = velocity,
                rotation = rotation,
                crouching = crouching,
            };
        }

        private void ApplyNetState(NetState state)
        {
            transform.position = state.position;
            velocity = state.velocity;
            rotation = state.rotation;
            crouching = state.crouching;
        }

        public struct NetState : INetworkSerializeByMemcpy
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector2 rotation;
            public bool crouching;
        }
    }
}