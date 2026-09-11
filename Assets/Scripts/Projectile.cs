using UnityEngine;
using UnityEngine.Rendering;

public class Projectile : MonoBehaviour
{
    [Tooltip("Initial visual travel speed in world units per second.")]
    public float projectileSpeed = 5f;
    [Tooltip("Maximum distance from the Sun before recycling the comet.")]
    public float destroyDistance = 140f;

    private Vector3 velocity;
    private float age;
    private LineRenderer ionTail;
    private bool configured;

    private void Awake()
    {
        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>())
        {
            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
        }
        foreach (Light lightSource in GetComponentsInChildren<Light>()) lightSource.enabled = false;
        foreach (AudioSource source in GetComponentsInChildren<AudioSource>())
        {
            source.Stop();
            source.enabled = false;
        }
        foreach (Collider shape in GetComponentsInChildren<Collider>()) shape.enabled = false;
    }

    private void Start()
    {
        if (!configured) Configure(null, null);
    }

    public void Configure(Material cometMaterial, Material tailMaterial)
    {
        configured = true;
        age = 0f;
        velocity = transform.forward * Mathf.Max(0.1f, projectileSpeed);
        transform.localScale = Vector3.one * 0.25f;
        foreach (MeshRenderer nucleus in GetComponentsInChildren<MeshRenderer>())
        {
            if (cometMaterial != null) nucleus.sharedMaterial = cometMaterial;
            nucleus.shadowCastingMode = ShadowCastingMode.Off;
            nucleus.receiveShadows = false;
        }
        if (ionTail == null) ionTail = gameObject.AddComponent<LineRenderer>();
        ionTail.useWorldSpace = true;
        ionTail.positionCount = 8;
        ionTail.numCapVertices = 4;
        ionTail.shadowCastingMode = ShadowCastingMode.Off;
        ionTail.receiveShadows = false;
        ionTail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.28f),
            new Keyframe(0.2f, 0.55f), new Keyframe(1f, 0.02f));
        ionTail.widthMultiplier = 1f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] {
            new GradientColorKey(new Color(1.4f, 1.8f, 2f), 0f),
            new GradientColorKey(new Color(0.15f, 0.55f, 1.2f), 1f)
        }, new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        ionTail.colorGradient = gradient;
        if (tailMaterial != null) ionTail.sharedMaterial = tailMaterial;
        ionTail.enabled = tailMaterial != null;
        UpdateTail();
    }

    private void Update()
    {
        if (SolarSystemDirector.Paused) return;
        float delta = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        age += Time.unscaledDeltaTime;
        Vector3 position = transform.position;
        float radius = position.magnitude;
        if (age > 60f || radius > Mathf.Max(10f, destroyDistance) || radius < 5f)
        {
            Destroy(gameObject);
            return;
        }
        // Gentle inverse-square attraction curves the flyby toward the Sun.
        velocity -= position.normalized * (180f / Mathf.Max(25f, radius * radius)) * delta;
        position += velocity * delta;
        if (position.sqrMagnitude < 25f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = position;
        UpdateTail();
    }

    private void UpdateTail()
    {
        if (ionTail == null) return;
        Vector3 position = transform.position;
        // Ion tails point away from the Sun, regardless of travel direction.
        Vector3 antiSolar = position.sqrMagnitude > 0.001f ? position.normalized : Vector3.right;
        float length = Mathf.Lerp(8f, 13f, Mathf.Clamp01(1f - position.magnitude / 100f));
        for (int i = 0; i < ionTail.positionCount; i++)
            ionTail.SetPosition(i, position + antiSolar * (length * i / (ionTail.positionCount - 1f)));
    }
}
