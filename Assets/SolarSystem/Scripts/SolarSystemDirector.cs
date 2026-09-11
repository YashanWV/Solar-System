using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-100)]
public sealed class SolarSystemDirector : MonoBehaviour
{
    public CelestialBody[] bodies;
    public Camera mainCamera, mapCamera;
    public Material orbitMaterial, starMaterial, ringMaterial, coronaMaterial;
    public AudioClip ambience, solarAudio;
    [Range(.1f, 120)] public float daysPerSecond = 3;
    public static bool Paused { get; private set; }
    CelestialBody selected;
    double days;
    float yaw = 0, pitch = 48, distance = 218, desiredDistance = 218;
    Vector3 pivot;
    bool showOrbits = true, showLabels = true, muted, showMap = true;
    GameObject effects;
    AudioSource ambientSource, focusSource;
    RenderTexture mapTexture;
    VolumeProfile profile;
    GUIStyle small, label, title, button, bodyText;
    Texture2D panel, line;
    float uiScale, viewWidth, viewHeight;
    Rect mapRect;

    void Start()
    {
        Paused = false;
        CelestialBody.SimulationDays = 0;
        effects = SolarSystemEffects.Build(transform, bodies, orbitMaterial, starMaterial, ringMaterial, coronaMaterial);
        mainCamera.backgroundColor = new Color(.001f,.002f,.007f);
        mainCamera.clearFlags = CameraClearFlags.Skybox;
        mainCamera.nearClipPlane = .05f;
        mainCamera.farClipPlane = 2000;
        mainCamera.fieldOfView = 48;
        var cameraData = mainCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraData.dithering = true;
        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(.65f); bloom.threshold.Override(1); bloom.scatter.Override(.65f);
        var tonemap = profile.Add<Tonemapping>(); tonemap.mode.Override(TonemappingMode.ACES);
        var vignette = profile.Add<Vignette>(); vignette.intensity.Override(.23f); vignette.smoothness.Override(.5f);
        mapTexture = new RenderTexture(384,384,16) { name = "Solar system overview" };
        mapTexture.Create();
        mapCamera.targetTexture = mapTexture;
        mapCamera.rect = new Rect(0,0,1,1);
        mapCamera.orthographic = true; mapCamera.orthographicSize = 118;
        mapCamera.transform.SetPositionAndRotation(new Vector3(0,250,0), Quaternion.Euler(90,0,0));
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(.008f,.016f,.026f);
        // Layer 2 contains backdrop particles; the plan view stays legible.
        mapCamera.cullingMask = ~(1 << 2);
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = ambience; ambientSource.loop = true; ambientSource.volume = .055f;
        if (ambience) ambientSource.Play();
        focusSource = gameObject.AddComponent<AudioSource>();
        focusSource.loop = true; focusSource.volume = .045f;
        UpdateCamera(true);
    }

    void Update()
    {
        if (!Paused) days += Time.deltaTime * daysPerSecond;
        CelestialBody.SimulationDays = days;
        if (Input.GetKeyDown(KeyCode.Space)) Paused = !Paused;
        if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.Escape)) Select(null);
        if (Input.GetKeyDown(KeyCode.O)) ToggleOrbits();
        if (Input.GetKeyDown(KeyCode.L)) showLabels = !showLabels;
        if (Input.GetKeyDown(KeyCode.M)) ToggleAudio();
        for (int i = 1; i <= 8; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0+i))) Select(bodies[i]);
        if (!PointerOverUI())
        {
            if (Input.GetMouseButton(1)) { yaw += Input.GetAxis("Mouse X")*3; pitch = Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*3,-80,85); }
            float minimum = selected ? selected.radius * (selected.displayName=="Saturn" ? 4 : 2.8f) : 25;
            desiredDistance = Mathf.Clamp(desiredDistance * Mathf.Exp(-Input.mouseScrollDelta.y*.1f),minimum,400);
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out var hit, 1000))
                { var body = hit.collider.GetComponentInParent<CelestialBody>(); if (body) Select(body); }
                else
                {
                    // Small inner planets remain selectable in the overview.
                    CelestialBody closest = null; float best = 18;
                    foreach (var b in bodies)
                    {
                        Vector3 p = mainCamera.WorldToScreenPoint(b.transform.position);
                        float d = Vector2.Distance(new Vector2(p.x,p.y), Input.mousePosition);
                        if (p.z>0 && d<best) { best=d; closest=b; }
                    }
                    if (closest) Select(closest);
                }
            }
        }
    }

    void LateUpdate() => UpdateCamera(false);
    void UpdateCamera(bool immediate)
    {
        Vector3 target = selected ? selected.transform.position : Vector3.zero;
        float blend = immediate ? 1 : 1-Mathf.Exp(-Time.unscaledDeltaTime*5);
        pivot = Vector3.Lerp(pivot,target,blend);
        distance = Mathf.Lerp(distance,desiredDistance,blend);
        float cameraYaw = yaw + (selected && selected.orbitRadius>0 ? Mathf.Atan2(selected.transform.position.x,selected.transform.position.z)*Mathf.Rad2Deg : 0);
        mainCamera.transform.position = pivot + Quaternion.Euler(pitch,cameraYaw,0)*new Vector3(0,0,-distance);
        mainCamera.transform.LookAt(pivot);
    }
    void Select(CelestialBody body)
    {
        selected = body;
        desiredDistance = body ? Mathf.Max(body.radius*(body.displayName=="Saturn"?7:5.5f),2.5f) : 218;
        pitch = body ? 18 : 48;
        yaw = body && body.orbitRadius>0 ? 32 : 0;
        mainCamera.cullingMask = body ? ~(1<<8) : ~0;
        focusSource.Stop();
        if (body)
        {
            focusSource.clip = body.displayName=="Sun" ? solarAudio : ambience;
            focusSource.pitch = .7f + Array.IndexOf(bodies,body)*.055f;
            if (focusSource.clip) focusSource.Play();
        }
    }
    void ToggleAudio() { muted=!muted; ambientSource.mute=muted; focusSource.mute=muted; }
    void ToggleOrbits()
    {
        showOrbits=!showOrbits;
        foreach(var l in effects.GetComponentsInChildren<LineRenderer>(true)) l.enabled=showOrbits;
    }
    bool PointerOverUI()
    {
        float s=Mathf.Min(Screen.width/1440f,Screen.height/900f);
        float w=Screen.width/s, h=Screen.height/s;
        Vector2 p=new Vector2(Input.mousePosition.x/s,(Screen.height-Input.mousePosition.y)/s);
        return p.y<108 || p.y>h-100 || p.x<230 || (selected && p.x>w-310 && p.y<410) || (showMap && p.x>w-260 && p.y>h-380);
    }
    static Texture2D Solid(Color color)
    {
        var t=new Texture2D(1,1); t.SetPixel(0,0,color); t.Apply(); return t;
    }
    void InitGUI()
    {
        if (small!=null) return;
        panel=Solid(new Color(.015f,.03f,.055f,.92f)); line=Solid(new Color(.18f,.35f,.43f,.6f));
        small=new GUIStyle(GUI.skin.label){fontSize=12}; small.normal.textColor=new Color(.48f,.65f,.72f);
        label=new GUIStyle(GUI.skin.label){fontSize=16}; label.normal.textColor=new Color(.84f,.9f,.95f);
        title=new GUIStyle(label){fontSize=31,fontStyle=FontStyle.Bold};
        bodyText=new GUIStyle(label){fontSize=14,wordWrap=true};
        button=new GUIStyle(GUI.skin.button){fontSize=14,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(14,8,0,0)};
        button.normal.background=panel; button.hover.background=line; button.active.background=line;
        button.normal.textColor=new Color(.8f,.87f,.91f);
    }
    void Text(Rect rect,string text,GUIStyle style,Color? color=null)
    { Color old=GUI.color; if(color.HasValue) GUI.color=color.Value; GUI.Label(rect,text,style); GUI.color=old; }
    bool Button(Rect rect,string text) => GUI.Button(rect,text,button);

    void OnGUI()
    {
        if (!mainCamera || !mapTexture) return;
        InitGUI();
        uiScale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
        viewWidth=Screen.width/uiScale; viewHeight=Screen.height/uiScale;
        Matrix4x4 old=GUI.matrix; GUI.matrix=Matrix4x4.Scale(Vector3.one*uiScale);
        Text(new Rect(30,20,400,22),"S O L   /   0 1       •       INTERACTIVE ATLAS",small);
        Text(new Rect(28,44,500,44),"THE SOLAR SYSTEM",title);
        Text(new Rect(viewWidth-440,34,410,22),"ONE STAR. EIGHT PLANETS. COUNTLESS WORLDS.",small);
        Text(new Rect(viewWidth-440,56,410,22),"Explore our neighbourhood in space",label);
        GUI.DrawTexture(new Rect(30,98,viewWidth-60,1),line);
        Text(new Rect(30,121,210,25),"DESTINATIONS",small);
        if(Button(new Rect(30,153,186,36),"↗   System overview")) Select(null);
        for(int i=0;i<bodies.Length;i++)
        {
            var b=bodies[i]; float y=200+i*36;
            if (selected==b) GUI.DrawTexture(new Rect(26,y+3,3,26),line);
            Color prior=GUI.backgroundColor; GUI.backgroundColor=selected==b?new Color(.35f,.7f,.8f):Color.white;
            if(Button(new Rect(30,y,186,31),(i>0&&i<9?i.ToString("00"):" •")+"    "+b.displayName)) Select(b);
            GUI.backgroundColor=prior;
        }
        Text(new Rect(30,viewHeight-220,180,90),"Drag right mouse to orbit\nScroll to zoom\nClick a world to explore\nHome to return",bodyText);
        if (selected) DrawInfo();
        if(showLabels) DrawLabels();
        if(showMap)
        {
            mapRect=new Rect(viewWidth-244,viewHeight-343,214,214);
            GUI.DrawTexture(new Rect(mapRect.x-8,mapRect.y-34,230,256),panel);
            Text(new Rect(mapRect.x,mapRect.y-29,214,22),"ORBITAL OVERVIEW  /  TOP",small);
            GUI.DrawTexture(mapRect,mapTexture,ScaleMode.StretchToFill);
            foreach(var b in bodies)
            {
                Vector3 p=mapCamera.WorldToViewportPoint(b.transform.position);
                if(p.x<0||p.x>1||p.y<0||p.y>1)continue;
                Rect dot=new Rect(mapRect.x+p.x*mapRect.width-3,mapRect.y+(1-p.y)*mapRect.height-3,6,6);
                var c=GUI.color; GUI.color=b.accent; GUI.DrawTexture(dot,Texture2D.whiteTexture);GUI.color=c;
                if(Event.current.type==EventType.MouseDown&&new Rect(dot.x-6,dot.y-6,18,18).Contains(Event.current.mousePosition)) {Select(b);Event.current.Use();}
            }
        }
        float bottom=viewHeight-86;
        GUI.DrawTexture(new Rect(30,bottom-15,viewWidth-60,1),line);
        if(Button(new Rect(30,bottom,98,34),Paused?"▶   Play":"Ⅱ   Pause"))Paused=!Paused;
        Text(new Rect(143,bottom+7,94,25),"TIME FLOW",small);
        daysPerSecond=GUI.HorizontalSlider(new Rect(234,bottom+14,148,16),daysPerSecond,.1f,120);
        Text(new Rect(396,bottom+7,180,25),daysPerSecond.ToString("0.0")+" days / second",small);
        if(Button(new Rect(584,bottom,130,34),showOrbits?"Orbits  •  ON":"Orbits  •  OFF"))ToggleOrbits();
        if(Button(new Rect(724,bottom,130,34),showLabels?"Labels  •  ON":"Labels  •  OFF"))showLabels=!showLabels;
        if(Button(new Rect(864,bottom,130,34),showMap?"Map  •  ON":"Map  •  OFF")){showMap=!showMap;mapCamera.enabled=showMap;}
        if(Button(new Rect(1004,bottom,150,34),muted?"Audio  •  OFF":"Audio  •  ON"))ToggleAudio();
        Text(new Rect(viewWidth-234,bottom+7,220,25),"ELAPSED   "+days.ToString("N1")+" DAYS",small);
        Text(new Rect(30,viewHeight-35,1100,22),"Illustrative positions • Compressed distances & enlarged worlds • Real orbital period ratios • Planet spins slowed 40× • Artistic audio; space is silent",small);
        GUI.matrix=old;
    }
    void DrawInfo()
    {
        float x=viewWidth-300;
        GUI.DrawTexture(new Rect(x,121,270,275),panel);
        Text(new Rect(x+20,139,236,22),selected.displayName=="Pluto"?"DWARF PLANET":selected.displayName=="Moon"?"EARTH'S NATURAL SATELLITE":selected.displayName=="Sun"?"G2V  /  OUR HOME STAR":"PLANETARY PROFILE",small);
        Text(new Rect(x+18,162,240,44),selected.displayName,title);
        Text(new Rect(x+20,213,231,75),selected.description,bodyText);
        Text(new Rect(x+20,294,235,22),"MEAN RADIUS     "+selected.trueRadiusKm.ToString("N0")+" km",small);
        Text(new Rect(x+20,321,235,22),(selected.displayName=="Moon"?"FROM EARTH         ":"FROM SUN              ")+selected.distanceAU.ToString("0.###")+" AU",small);
        Text(new Rect(x+20,348,235,22),selected.displayName=="Sun"?"SPIN                      ~25 days (equator)":"ORBIT                     "+selected.orbitalPeriodDays.ToString("N1")+" days",small);
    }
    void DrawLabels()
    {
        var occupied = new System.Collections.Generic.List<Rect>();
        foreach(var b in bodies)
        {
            if(selected && b!=selected)continue;
            if(!selected && b.displayName=="Moon")continue;
            Vector3 p=mainCamera.WorldToScreenPoint(b.transform.position+mainCamera.transform.up*(b.radius+.12f));
            float x=p.x/uiScale, y=(Screen.height-p.y)/uiScale;
            if(p.z<0 || x<235 || x>viewWidth-310 || y<120 || y>viewHeight-125)continue;
            Rect rect = new Rect(x+8,y-22,92,24);
            for(int tries=0;tries<6;tries++)
            {
                bool overlap=false;foreach(var prior in occupied)if(prior.Overlaps(rect)){overlap=true;break;}
                if(!overlap)break;rect.y-=25;
            }
            occupied.Add(rect);
            Text(rect,b.displayName,label,b.accent);
        }
    }
    void OnDestroy()
    {
        Paused=false;
        if(mapCamera)mapCamera.targetTexture=null;
        if(mapTexture){mapTexture.Release();Destroy(mapTexture);}
        if(profile){foreach(var c in profile.components)Destroy(c);Destroy(profile);}
        if(panel)Destroy(panel); if(line)Destroy(line);
    }
}
