using Unity.Netcode;
using UnityEngine;

namespace Runtime
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

        private new CapsuleCollider collider;
        private Vector3 lastPosition;
        private float lastJumpTimer;

        // --- Input ---
        public Vector3 moveDirection { get; set; }
        public bool jump { get; set; }
        public Vector2 rotation { get; set; }

        // --- State ---
        public Vector3 headPosition => interpolatedPosition + Vector3.up * height;
        public Quaternion headRotation => Quaternion.Euler(-rotation.y, rotation.x, 0f);
        public Vector3 velocity { get; set; }
        public Vector3 interpolatedPosition { get; private set; }
        public bool onGround { get; private set; }

        private void Awake()
        {
            collider = new GameObject("Collision").AddComponent<CapsuleCollider>();
            collider.transform.SetParent(transform);
            collider.transform.SetLocalPositionAndRotation(Vector3.up * height / 2f, Quaternion.identity);

            collider.height = height;
            collider.radius = radius;
        }

        private void Update()
        {
            Rotate();
            interpolatedPosition = Vector3.Lerp(lastPosition, transform.position, (Time.time - Time.fixedTime) / Time.fixedDeltaTime);
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

            ResetInputs();

            if (IsOwner) SendInputDataServerRpc(new InputData(this));
            if (IsServer) SendStateDataClientRpc(new StateData(this));
        }


        [ClientRpc]
        private void SendStateDataClientRpc(StateData state)
        {
            transform.position = state.position;
            velocity = state.velocity;
            lastJumpTimer = state.lastJumpTimer;
            if (!IsOwner) rotation = state.rotation;
        }

        [ServerRpc]
        private void SendInputDataServerRpc(InputData input)
        {
            moveDirection = input.moveDirection;
            jump = input.jump;
            rotation = input.rotation;
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

            lastJumpTimer += Time.deltaTime;
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

        private void Move()
        {
            var target = moveDirection * maxMoveSpeed;
            var velocity = this.velocity;

            var acceleration = maxMoveSpeed * Time.deltaTime / moveAccelerationTime;
            if (!onGround) acceleration *= 1f - moveAccelerationAirPenalty;
            velocity = Vector3.MoveTowards(velocity, target, acceleration);

            this.velocity = new Vector3()
            {
                x = velocity.x,
                y = this.velocity.y,
                z = velocity.z,
            };
        }

        private void ResetInputs() { jump = false; }

        private void OnValidate()
        {
            if (collider != null)
            {
                collider.height = height;
                collider.radius = radius;
                collider.transform.SetLocalPositionAndRotation(Vector3.up * height / 2f, Quaternion.identity);
            }
        }

        public struct InputData : INetworkSerializeByMemcpy
        {
            public Vector3 moveDirection;
            public bool jump;
            public Vector2 rotation;

            public InputData(PlayerMotor motor)
            {
                moveDirection = motor.moveDirection;
                jump = motor.jump;
                rotation = motor.rotation;
            }
        }

        public struct StateData : INetworkSerializeByMemcpy
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector2 rotation;
            public float lastJumpTimer;

            public StateData(PlayerMotor motor)
            {
                position = motor.transform.position;
                velocity = motor.velocity;
                rotation = motor.rotation;
                lastJumpTimer = motor.lastJumpTimer;
            }
        }
    }
}