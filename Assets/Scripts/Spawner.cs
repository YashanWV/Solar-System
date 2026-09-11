using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject spawnPrefab;
    [Min(1f)] public float spawnTime = 18f;
    public Material cometMaterial;
    public Material tailMaterial;

    private float countdown = 6f;
    private readonly System.Random random = new System.Random(7341);

    private void Update()
    {
        if (SolarSystemDirector.Paused || spawnPrefab == null) return;
        countdown -= Time.unscaledDeltaTime;
        if (countdown > 0f) return;
        countdown = Mathf.Max(1f, spawnTime);
        if (GetComponentsInChildren<Projectile>().Length >= 4) return;

        Vector3 position = new Vector3(-95f, Range(10f, 25f), Range(-20f, 20f));
        Vector3 target = new Vector3(Range(7f, 14f), 0f, Range(7f, 14f));
        GameObject instance = Instantiate(spawnPrefab, position,
            Quaternion.LookRotation(target - position), transform);
        instance.name = "Passing comet";
        Projectile comet = instance.GetComponent<Projectile>();
        if (comet == null) comet = instance.AddComponent<Projectile>();
        comet.projectileSpeed = 5f;
        comet.destroyDistance = 140f;
        comet.Configure(cometMaterial, tailMaterial);
    }

    private float Range(float minimum, float maximum)
    {
        return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
    }
}
