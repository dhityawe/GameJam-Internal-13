using System.Collections;
using System.Collections.Generic;
using Game.Units.Players;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Creates afterimage/ghost effect by spawning fading sprite copies.
    /// Uses object pooling for optimal performance.
    /// </summary>
    public class AfterImageEffect : MonoBehaviour
    {
        [Header("Afterimage Settings")]
        [SerializeField] private float _spawnInterval = 0.05f; // Time between spawning afterimages
        [SerializeField] private float _fadeDuration = 0.5f; // How long each afterimage takes to fade
        [SerializeField] private Color _afterimageColor = new Color(1f, 1f, 1f, 0.7f); // Initial color/alpha
        [SerializeField] private int _poolSize = 10; // Number of afterimages to pool
        
        [Header("References")]
        [SerializeField] private SpriteRenderer _sourceSprite; // The sprite to copy
        
        private Queue<GameObject> _afterimagePool;
        private bool _isActive;
        private float _spawnTimer;
        private PlayerDash playerDash;
        
        private void Start()
        {
            // Get reference to PlayerDash
            playerDash = GetComponent<PlayerDash>();
            
            // Auto-find sprite renderer if not assigned
            if (_sourceSprite == null)
            {
                _sourceSprite = GetComponent<SpriteRenderer>();
                if (_sourceSprite == null)
                {
                    _sourceSprite = GetComponentInChildren<SpriteRenderer>();
                }
            }
            
            InitializePool();
            
            // Subscribe to dash events if PlayerDash exists
            if (playerDash != null)
            {
                playerDash.OnStartDash += StartAfterimage;
                playerDash.OnEndDash += StopAfterimage;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (playerDash != null)
            {
                playerDash.OnStartDash -= StartAfterimage;
                playerDash.OnEndDash -= StopAfterimage;
            }
        }
        
        /// <summary>
        /// Initialize the object pool for afterimages
        /// </summary>
        private void InitializePool()
        {
            _afterimagePool = new Queue<GameObject>(_poolSize);
            
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject afterimage = CreateAfterimageObject();
                afterimage.SetActive(false);
                _afterimagePool.Enqueue(afterimage);
            }
        }
        
        /// <summary>
        /// Creates a new afterimage GameObject with required components
        /// </summary>
        private GameObject CreateAfterimageObject()
        {
            GameObject afterimage = new GameObject("Afterimage");
            afterimage.transform.parent = transform.parent; // Same parent as player
            
            SpriteRenderer sr = afterimage.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = _sourceSprite.sortingLayerName;
            sr.sortingOrder = _sourceSprite.sortingOrder - 1; // Render behind player
            
            return afterimage;
        }
        
        /// <summary>
        /// Starts spawning afterimages
        /// </summary>
        public void StartAfterimage()
        {
            if (_sourceSprite == null)
            {
                Debug.LogWarning("AfterImageEffect: Source sprite not assigned!");
                return;
            }
            
            _isActive = true;
            _spawnTimer = 0f;
        }
        
        /// <summary>
        /// Stops spawning afterimages
        /// </summary>
        public void StopAfterimage()
        {
            _isActive = false;
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            _spawnTimer += Time.deltaTime;
            
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnAfterimage();
            }
        }
        
        /// <summary>
        /// Spawns an afterimage from the pool
        /// </summary>
        private void SpawnAfterimage()
        {
            if (_afterimagePool.Count == 0)
            {
                // Pool exhausted, create new one (shouldn't happen with proper pool size)
                GameObject newAfterimage = CreateAfterimageObject();
                _afterimagePool.Enqueue(newAfterimage);
            }
            
            GameObject afterimage = _afterimagePool.Dequeue();
            
            // Set position and rotation
            afterimage.transform.position = transform.position;
            afterimage.transform.rotation = transform.rotation;
            afterimage.transform.localScale = transform.localScale;
            
            // Copy sprite properties
            SpriteRenderer afterimageSR = afterimage.GetComponent<SpriteRenderer>();
            afterimageSR.sprite = _sourceSprite.sprite;
            afterimageSR.flipX = _sourceSprite.flipX;
            afterimageSR.flipY = _sourceSprite.flipY;
            afterimageSR.color = _afterimageColor;
            
            afterimage.SetActive(true);
            
            // Start fade coroutine
            StartCoroutine(FadeAfterimage(afterimage, afterimageSR));
        }
        
        /// <summary>
        /// Fades out the afterimage and returns it to pool
        /// </summary>
        private IEnumerator FadeAfterimage(GameObject afterimage, SpriteRenderer sr)
        {
            float elapsed = 0f;
            Color startColor = _afterimageColor;
            
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / _fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            // Return to pool
            afterimage.SetActive(false);
            _afterimagePool.Enqueue(afterimage);
        }
        
        #region Public API
        /// <summary>
        /// Check if afterimage effect is currently active
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Set the spawn interval dynamically
        /// </summary>
        public void SetSpawnInterval(float interval)
        {
            _spawnInterval = Mathf.Max(0.01f, interval);
        }
        
        /// <summary>
        /// Set the fade duration dynamically
        /// </summary>
        public void SetFadeDuration(float duration)
        {
            _fadeDuration = Mathf.Max(0.1f, duration);
        }
        
        /// <summary>
        /// Set the afterimage color
        /// </summary>
        public void SetAfterimageColor(Color color)
        {
            _afterimageColor = color;
        }
        #endregion
    }
}
