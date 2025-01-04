using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

namespace Runtime.Player
{
    [RequireComponent(typeof(Rigidbody))]
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

        public Rigidbody body { get; private set; }
        public Vector3 headPosition => interpolatedPosition + Vector3.up * cameraPivot + headRotation * Vector3.up * (cameraHeight - cameraPivot);
        public Vector3 rawHeadPosition => transform.position + Vector3.up * height;
        public Quaternion headRotation => Quaternion.Euler(-rotation.y, rotation.x, 0f);
        public Vector3 interpolatedPosition
        {
            get
            {
                var transform = NetworkObject.GetGraphicalObject();
                if (transform == null) transform = this.transform;
                return transform.position;
            }
        }
        
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

            body = GetComponent<Rigidbody>();
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
            TimeManager.OnPostTick += OnPostTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnPostTick()
        {
            CreateReconcile();
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
            CheckForGround();
            Move(input);
            Jump(input);
            Rotate(ref input.rotation);
            
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
            body.linearVelocity = data.velocity;
            lastJumpTimer = data.lastJumpTimer;
        }

        private void Jump(InputData input)
        {
            if (input.jump && onGround)
            {
                body.linearVelocity = new Vector3
                {
                    x = body.linearVelocity.x,
                    z = body.linearVelocity.z,
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
                body.linearVelocity = new Vector3()
                {
                    x = body.linearVelocity.x,
                    y = Mathf.Max(0f, body.linearVelocity.y),
                    z = body.linearVelocity.z,
                };
                onGround = true;
            }
        }

        private void Move(InputData input)
        {
            var target = input.moveDirection * maxMoveSpeed;
            var velocity = body.linearVelocity;

            var acceleration = maxMoveSpeed * Time.fixedDeltaTime / moveAccelerationTime;
            if (!onGround) acceleration *= 1f - moveAccelerationAirPenalty;
            velocity = Vector3.MoveTowards(velocity, target, acceleration);

            body.linearVelocity = new Vector3()
            {
                x = velocity.x,
                y = body.linearVelocity.y,
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
                velocity = motor.body.linearVelocity;
                lastJumpTimer = motor.lastJumpTimer;
            }

            private uint tick;
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }
    }
}