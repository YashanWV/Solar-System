Shader "SolarSystem/Line"
{
 Properties { _Tint("Tint",Color)=(1,1,1,1) }
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
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint;
   CBUFFER_END
   struct A {float4 p:POSITION;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.color=i.color*_Tint;return o;}
   half4 frag(V i):SV_Target{return i.color;}
   ENDHLSL
  }
 }
}
