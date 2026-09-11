using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SolarSystemBuilder
{
    const string Root = "Assets/SolarSystem/";
    const string ScenePath = "Assets/_Scenes/SolarSystem.unity";
    static readonly string[] Names = {"Sun","Mercury","Venus","Earth","Mars","Jupiter","Saturn","Uranus","Neptune","Moon","Pluto"};
    static readonly string[] Maps = {"sun","mercury","venus_atmosphere","earth_daymap","mars","jupiter","saturn","uranus","neptune","moon",""};
    // AU, period days, eccentricity, inclination, signed rotation hours, mean radius km, node, tilt, display orbit, display radius, phase.
    static readonly double[][] Data = {
        new[]{0d,0,0,0,609.12,695700,0,7.25,0,5,0},
        new[]{.387099,87.969,.205636,7.004979,1407.5088,2439.4,48.330766,.034,8,.40,15},
        new[]{.723336,224.701,.006777,3.394676,-5832.432,6051.8,76.679843,177.4,11,.82,108},
        new[]{1d,365.256,.016711,0,23.9345,6371.0084,0,23.4,15,.88,242},
        new[]{1.523710,686.98,.093394,1.849691,24.623,3389.5,49.559539,25.2,21,.59,332},
        new[]{5.202887,4332.82,.048386,1.304397,9.925,69911,100.473909,3.1,34,3.2,210},
        new[]{9.536676,10755.699,.053862,2.485992,10.6562,58232,113.662424,26.7,46,2.7,333},
        new[]{19.189165,30687.153,.047257,.772638,-17.2399,25362,74.016925,97.8,62,1.75,74},
        new[]{30.069923,60190.03,.00859,1.770043,16.11,24622,131.784226,28.3,78,1.7,150},
        new[]{.00257,27.322,.0549,5.145,655.728,1737.4,0,6.68,2.1,.24,130},
        new[]{39.482,90560,.2488,17.16,-153.2928,1188.3,110.3,119.6,94,.33,285}
    };
    static readonly string[] Descriptions = {
        "The star at the heart of our system. Its gravity holds this family of worlds together.",
        "A small, cratered world with the fastest orbit. Its eccentric path carries it around the Sun in just 88 days.",
        "Wrapped in thick, pale clouds. Venus turns slowly in the opposite direction to most planets.",
        "Our ocean world. Clouds veil continents, while city lights trace the night side.",
        "An ochre desert world with polar ice, towering volcanoes, and a thin atmosphere.",
        "The largest planet: a gas giant wrapped in turbulent cloud bands and enormous storms.",
        "An airy gas giant encircled by bright rings of ice and rock, separated by dark gaps.",
        "A pale blue ice giant tipped dramatically onto its side, with a faint ring system.",
        "Cold, blue, and remote. Neptune takes about 165 Earth years to complete an orbit.",
        "Earth's companion. Its rotation is synchronized with its orbit; eccentricity produces gentle libration.",
        "A dwarf planet beyond Neptune. Its tilted, eccentric orbit crosses the region of the Kuiper belt."
    };
    static readonly Color[] Colors = {
        new Color(1,.75f,.34f),new Color(.72f,.68f,.61f),new Color(.96f,.81f,.56f),new Color(.45f,.76f,1),
        new Color(1,.52f,.34f),new Color(.92f,.77f,.61f),new Color(.93f,.83f,.61f),new Color(.54f,.9f,.91f),
        new Color(.39f,.56f,1),new Color(.78f,.82f,.86f),new Color(.73f,.64f,.59f)
    };

    [MenuItem("Solar System/Build Complete Model")]
    public static void Build()
    {
        // Preserve the supplied scene once, entirely within the authorized project.
        Directory.CreateDirectory("Tools/OriginalScene");
        if (!File.Exists("Tools/OriginalScene/SolarSystem.unity")) File.Copy(ScenePath,"Tools/OriginalScene/SolarSystem.unity");
        Directory.CreateDirectory(Root+"Materials");
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var sky=MaterialAsset("Milky Way sky", "Skybox/Panoramic");
        sky.SetTexture("_MainTex",Texture("stars_milky_way"));sky.SetFloat("_Exposure",.42f);sky.SetColor("_Tint",new Color(.35f,.42f,.55f));sky.SetFloat("_Rotation",55);EditorUtility.SetDirty(sky);
        RenderSettings.skybox=sky; RenderSettings.fog=false;
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.02f,.025f,.04f);
        var director=new GameObject("Solar System • Interactive Atlas").AddComponent<SolarSystemDirector>();
        var system=new GameObject("Worlds • compressed astronomical scale");
        var mesh=CreateSphere();
        var worlds=new List<CelestialBody>();
        for(int i=0;i<Names.Length;i++)
        {
            var go=new GameObject(Names[i]);go.transform.SetParent(system.transform);
            var b=go.AddComponent<CelestialBody>(); var d=Data[i];
            b.displayName=Names[i];b.description=Descriptions[i];b.distanceAU=(float)d[0];b.orbitalPeriodDays=(float)d[1];
            b.eccentricity=(float)d[2];b.inclination=(float)d[3];b.rotationHours=(float)d[4];b.trueRadiusKm=d[5];
            b.ascendingNode=(float)d[6];b.axialTilt=(float)d[7];b.orbitRadius=(float)d[8];b.radius=(float)d[9];b.phaseDegrees=(float)d[10];b.accent=Colors[i];
            if(i>0)b.orbitCenter=worlds[i==9?3:0].transform;
            b.surface=new GameObject("Axial tilt & rotation").transform;b.surface.SetParent(go.transform,false);
            var globe=new GameObject(Names[i]+" • Surface");globe.transform.SetParent(b.surface,false);globe.transform.localScale=Vector3.one*b.radius;
            globe.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=globe.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var mat=MaterialAsset(Names[i],"SolarSystem/Planet");
            mat.SetTexture("_BaseMap",i==10?AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PlutoTexture.jpg"):Texture(Maps[i]));
            mat.SetColor("_Tint",Color.white);mat.SetColor("_Atmosphere",i==3?new Color(.12f,.45f,1):i==7?new Color(.15f,.4f,.45f):i==8?new Color(.1f,.22f,.7f):Color.black);
            mat.SetFloat("_Emission",i==0?3.2f:0);
            if(i==3){mat.SetTexture("_NightMap",Texture("earth_nightmap"));mat.SetTexture("_CloudMap",Texture("earth_clouds"));mat.SetFloat("_Clouds",.75f);}
            renderer.sharedMaterial=mat;
            var collider=go.AddComponent<SphereCollider>();collider.radius=b.radius;
            b.Evaluate(0);worlds.Add(b);EditorUtility.SetDirty(mat);
        }
        director.bodies=worlds.ToArray();
        director.mainCamera=MakeCamera("Main Camera",new Vector3(0,175,-157),false);
        director.mainCamera.tag="MainCamera";director.mainCamera.gameObject.AddComponent<AudioListener>();
        director.mapCamera=MakeCamera("Minimap Camera",new Vector3(0,250,0),true);
        director.mapCamera.rect=new Rect(.82f,.13f,.16f,.24f);director.mapCamera.depth=1;
        director.orbitMaterial=MaterialAsset("Orbit paths","SolarSystem/Line");

        director.orbitMaterial.SetColor("_Tint",new Color(.55f,.7f,.85f,.68f));
        director.starMaterial=MaterialAsset("Starlight","SolarSystem/Glow");director.starMaterial.SetColor("_Tint",new Color(2,2,2,1));director.starMaterial.SetFloat("_Softness",1.4f);
        director.coronaMaterial=MaterialAsset("Solar corona","SolarSystem/Glow");director.coronaMaterial.SetColor("_Tint",new Color(2,1.15f,.4f,.42f));director.coronaMaterial.SetFloat("_Softness",3);
        director.ringMaterial=MaterialAsset("Saturn rings","SolarSystem/Rings");director.ringMaterial.SetTexture("_BaseMap",Texture("saturn_ring_alpha","png"));
        director.ambience=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/dronehum.aif");
        director.solarAudio=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/burning.aif");
        var comet=MaterialAsset("Comet nucleus","SolarSystem/Planet");comet.SetColor("_Tint",new Color(.48f,.8f,1));comet.SetFloat("_Emission",2);
        var tail=MaterialAsset("Comet ion tail","SolarSystem/Line");tail.SetColor("_Tint",new Color(.6f,.85f,1,.85f));
        var spawner=new GameObject("CometSpawner").AddComponent<Spawner>();spawner.transform.position=new Vector3(-95,18,0);spawner.spawnTime=18;
        spawner.spawnPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Comet.prefab");spawner.cometMaterial=comet;spawner.tailMaterial=tail;
        foreach(var m in new[]{director.orbitMaterial,director.starMaterial,director.coronaMaterial,director.ringMaterial,comet,tail})EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        Selection.activeGameObject=system;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(Vector3.zero,Quaternion.Euler(48,0,0),130);
        Validate();
        Debug.Log("Solar system complete. Press Play to explore.");
    }
    static Camera MakeCamera(string name,Vector3 position,bool map)
    {
        var c=new GameObject(name).AddComponent<Camera>();c.transform.position=position;c.transform.LookAt(Vector3.zero);
        c.clearFlags=map?CameraClearFlags.SolidColor:CameraClearFlags.Skybox;c.backgroundColor=new Color(.001f,.002f,.007f);c.nearClipPlane=.05f;c.farClipPlane=2000;c.fieldOfView=48;c.allowHDR=true;
        c.orthographic=map;c.orthographicSize=118;c.GetUniversalAdditionalCameraData().renderPostProcessing=!map;return c;
    }
    static Texture2D Texture(string name,string extension="jpg")=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/2k_"+name+"."+extension);
    static Material MaterialAsset(string name,string shader)
    {
        string path=Root+"Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(shader));m.name=name;AssetDatabase.CreateAsset(m,path);}else m.shader=Shader.Find(shader);
        return m;
    }
    static void SetTransparent(Material m)
    {
        m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
    }
    static Mesh CreateSphere()
    {
        string path=Root+"Materials/Planet sphere.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        const int longitude=96,latitude=64;
        var vertices=new Vector3[(longitude+1)*(latitude+1)];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
        for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++)
        {
            float u=(float)x/longitude,v=(float)y/latitude;float a=u*Mathf.PI*2,b=v*Mathf.PI;int index=y*(longitude+1)+x;
            vertices[index]=new Vector3(Mathf.Sin(b)*Mathf.Sin(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Cos(a));uv[index]=new Vector2(u,1-v);
            if(y<latitude&&x<longitude){int next=index+longitude+1;triangles.AddRange(new[]{index,next,index+1,index+1,next,next+1});}
        }
        var mesh=new Mesh{name="Smooth planetary sphere"};mesh.vertices=vertices;mesh.normals=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    [MenuItem("Solar System/Validate Model")]
    public static void Validate()
    {
        var errors=new List<string>();
        var director=UnityEngine.Object.FindFirstObjectByType<SolarSystemDirector>();
        if(!director)errors.Add("Missing director");else
        {
            if(director.bodies.Length!=11)errors.Add("Expected Sun, 8 planets, Moon, Pluto");
            foreach(var b in director.bodies)
            {
                if(!b||!b.surface){errors.Add("Missing world reference");continue;}
                var r=b.surface.GetComponentInChildren<MeshRenderer>();
                if(!r||!r.sharedMaterial||!r.sharedMaterial.GetTexture("_BaseMap"))errors.Add("Missing surface: "+b.name);
                if(r&&r.sharedMaterial&&ShaderUtil.ShaderHasError(r.sharedMaterial.shader))errors.Add("Shader error: "+b.name);
                if(b.orbitRadius>0&&!b.orbitCenter)errors.Add("Missing orbit center: "+b.name);
            }
            if(!director.mainCamera||!director.mapCamera)errors.Add("Missing camera");
            if(!director.ambience||!director.solarAudio)errors.Add("Missing audio");
            foreach(var m in new[]{director.orbitMaterial,director.starMaterial,director.ringMaterial,director.coronaMaterial})
                if(!m||ShaderUtil.ShaderHasError(m.shader))errors.Add("Missing or broken effect material");
        }
        if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length!=1)errors.Add("Expected exactly one audio listener");
        string report=errors.Count==0?"PASS: 11 worlds; all planet textures, materials, orbital centers, cameras, audio, and effect shaders are present.\n":string.Join("\n",errors);
        Directory.CreateDirectory("Tools");File.WriteAllText("Tools/UnityValidation.txt",DateTime.UtcNow.ToString("u")+"\n"+report);
        if(errors.Count>0)Debug.LogError(report);else Debug.Log(report);
    }
    [MenuItem("Solar System/Capture Preview")]
    static void Capture()
    {
        Directory.CreateDirectory("Screenshots");
        ScreenCapture.CaptureScreenshot("Screenshots/SolarSystem.png",1);
    }
}
