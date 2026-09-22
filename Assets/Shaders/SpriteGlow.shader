Shader "Pinball/Sprite Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (1.1, 0.2, 1.8, 1)
        _Radius ("Radius", Range(0, 1)) = 0
        _Thickness ("Thickness", Range(0.002, 0.2)) = 0.025
        _Alpha ("Alpha", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _Radius;
                float _Thickness;
                float _Alpha;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float alpha = max(0.0, _Alpha);
                if (alpha <= 0.0001)
                {
                    discard;
                }

                float dist = length(input.uv - 0.5) * 2.0;
                float halfT = max(_Thickness * 0.5, 0.0015);
                float ring = saturate(1.0 - abs(dist - _Radius) / halfT);
                ring *= ring;
                float intensity = ring * alpha;
                if (intensity <= 0.0001)
                {
                    discard;
                }

                return float4(_GlowColor.rgb * intensity, intensity);
            }
            ENDHLSL
        }
    }
}
