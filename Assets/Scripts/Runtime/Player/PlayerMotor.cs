using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
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
        public float cameraPivot;
        public float cameraHeight;

        private new CapsuleCollider collider;
        private float lastJumpTimer;

        // --- Input ---
        public Vector3 moveDirection { get; set; }
        public bool jump { get; set; }
        public Vector2 rotation { get; set; }

        // --- State ---
        public Vector3 headPosition => interpolatedPosition + Vector3.up * cameraPivot + headRotation * Vector3.up * (cameraHeight - cameraPivot);
        public Vector3 rawHeadPosition => transform.position + Vector3.up * height;
        public Quaternion headRotation => Quaternion.Euler(-rotation.y, rotation.x, 0f);
        public Vector3 velocity { get; set; }
        public Vector3 interpolatedPosition
        {
            get
            {
                var transform = NetworkObject.GetGraphicalObject();
                if (transform == null) transform = this.transform;
                return transform.position;
            }
        }
        public bool onGround { get; private set; }

        private void Awake()
        {
            collider = new GameObject("Collision").AddComponent<CapsuleCollider>();
            collider.transform.SetParent(transform);
            collider.transform.SetLocalPositionAndRotation(Vector3.up * height / 2f, Quaternion.identity);

            collider.height = height;
            collider.radius = radius;
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
        }

        private void LateUpdate()
        {
            var rotation = this.rotation;
            Rotate(ref rotation);
            this.rotation = rotation;
        }

        private void Rotate(ref Vector2 rotation)
        {
            rotation.x %= 360f;
            rotation.y = Mathf.Clamp(rotation.y, -90f, 90f);

            transform.rotation = Quaternion.Euler(0f, rotation.x, 0f);
        }

        private void OnTick()
        {
            CreateReconcile();
            RunInputs(GetInputData());
        }

        private InputData GetInputData()
        {
            if (!IsOwner) return default;
            
            var data = new InputData(this);
            jump = false;
            return data;
        }

        [Replicate]
        private void RunInputs(InputData input, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (state.IsFuture()) return;
            
            CheckForGround();
            Move(input);
            Jump(input);
            Rotate(ref input.rotation);
            Iterate();
            Collide();
            
            if (!IsOwner) rotation = input.rotation;
        }

        public override void CreateReconcile()
        {
            var data = new StateData(this);
            Reconcile(data);
        }

        [Reconcile]
        private void Reconcile(StateData data, Channel channel = Channel.Unreliable)
        {
            transform.position = data.position;
            velocity = data.velocity;
            lastJumpTimer = data.lastJumpTimer;
        }

        private void Jump(InputData input)
        {
            if (input.jump && onGround)
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
            transform.position += velocity * Time.fixedDeltaTime;
            velocity += Physics.gravity * Time.fixedDeltaTime;
        }

        private void Move(InputData input)
        {
            var target = input.moveDirection * maxMoveSpeed;
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

        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (collider != null)
            {
                collider.height = height;
                collider.radius = radius;
                collider.transform.SetLocalPositionAndRotation(Vector3.up * height / 2f, Quaternion.identity);
            }
        }

        public struct InputData : IReplicateData
        {
            public Vector3 moveDirection;
            public bool jump;
            public Vector2 rotation;

            public InputData(PlayerMotor motor) : this()
            {
                moveDirection = motor.moveDirection;
                jump = motor.jump;
                rotation = motor.rotation;
            }

            private uint tick;
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }

        public struct StateData : IReconcileData
        {
            public Vector3 position;
            public Vector3 velocity;
            public float lastJumpTimer;

            public StateData(PlayerMotor motor) : this()
            {
                position = motor.transform.position;
                velocity = motor.velocity;
                lastJumpTimer = motor.lastJumpTimer;
            }

            private uint tick;
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }
    }
}