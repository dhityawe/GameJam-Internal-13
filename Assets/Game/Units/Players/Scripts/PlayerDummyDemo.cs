using UnityEngine;

using DG.Tweening;
using Assets.Scripts.Player;
using GabrielBigardi.SpriteAnimator;

public class PlayerDummyDemo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AfterImageEffect afterImageEffect;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackUpward = 2f;
    [SerializeField] private float flashDuration = 0.15f;

    private Color _originalColor;
    [SerializeField] private SpriteAnimator effectAnim;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (afterImageEffect == null) afterImageEffect = GetComponent<AfterImageEffect>();
        _originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    void Update()
    {
        // ...existing code...
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 hitDir = (transform.position - collision.transform.position).normalized;
            if (effectAnim != null)
            {
                effectAnim.Play("Hurt");
            }
            else
            {
                Debug.LogWarning("PlayerDummyDemo: effectAnim reference is missing!");
            }
            GotHit(hitDir);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Vector2 hitDir = (transform.position - other.transform.position).normalized;
            if (effectAnim != null)
            {
                effectAnim.Play("Hurt");
            }
            else
            {
                Debug.LogWarning("PlayerDummyDemo: effectAnim reference is missing!");
            }
            GotHit(hitDir);
        }
    }

    /// <summary>
    /// Called when player gets hit by enemy. Applies knockback, afterimage, and flashes red.
    /// </summary>
    /// <param name="hitDirection">Direction from enemy to player</param>
    public void GotHit(Vector2 hitDirection)
    {
        // Knockback
        if (rb != null)
        {
            // Only use horizontal direction for knockback, always away from enemy
            Vector2 horizontalDir = new Vector2(hitDirection.x, 0f).normalized;
            Vector2 force = (horizontalDir * knockbackForce) + (Vector2.up * knockbackUpward);
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        // Afterimage
        if (afterImageEffect != null)
        {
            afterImageEffect.StartAfterimage();
            // Optionally stop after a short time
            Invoke(nameof(StopAfterimage), 0.2f);
        }

        // Flash red
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, flashDuration * 0.5f)
                .OnComplete(() => spriteRenderer.DOColor(_originalColor, flashDuration * 0.5f));
        }
    }

    private void StopAfterimage()
    {
        if (afterImageEffect != null)
            afterImageEffect.StopAfterimage();
    }
}
