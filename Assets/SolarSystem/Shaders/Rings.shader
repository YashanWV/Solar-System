Shader "SolarSystem/Rings"
{
 Properties { _BaseMap("Radial ring map",2D)="white" {} _Tint("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint; float4 _BaseMap_ST;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.world=TransformObjectToWorld(i.p.xyz);o.local=i.p.xyz;o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target
   {
    half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(i.uv.x,.5))*_Tint;
    float3 center=TransformObjectToWorld(float3(0,0,0));
    float3 light=normalize(-i.world), rel=i.world-center;
    float projection=dot(-rel,light);
    float distanceToAxis=length(rel+light*projection);
    // Ring inner edge is 1.3 planet radii. The material's mesh has unit scale.
    float radius=length(i.local)/lerp(1.3,2.35,i.uv.x);
    float shadow=projection>0?smoothstep(radius*.97,radius*1.05,distanceToAxis):1;
    c.rgb*=lerp(.09,1.1,shadow);return c;
   }
   ENDHLSL
  }
 }
}
