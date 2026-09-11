using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Checks and reports only within this project; never reads external editor logs.
[InitializeOnLoad]
public static class SolarSystemPlayCheck
{
    static readonly List<string> errors = new List<string>();
    static double started;
    static SolarSystemPlayCheck()
    {
        Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged += OnState;
    }
    static void OnLog(string message,string stack,LogType type)
    {
        if (EditorApplication.isPlaying && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) errors.Add(message);
    }
    static void OnState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode){errors.Clear();started=EditorApplication.timeSinceStartup;}
        if(state==PlayModeStateChange.ExitingPlayMode)Report();
    }
    [MenuItem("Solar System/Check Running Model")]
    public static void Report()
    {
        if(!EditorApplication.isPlaying)return;
        var director=Object.FindFirstObjectByType<SolarSystemDirector>();
        var findings=new List<string>(errors);
        if(!director)findings.Add("Missing director");else
        {
            if(director.bodies.Length!=11)findings.Add("Incorrect body count");
            if(!director.mainCamera || !director.mapCamera || director.mapCamera.targetTexture==null)findings.Add("Camera/map not initialized");
            if(director.GetComponents<AudioSource>().Length!=2)findings.Add("Ambience sources missing");
            if(!GameObject.Find("Saturn • Rings"))findings.Add("Saturn rings missing");
            if(!GameObject.Find("Distant stars • 1800"))findings.Add("Stars missing");
            if(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length>4)findings.Add("Comet cap exceeded");
            if(director.GetComponent<UnityEngine.Rendering.Volume>()==null)findings.Add("Post processing missing");
        }
        Directory.CreateDirectory("Tools");
        string report="Runtime smoke check after "+(EditorApplication.timeSinceStartup-started).ToString("0.0")+" seconds\n";
        report+=findings.Count==0?"PASS: no runtime errors; worlds, two cameras, map render target, audio sources, rings, stars, bloom, and comet cap verified.\n":string.Join("\n",findings);
        File.WriteAllText("Tools/RuntimeValidation.txt",report);
        Debug.Log(report);
    }
}
