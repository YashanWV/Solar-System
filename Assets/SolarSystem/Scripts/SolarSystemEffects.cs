using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Static, batched dressing built once by the scene director.</summary>
public sealed class SolarSystemEffects : MonoBehaviour
{
    readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

    public static GameObject Build(Transform parent, CelestialBody[] bodies, Material orbitMaterial,
        Material starMaterial, Material ringMaterial, Material coronaMaterial)
    {
        GameObject root = new GameObject("Solar System • Orbits & Starlight");
        root.transform.SetParent(parent, false);
        SolarSystemEffects owner = root.AddComponent<SolarSystemEffects>();
        foreach (CelestialBody body in bodies)
        {
            if (body == null) continue;
            if (body.orbitRadius > 0f && !string.Equals(body.displayName, "Moon", StringComparison.OrdinalIgnoreCase))
                owner.Orbit(body, orbitMaterial);
            if (body.surface != null && string.Equals(body.displayName, "Saturn", StringComparison.OrdinalIgnoreCase))
                owner.Rings(body, ringMaterial, 1.30f, 2.35f, false);
            if (body.surface != null && string.Equals(body.displayName, "Uranus", StringComparison.OrdinalIgnoreCase))
                owner.Rings(body, ringMaterial, 1.85f, 2.05f, true);
            if (string.Equals(body.displayName, "Sun", StringComparison.OrdinalIgnoreCase))
                owner.Cloud("Solar corona", coronaMaterial, new[] {
                    Particle(root.transform.InverseTransformPoint(body.transform.position), body.radius * 6f, new Color(1f, .40f, .09f, .45f))
                });
        }
        owner.Stars(starMaterial);
        owner.Asteroids(orbitMaterial);
        owner.KuiperBelt(starMaterial);
        return root;
    }

    void Orbit(CelestialBody body, Material material)
    {
        GameObject go = new GameObject(body.displayName + " • Orbit");
        go.layer = 8;
        go.transform.SetParent(transform, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 256;
        line.widthMultiplier = Mathf.Lerp(.07f, .11f, Mathf.Clamp01(body.orbitRadius / 80f));
        line.startColor = line.endColor = new Color(body.accent.r, body.accent.g, body.accent.b, .5f);
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        Vector3[] points = new Vector3[256];
        for (int i = 0; i < points.Length; i++) points[i] = body.OrbitPoint(i * 360f / points.Length);
        line.SetPositions(points);
    }

    void Rings(CelestialBody body, Material material, float inner, float outer, bool faint)
    {
        const int sectors = 256;
        Vector3[] vertices = new Vector3[(sectors + 1) * 2];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[sectors * 6];
        for (int i = 0; i <= sectors; i++)
        {
            float angle = i * Mathf.PI * 2f / sectors;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices[i * 2] = direction * body.radius * inner;
            vertices[i * 2 + 1] = direction * body.radius * outer;
            normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
            uvs[i * 2] = new Vector2(0f, (float)i / sectors);
            uvs[i * 2 + 1] = new Vector2(1f, (float)i / sectors);
            if (i == sectors) continue;
            int t = i * 6, v = i * 2;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        Mesh mesh = new Mesh { name = body.displayName + " ring mesh", vertices = vertices,
            normals = normals, uv = uvs, triangles = triangles };
        mesh.RecalculateBounds();
        owned.Add(mesh);
        GameObject go = new GameObject(body.displayName + " • Rings");
        go.transform.SetParent(body.surface, false);
        owned.Add(go); // Ring objects live outside this effect root.
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        if (faint)
        {
            Material copy = new Material(material) { name = "Uranus • faint icy rings" };
            if (copy.HasProperty("_Tint")) copy.SetColor("_Tint", new Color(.56f, .74f, .75f, .20f));
            if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", new Color(.56f, .74f, .75f, .20f));
            owned.Add(copy);
            renderer.sharedMaterial = copy;
        }
        else renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void Stars(Material material)
    {
        System.Random random = new System.Random(73021);
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1800];
        for (int i = 0; i < particles.Length; i++)
        {
            double z = random.NextDouble() * 2.0 - 1.0;
            double a = random.NextDouble() * Math.PI * 2.0;
            double r = Math.Sqrt(1.0 - z * z);
            float distance = Range(random, 270f, 430f);
            Vector3 p = new Vector3((float)(r * Math.Cos(a)), (float)z, (float)(r * Math.Sin(a))) * distance;
            Color color = Color.Lerp(new Color(.56f, .72f, 1f), new Color(1f, .88f, .69f), (float)random.NextDouble());
            color.a = Range(random, .38f, .86f);
            particles[i] = Particle(p, i % 45 == 0 ? Range(random, 1.2f, 1.8f) : Range(random, .35f, .9f), color);
        }
        Cloud("Distant stars • 1800", material, particles, true);
    }

    void Asteroids(Material material)
    {
        const int count = 1100;
        System.Random random = new System.Random(19362);
        Vector3[] vertices = new Vector3[count * 6];
        int[] triangles = new int[count * 24];
        Vector3[] shape = { Vector3.up, Vector3.right, Vector3.forward, Vector3.left, Vector3.back, Vector3.down };
        int[] faces = { 0,2,1, 0,3,2, 0,4,3, 0,1,4, 5,1,2, 5,2,3, 5,3,4, 5,4,1 };
        for (int i = 0; i < count; i++)
        {
            float angle = Range(random, 0f, Mathf.PI * 2f), distance = Range(random, 24f, 28f);
            Vector3 center = new Vector3(Mathf.Cos(angle) * distance, Range(random, -.52f, .52f), Mathf.Sin(angle) * distance);
            float size = Range(random, .022f, .10f);
            Quaternion rotation = Quaternion.Euler(Range(random, 0f, 360f), Range(random, 0f, 360f), Range(random, 0f, 360f));
            for (int v = 0; v < 6; v++) vertices[i * 6 + v] = center + rotation * shape[v] * size * Range(random, .65f, 1.5f);
            for (int t = 0; t < faces.Length; t++) triangles[i * 24 + t] = i * 6 + faces[t];
        }
        Mesh mesh = new Mesh { name = "Batched asteroid belt", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        owned.Add(mesh);
        Material rock = new Material(material) { name = "Asteroid dust • stone" };
        if (rock.HasProperty("_Tint")) rock.SetColor("_Tint", new Color(.32f, .28f, .23f, .8f));
        if (rock.HasProperty("_BaseColor")) rock.SetColor("_BaseColor", new Color(.32f, .28f, .23f, .8f));
        if (rock.HasProperty("_Color")) rock.SetColor("_Color", new Color(.32f, .28f, .23f, .8f));
        owned.Add(rock);
        GameObject go = new GameObject("Asteroid belt • 1100 fragments");
        go.layer = 8;
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = rock;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void KuiperBelt(Material material)
    {
        System.Random random = new System.Random(4278);
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[480];
        for (int i = 0; i < particles.Length; i++)
        {
            float a = Range(random, 0f, Mathf.PI * 2f), r = Range(random, 80f, 92f);
            particles[i] = Particle(new Vector3(Mathf.Cos(a) * r, Range(random, -1.8f, 1.8f), Mathf.Sin(a) * r),
                Range(random, .07f, .22f), new Color(.40f, .55f, .65f, .32f));
        }
        Cloud("Kuiper belt • icy debris", material, particles);
        transform.Find("Kuiper belt • icy debris").gameObject.layer = 8;
    }

    void Cloud(string label, Material material, ParticleSystem.Particle[] particles, bool stars = false)
    {
        GameObject go = new GameObject(label);
        if (stars) go.layer = 2;
        go.transform.SetParent(transform, false);
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.useAutoRandomSeed = false;
        system.randomSeed = 42;
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startSpeed = 0f;
        main.startLifetime = 100000f;
        main.maxParticles = particles.Length;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = system.emission; emission.enabled = false;
        var shape = system.shape; shape.enabled = false;
        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 1f;
        if (stars) renderer.minParticleSize = .0015f;
        system.Play(false);
        system.SetParticles(particles, particles.Length);
        system.Pause(false);
    }

    static ParticleSystem.Particle Particle(Vector3 position, float size, Color color)
    {
        return new ParticleSystem.Particle { position = position, startSize = size, startColor = color,
            startLifetime = 100000f, remainingLifetime = 100000f, velocity = Vector3.zero };
    }

    static float Range(System.Random random, float min, float max) => min + (max - min) * (float)random.NextDouble();

    void OnDestroy()
    {
        foreach (UnityEngine.Object item in owned)
        {
            if (item == null) continue;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
        owned.Clear();
    }
}
