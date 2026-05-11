using UnityEngine;

public class MissTextEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float lifetime = 0.45f;
    [SerializeField] private float moveUpDistance = 0.45f;
    [SerializeField] private float startScale = 0.28f;
    [SerializeField] private float endScale = 0.34f;

    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private Vector3 startPosition;
    private float timer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.localPosition;
        transform.localScale = Vector3.one * startScale;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        transform.localPosition = Vector3.Lerp(
            startPosition,
            startPosition + Vector3.up * moveUpDistance,
            t
        );

        transform.localScale = Vector3.Lerp(
            Vector3.one * startScale,
            Vector3.one * endScale,
            t
        );

        if (spriteRenderer != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}