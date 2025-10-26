using UnityEngine;
using DG.Tweening;

public class BossDummyDemo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashDuration = 0.15f;

    private Color _originalColor;

    void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Attack"))
        {
            FlashRed();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Attack"))
        {
            FlashRed();
        }
    }

    /// <summary>
    /// Flashes the boss sprite red briefly to indicate getting hit.
    /// </summary>
    public void FlashRed()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, flashDuration * 0.5f)
                .OnComplete(() => spriteRenderer.DOColor(_originalColor, flashDuration * 0.5f));
        }
    }
}
