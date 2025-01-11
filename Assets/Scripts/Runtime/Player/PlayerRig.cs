using System;
using Runtime.Weapons;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Runtime.Player
{
    public class PlayerRig : MonoBehaviour
    {
        public TwoBoneIKConstraint leftArm;
        public TwoBoneIKConstraint rightArm;

        private PlayerController player;
        private RigBuilder rigBuilder;
        private Weapon currentWeapon;

        private void Awake()
        {
            player = GetComponentInParent<PlayerController>();
            rigBuilder = GetComponentInParent<RigBuilder>();
        }

        private void Update()
        {
            var weapon = player.weaponsManager.currentWeapon;
            if (currentWeapon != weapon)
            {
                rigBuilder.Clear();
                
                currentWeapon = weapon;
                if (weapon != null)
                {
                    leftArm.data.target = weapon.leftHandIkTarget;

                    rightArm.data.target = weapon.rightHandIkTarget;
                }

                leftArm.enabled = leftArm.data.target != null;
                rightArm.enabled = rightArm.data.target != null;

                rigBuilder.Build();
            }
        }
    }
}