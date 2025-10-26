using UnityEngine;
using System.Collections.Generic;

namespace Game.Units.Boss
{
    public class BossBulletHellAttack : MonoBehaviour
    {
        public enum BulletPattern { Circle, Arc, Aimed, Random, SingleShot, Spiral, Wave, Shotgun, BurstRing, Cross, X }
        [Header("Bullet Hell Settings")]
        public GameObject projectilePrefab;
        public Transform attackPoint;
        public int bulletsPerBurst = 12;
        public float burstInterval = 1.0f;
        public float bulletSpeed = 8f;
        public float spreadAngle = 360f;
        public int poolSize = 50;
        [Header("Pattern")]
        public BulletPattern pattern = BulletPattern.Circle;
        [Tooltip("Arc pattern: center angle in degrees (0=right, 90=up, 180=left, 270=down)")]
        public float arcCenterAngle = 0f;
        [Tooltip("Arc pattern: arc width in degrees")]
        public float arcWidth = 90f;
        [Tooltip("Aimed pattern: target transform (e.g., player)")]
        public Transform aimTarget;
        [Header("Breath Cooldown")]
        public float breathCooldown = 2f;
        private float breathTimer;
        private bool isBreathing;
        [Header("Direction Override")]
        public Vector2 fixedDirection = Vector2.right;
        public bool useFixedDirection = false;
        [Header("Boss Targeting & Teleport")]
        public Transform playerTarget;
        public float teleportInterval = 4f;
        public Vector2 teleportAreaCenter;
        public Vector2 teleportAreaSize = new Vector2(10, 6);
        [Header("Spiral Settings")]
        private float spiralAngle = 0f;
        [Header("Wave Settings")]
        private float wavePhase = 0f;
        [Header("Shotgun Settings")]
        public int shotgunPellets = 5;
        public float shotgunSpread = 30f;
        [Header("Burst Ring Settings")]
        public int burstRings = 3;
        public float burstRingInterval = 0.15f;
        private int burstRingCount = 0;
        private float burstRingTimer = 0f;
        private bool isBursting = false;

        private float burstTimer;
        private Queue<GameObject> projectilePool;
        private System.Array patternValues;
        private float teleportTimer;

        void Awake()
        {
            projectilePool = new Queue<GameObject>(poolSize);
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(projectilePrefab);
                obj.SetActive(false);
                projectilePool.Enqueue(obj);
            }
            patternValues = System.Enum.GetValues(typeof(BulletPattern));
        }

        void Update()
        {
            // Teleport logic
            teleportTimer += Time.deltaTime;
            if (teleportTimer >= teleportInterval)
            {
                TeleportRandomly();
                teleportTimer = 0f;
            }

            if (isBreathing)
            {
                breathTimer += Time.deltaTime;
                if (breathTimer >= breathCooldown)
                {
                    isBreathing = false;
                    breathTimer = 0f;
                }
                return;
            }
            // Burst ring logic
            if (isBursting)
            {
                burstRingTimer += Time.deltaTime;
                if (burstRingTimer >= burstRingInterval)
                {
                    burstRingTimer = 0f;
                    burstRingCount++;
                    FireCircle();
                    if (burstRingCount >= burstRings)
                    {
                        isBursting = false;
                    }
                }
                return;
            }
            burstTimer += Time.deltaTime;
            if (burstTimer >= burstInterval)
            {
                // Randomize pattern each burst
                pattern = (BulletPattern)patternValues.GetValue(Random.Range(0, patternValues.Length));
                FireBurst();
                burstTimer = 0f;
                isBreathing = true;
            }

            // Always flip boss to face player on Y axis
            if (playerTarget != null)
            {
                float dir = playerTarget.position.x - transform.position.x;
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (dir >= 0 ? 1 : -1);
                transform.localScale = scale;
            }
        }

        void FireBurst()
        {
            if (projectilePrefab == null || attackPoint == null) return;
            switch (pattern)
            {
                case BulletPattern.Circle:
                    FireCircle();
                    break;
                case BulletPattern.Arc:
                    FireArc();
                    break;
                case BulletPattern.Aimed:
                    FireAimed();
                    break;
                case BulletPattern.Random:
                    FireRandom();
                    break;
                case BulletPattern.SingleShot:
                    FireSingleShot();
                    break;
                case BulletPattern.Spiral:
                    FireSpiral();
                    break;
                case BulletPattern.Wave:
                    FireWave();
                    break;
                case BulletPattern.Shotgun:
                    FireShotgun();
                    break;
                case BulletPattern.BurstRing:
                    StartBurstRing();
                    break;
                case BulletPattern.Cross:
                    FireCross();
                    break;
                case BulletPattern.X:
                    FireX();
                    break;
            }
        }

        // Utility: get base angle to player (world positions only, no inversion)
        float GetBaseAngleToPlayer()
        {
            if (playerTarget != null && attackPoint != null)
            {
                Vector2 baseDir = ((Vector2)playerTarget.position - (Vector2)attackPoint.position).normalized;
                return Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            }
            return 0f;
        }

        void FireSingleShot()
        {
            Vector2 dir = (playerTarget != null && attackPoint != null)
                ? ((Vector2)playerTarget.position - (Vector2)attackPoint.position).normalized
                : Vector2.right;
            SpawnBullet(dir);
        }

        void FireCircle()
        {
            float angleStep = 360f / bulletsPerBurst;
            float angle = 0f;
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
                SpawnBullet(dir);
                angle += angleStep;
            }
        }

        void FireArc()
        {
            float baseAngle = GetBaseAngleToPlayer();
            float angleStep = arcWidth / (bulletsPerBurst - 1);
            float startAngle = baseAngle - arcWidth / 2f;
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnBullet(dir.normalized);
            }
        }

        void FireAimed()
        {
            float baseAngle = GetBaseAngleToPlayer();
            float angleStep = 30f / Mathf.Max(1, bulletsPerBurst - 1); // 30 deg spread
            float startAngle = baseAngle - (angleStep * (bulletsPerBurst - 1) / 2f);
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnBullet(dir.normalized);
            }
        }

        void FireRandom()
        {
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                float angle = Random.Range(0f, 360f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
                SpawnBullet(dir);
            }
        }

        void FireSpiral()
        {
            float baseAngle = GetBaseAngleToPlayer();
            float angleStep = 360f / bulletsPerBurst;
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                float angle = baseAngle + spiralAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnBullet(dir.normalized);
            }
            spiralAngle += 15f; // Spiral speed
        }

        void FireWave()
        {
            float baseAngle = GetBaseAngleToPlayer();
            float waveAmplitude = 45f;
            float waveFrequency = 2f;
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                float offset = Mathf.Sin(wavePhase + i * waveFrequency) * waveAmplitude;
                float angle = baseAngle + offset;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnBullet(dir.normalized);
            }
            wavePhase += 0.5f;
        }

        void FireShotgun()
        {
            float baseAngle = GetBaseAngleToPlayer();
            float startAngle = baseAngle - shotgunSpread / 2f;
            float angleStep = shotgunSpread / (shotgunPellets - 1);
            for (int i = 0; i < shotgunPellets; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnBullet(dir.normalized);
            }
        }

        void StartBurstRing()
        {
            isBursting = true;
            burstRingCount = 0;
            burstRingTimer = 0f;
        }

        void FireCross()
        {
            Vector2[] dirs = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
            foreach (var dir in dirs)
                SpawnBullet(dir);
        }

        void FireX()
        {
            Vector2[] dirs = {
                new Vector2(1,1).normalized,
                new Vector2(-1,1).normalized,
                new Vector2(1,-1).normalized,
                new Vector2(-1,-1).normalized
            };
            foreach (var dir in dirs)
                SpawnBullet(dir);
        }

        void SpawnBullet(Vector2 direction)
        {
            GameObject bullet = GetPooledProjectile();
            bullet.transform.position = attackPoint.position;
            // Always use world direction, never flip for boss or attack point
            Vector2 finalDir = direction.normalized;
            float angle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle); // Always set rotation
            // Always set projectile localScale.x positive
            Vector3 scale = bullet.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            bullet.transform.localScale = scale;
            bullet.SetActive(true);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (useFixedDirection)
                    finalDir = fixedDirection.normalized;
                rb.linearVelocity = finalDir * bulletSpeed;
            }
        }

        GameObject GetPooledProjectile()
        {
            if (projectilePool.Count > 0)
            {
                GameObject obj = projectilePool.Dequeue();
                return obj;
            }
            // If pool is empty, instantiate a new one (optional, or just reuse oldest)
            GameObject newObj = Instantiate(projectilePrefab);
            return newObj;
        }

        public void ReturnProjectile(GameObject obj)
        {
            obj.SetActive(false);
            projectilePool.Enqueue(obj);
        }

        void TeleportRandomly()
        {
            Vector2 min = teleportAreaCenter - teleportAreaSize * 0.5f;
            Vector2 max = teleportAreaCenter + teleportAreaSize * 0.5f;
            Vector2 newPos = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(teleportAreaCenter, teleportAreaSize);
        }
    }
}
