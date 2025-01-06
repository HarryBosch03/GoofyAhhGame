using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Weapons
{
    public class GunAnimator : MonoBehaviour
    {
        public Vector3 viewportPosition = new Vector3(0.33f, -0.19f, 0.64f);
        public Vector3 viewportRotation = new Vector3(0f, 0f, 6f);
        public Vector3 thirdPersonPosition;
        public Vector3 thirdPersonPivotOffset;
        
        [Space]
        public float translationSway;
        public float rotationSway;
        public float smoothing;

        [Space]
        public float moveSwayFrequency;
        public float moveSwayAmplitude;
        
        [Space]
        public Vector3 recoilImpulse;
        public float recoilSpring;
        public float recoilDamping;
        public float recoilSwing;

        private SimpleProjectileWeapon weapon;
        private Vector2 lastRotation;
        private Vector2 smoothedDelta;
        
        private Vector3 recoilPosition;
        private Vector3 recoilVelocity;
        
        private float distance;
        private float speed;

        private void Awake()
        {
            weapon = GetComponentInParent<SimpleProjectileWeapon>();
        }

        private void OnEnable()
        {
            gameObject.layer = 3;
            weapon.ShootEvent += OnShoot;
        }

        private void OnDisable()
        {
            weapon.ShootEvent -= OnShoot;
        }

        private void OnShoot()
        {
            recoilVelocity += new Vector3
            {
                x = Random.Range(-recoilImpulse.x, recoilImpulse.x),
                y = recoilImpulse.y,
                z = recoilImpulse.z,
            };
        }

        private void LateUpdate()
        {
            gameObject.layer = weapon.player.isActiveViewer ? 3 : 0;
            var parentRotation = new Vector2(weapon.player.motor.headRotation.eulerAngles.y, -weapon.player.motor.headRotation.eulerAngles.x);
            var delta = new Vector2()
            {
                x = Mathf.DeltaAngle(lastRotation.x, parentRotation.x),
                y = Mathf.DeltaAngle(lastRotation.y, parentRotation.y),
            } / Time.deltaTime;
            smoothedDelta = Vector2.Lerp(smoothedDelta, delta, Time.deltaTime / Mathf.Max(Time.deltaTime, smoothing));

            transform.position = weapon.player.motor.headPosition + (weapon.player.motor.headRotation * Quaternion.Euler(new Vector3(-smoothedDelta.y, smoothedDelta.x, 0f) * translationSway)) * viewportPosition;
            transform.rotation = weapon.player.motor.headRotation * Quaternion.Euler(new Vector3(-smoothedDelta.y, smoothedDelta.x, 0f) * rotationSway) * Quaternion.Euler(viewportRotation);

            transform.localPosition += recoilPosition;
            transform.localRotation *= Quaternion.Euler(new Vector3(recoilVelocity.z, -recoilVelocity.x, 0f) * recoilSwing);
            
            var recoilForce = -recoilPosition * recoilSpring - recoilVelocity * recoilDamping;
            recoilPosition += recoilVelocity * Time.deltaTime;
            recoilVelocity += recoilForce * Time.deltaTime;
            
            lastRotation = parentRotation;


            speed = weapon.player.motor.onGround ? new Vector2(weapon.player.motor.velocity.x, weapon.player.motor.velocity.z).magnitude : 0f;
            distance += speed * Time.deltaTime;
            
            transform.localPosition += new Vector3(Mathf.Cos(distance * Mathf.PI * moveSwayFrequency), -Mathf.Abs(Mathf.Sin(distance * Mathf.PI * moveSwayFrequency))) * moveSwayAmplitude * speed / weapon.player.motor.maxMoveSpeed;
                
            if (!weapon.player.isActiveViewer)
            {
                transform.position = weapon.player.transform.position + Vector3.up * weapon.player.motor.cameraHeight + weapon.player.transform.rotation * (thirdPersonPosition - thirdPersonPivotOffset) + weapon.player.motor.headRotation * thirdPersonPivotOffset;
            }
        }
    }
}