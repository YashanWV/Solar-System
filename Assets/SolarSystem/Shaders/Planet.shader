Shader "SolarSystem/Planet"
{
 Properties
 {
  _BaseMap("Surface", 2D) = "white" {}
  _NightMap("City lights", 2D) = "black" {}
  _CloudMap("Clouds", 2D) = "black" {}
  _Tint("Tint", Color) = (1,1,1,1)
  _Atmosphere("Atmosphere", Color) = (0,0,0,1)
  _Emission("Stellar emission", Float) = 0
  _Clouds("Cloud coverage", Float) = 0
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_NightMap); SAMPLER(sampler_NightMap);
   TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST, _Tint, _Atmosphere;
   float _Emission, _Clouds;
   CBUFFER_END
   struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
   struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; float2 uv:TEXCOORD2; };
   V vert(A a) { V o; o.world=TransformObjectToWorld(a.p.xyz); o.p=TransformWorldToHClip(o.world); o.n=TransformObjectToWorldNormal(a.n); o.uv=TRANSFORM_TEX(a.uv,_BaseMap); return o; }
   half4 frag(V i):SV_Target
   {
    float3 n=normalize(i.n), v=normalize(GetCameraPositionWS()-i.world);
    float3 light=normalize(-i.world+float3(0,.001,0));
    float sun=dot(n,light), day=smoothstep(-.09,.12,sun);
    float3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_Tint.rgb;
    float clouds=SAMPLE_TEXTURE2D(_CloudMap,sampler_CloudMap,i.uv).r*_Clouds;
    albedo=lerp(albedo,float3(.92,.96,1),clouds*.85);
    float3 cities=SAMPLE_TEXTURE2D(_NightMap,sampler_NightMap,i.uv).rgb*(1-day)*(1-clouds)*1.8;
    float rim=pow(1-saturate(dot(n,v)),3.4);
    float3 color=albedo*(.035+1.45*max(sun,0))+cities;
    color+=_Atmosphere.rgb*rim*(.08+.7*day);
    color=lerp(color,albedo*_Emission,step(.01,_Emission));
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
