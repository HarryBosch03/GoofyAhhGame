using System;
using Runtime.Player;
using UnityEngine;

namespace Runtime.Weapons
{
    [DefaultExecutionOrder(200)]
    public class WeaponFabrIk : MonoBehaviour
    {
        public Handedness handedness;
        public Transform root;
        public Transform mid;
        public Transform tip;
        public Transform hint;

        private PlayerController player;
        private float rootMidLength;
        private float midTipLength;

        private void Awake()
        {
            player = GetComponentInParent<PlayerController>();
        }

        private void LateUpdate()
        {
            ResetPose();
            var weapon = player.weaponsManager.currentWeapon;
            if (weapon != null)
            {
                var target = handedness switch
                {
                    Handedness.Left => weapon.leftHandIkTarget,
                    Handedness.Right => weapon.rightHandIkTarget,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (target != null)
                {
                    var rootPos = root.position;
                    var tipPos = target.position;
                    var midPos = GetMidPosition(rootPos, tipPos);

                    root.position = rootPos;
                    root.rotation = Quaternion.LookRotation(rootPos - midPos);
                    mid.position = midPos;
                    mid.rotation = Quaternion.LookRotation(midPos - tipPos);
                    tip.position = tipPos;
                    tip.rotation = target.rotation;
                }
            }
        }

        private Vector3 GetMidPosition(Vector3 rootPos, Vector3 tipPos)
        {
            var midPos = hint.transform.position;

            for (var i = 0; i < 16; i++)
            {
                midPos += (rootPos - midPos).normalized * rootMidLength;
                midPos += (tipPos - midPos).normalized * midTipLength;
            }

            return midPos;
        }

        private void ResetPose()
        {
            root.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            mid.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            tip.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (root != null && mid != null) rootMidLength = (root.position - mid.position).magnitude;
                if (mid != null && tip != null) midTipLength = (mid.position - tip.position).magnitude;
            }
        }

        public enum Handedness
        {
            Left,
            Right
        }
    }
}