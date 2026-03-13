using System.Collections.Generic;
using UnityEngine;

namespace Game.Units.Players
{
    public enum FrameEventType
    {
        Hit,
        UnlockMovement,
        MoveForce,
        SpawnVFX,
        PlaySound,
        CameraShake
    }

    public enum AttackHitboxShape
    {
        Circle,
        Box
    }

    [System.Serializable]
    public class AttackHitbox
    {
        [SerializeField] private AttackHitboxShape shape = AttackHitboxShape.Circle;
        [SerializeField] private Vector2 offset = new(0.5f, 0f);
        [Min(0.01f)]
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private Vector2 size = Vector2.one;

        public AttackHitboxShape Shape => shape;
        public Vector2 Offset => offset;
        public float Radius => radius;
        public Vector2 Size => size;
    }

    [System.Serializable]
    public class FrameEvent
    {
        [SerializeField] private FrameEventType eventType = FrameEventType.Hit;
        [Min(0)]
        [SerializeField] private int startFrame;
        [Min(0)]
        [SerializeField] private int endFrame;
        [SerializeField] private GameObject vfxPrefab;
        [SerializeField] private Vector2 vfxOffset;
        [SerializeField] private AudioClip sound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 1f;
        [Min(0f)]
        [SerializeField] private float cameraShakeStrength = 1f;
        [Min(0f)]
        [SerializeField] private float cameraShakeDuration = 0.1f;
        [SerializeField] private Vector2 moveForce = new(2f, 0f);
        [SerializeField] private bool moveForceUsesFacing = true;
        [SerializeField] private bool overrideHorizontalVelocity = true;

        public FrameEventType EventType => eventType;
        public int StartFrame => startFrame;
        public int EndFrame => Mathf.Max(startFrame, endFrame);
        public GameObject VfxPrefab => vfxPrefab;
        public Vector2 VfxOffset => vfxOffset;
        public AudioClip Sound => sound;
        public float SoundVolume => soundVolume;
        public float CameraShakeStrength => cameraShakeStrength;
        public float CameraShakeDuration => cameraShakeDuration;
        public Vector2 MoveForce => moveForce;
        public bool MoveForceUsesFacing => moveForceUsesFacing;
        public bool OverrideHorizontalVelocity => overrideHorizontalVelocity;

        public bool IncludesFrame(int frame)
        {
            return frame >= StartFrame && frame <= EndFrame;
        }
    }

    [CreateAssetMenu(fileName = "AttackData", menuName = "Game/Combat/Attack Data")]
    public class AttackData : ScriptableObject
    {
        [SerializeField] private AttackHitbox hitbox = new();
        [SerializeField] private List<FrameEvent> frameEvents = new();

        public AttackHitbox Hitbox => hitbox;
        public IReadOnlyList<FrameEvent> FrameEvents => frameEvents;
    }
}