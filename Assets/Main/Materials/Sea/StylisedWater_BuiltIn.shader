Shader "Custom/StylisedWater_BuiltIn"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.207843, 0.643137, 1.0, 1.0)
        _DarkFoamColor ("Dark Foam Color", Color) = (0.181239, 0.560816, 0.872000, 1.0)
        _LightFoamColor ("Light Foam Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1.0


        _FoamTex ("Foam Texture", 2D) = "white" {}
        _FlowMap ("Flow Texture", 2D) = "bump" {}


        _Size ("Size (Tiling)", Float) = 2.0
        _FlowSpeed ("Flow Speed", Float) = 0.01
        _FlowStrength ("Flow Strength", Range(0,1)) = 0.0075
        _FoamDistance ("Foam Distance", Float) = 1.5
        _Choppiness ("Choppiness (Vertex Wave)", Float) = 0.01

        // Light foam (depthless variant)
        _LightFoamScale ("Light Foam Scale", Float) = 2.0
        _LightFoamSpeed ("Light Foam Speed", Float) = 1.0
        _LightFoamStrength ("Light Foam Strength", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        GrabPass { } // optional (not used here, but keeps compatibility with some pipelines)

        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"


            sampler2D _FoamTex;
            sampler2D _FlowMap;
            float4 _FoamTex_ST;
            float4 _FlowMap_ST;


            fixed4 _WaterColor;
            fixed4 _DarkFoamColor;
            fixed4 _LightFoamColor;


            float _Size;
            float _FlowSpeed;
            float _FlowStrength;
            float _FoamDistance;
            float _Choppiness;
            fixed _Opacity;

            // Light foam (depthless) controls
            float _LightFoamScale;
            float _LightFoamSpeed;
            float _LightFoamStrength;


            // Depth texture from camera (Built-in)
            // UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };


            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uvFoam : TEXCOORD0;
                float2 uvFlow : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };


            float2 UnpackFlowXY(float4 nrmSample)
            {
                // UnpackNormal gives xy in -1..1 and assumes z from those;
                float3 n = UnpackNormal(nrmSample);
                return n.xy;
            }

            v2f vert (appdata v)
            {
                v2f o;

                // World space for simple vertical displacement
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Simple choppy sine using X+Z and time (matches ShaderGraph's sin(x+z + t) * _Choppiness)
                float wave = sin((wp.x + wp.z) + _Time.y) * _Choppiness;

                // Displace upward (Y). If your water mesh isn't horizontal, consider displacing along normal instead.
                wp.y += wave;

                // Back to clip space
                float4 newVertex = mul(unity_WorldToObject, float4(wp, 1.0));
                o.pos = UnityObjectToClipPos(newVertex);

                // Base UVs with tiling
                float2 baseUV = TRANSFORM_TEX(v.uv, _FoamTex) * _Size;

                // Flow UVs are driven by a normal/flow map
                float2 flowUV = TRANSFORM_TEX(v.uv, _FlowMap) * _Size;

                // Animate flow: offset strength scales with time * speed
                float t = _Time.y * _FlowSpeed;

                // Precompute and pass to fragment
                o.uvFoam = baseUV;
                o.uvFlow = flowUV + t; // phase only; actual vector offset is computed in frag from map contents
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = wp;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample flow map and convert to a 2D vector
                float2 flowXY = UnpackFlowXY(tex2D(_FlowMap, i.uvFlow)) * _FlowStrength;

                // Distort foam UVs using the flow vector
                float2 foamUV = i.uvFoam + flowXY;

                // Foam pattern
                fixed foamVal = tex2D(_FoamTex, foamUV).r;

                // Base water / dark foam blend by foam noise
                fixed3 baseCol = lerp(_WaterColor.rgb, _DarkFoamColor.rgb, foamVal);

                // Edge foam using scene depth vs current fragment depth
                // #if defined(UNITY_REVERSED_Z)
                // Built-in forward doesn't use reversed Z, but guard anyway
                // #endif
                // float scene01 = Linear01Depth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                // float frag01 = Linear01Depth(i.screenPos.z / i.screenPos.w);

                // Positive when water pixel is in front of background geometry (near shore / intersections)
                // float edge = saturate((frag01 - scene01) / max(_FoamDistance, 1e-5));
                // float edge = saturate((scene01 - frag01) / max(_FoamDistance, 1e-5));

                // Final foam-tinted color
                // fixed3 color = lerp(baseCol, _LightFoamColor.rgb, edge);
                // fixed3 color = lerp(baseCol, _LightFoamColor.rgb, foamVal);

                // return fixed4(color, _Opacity);


                // ---- Light foam (depthless variant) ----
                // Phase-shifted / scaled second sampling of the same foam texture
                float t = _Time.y * _LightFoamSpeed;
                float2 foamUV2 = i.uvFoam * _LightFoamScale + flowXY + float2(t, -t);
                fixed lightFoam = tex2D(_FoamTex, foamUV2).r;


                fixed3 color = lerp(baseCol, _LightFoamColor.rgb, lightFoam * _LightFoamStrength);
                return fixed4(color, _Opacity);
            }
            ENDCG
        }
    }
    FallBack Off
}
