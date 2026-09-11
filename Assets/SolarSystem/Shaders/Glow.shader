Shader "SolarSystem/Glow"
{
 Properties { _Tint("Tint",Color)=(1,1,1,1) _Softness("Softness",Float)=3 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha One
  ZWrite Off
  Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint; float _Softness;
   CBUFFER_END
   struct A {float4 p:POSITION; float2 uv:TEXCOORD0; float4 c:COLOR;};
   struct V {float4 p:SV_POSITION; float2 uv:TEXCOORD0; float4 c:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c*_Tint;return o;}
   half4 frag(V i):SV_Target{float r=length(i.uv*2-1);return half4(i.c.rgb,i.c.a*pow(saturate(1-r),_Softness));}
   ENDHLSL
  }
 }
}
