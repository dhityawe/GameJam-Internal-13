using UnityEngine;
using System;
using GabrielBigardi.SpriteAnimator;

namespace Game.Units.Players
{
    public class PlayerVfxController : MonoBehaviour
    {
        [Tooltip("Optional effect SpriteAnimator")]
        public SpriteAnimator playerEffect;
        
        // Subscribe to animation events
        private void OnEnable()
        {
            PlayerAnimationController.OnPlayerStateChanged += HandlePlayerStateChanged;
            PlayerAnimationController.OnPlayerHurt += PlayHurtVfx;
            PlayerAnimationController.OnPlayerSkill += PlaySkillVfx;
        }

        private void OnDisable()
        {
            PlayerAnimationController.OnPlayerStateChanged -= HandlePlayerStateChanged;
            PlayerAnimationController.OnPlayerHurt -= PlayHurtVfx;
            PlayerAnimationController.OnPlayerSkill -= PlaySkillVfx;
        }

        private void HandlePlayerStateChanged(PlayerAnimationController.AnimState state)
        {
            // Example: play jump VFX
            if (state == PlayerAnimationController.AnimState.Jump)
            {
                PlayJumpVfx();
            }
            // Add more state-based VFX as needed
        }


        [ContextMenu("Debug/Play Jump VFX")]
        private void PlayJumpVfx()
        {
            // TODO: Implement jump VFX
            Debug.Log("Play Jump VFX");
        }


        [ContextMenu("Debug/Play Hurt VFX")]
        private void PlayHurtVfx()
        {
            playerEffect.Play("Hurt");
        }


        [ContextMenu("Debug/Play Skill VFX")]
        private void PlaySkillVfx()
        {
            // TODO: Implement skill VFX
            Debug.Log("Play Skill VFX");
        }


        [ContextMenu("Debug/Play Block VFX")]
        private void PlayBlockVfx()
        {
            // TODO: Implement block VFX
            Debug.Log("Play Block VFX");
        }

        
    }
}
