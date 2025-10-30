using UnityEditor;
using UnityEngine;

namespace Game.Units.Players
{
    [CustomEditor(typeof(PlayerVfxController))]
    public class PlayerVfxControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlayerVfxController vfx = (PlayerVfxController)target;

            GUILayout.Space(10);
            GUILayout.Label("Debug VFX", EditorStyles.boldLabel);

            if (GUILayout.Button("Play Jump VFX"))
            {
                var method = vfx.GetType().GetMethod("PlayJumpVfx", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(vfx, null);
            }
            if (GUILayout.Button("Play Hurt VFX"))
            {
                var method = vfx.GetType().GetMethod("PlayHurtVfx", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(vfx, null);
            }
            if (GUILayout.Button("Play Skill VFX"))
            {
                var method = vfx.GetType().GetMethod("PlaySkillVfx", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(vfx, null);
            }
            if (GUILayout.Button("Play Block VFX"))
            {
                var method = vfx.GetType().GetMethod("PlayBlockVfx", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(vfx, null);
            }
        }
    }
}
