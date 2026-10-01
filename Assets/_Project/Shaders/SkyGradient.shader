// Cartoon sky used as the scene skybox: a screen-space vertical gradient (zenith -> bright horizon band -> ground haze).
// Screen space on purpose: the camera only looks straight ahead and the reference sky reads as a flat backdrop.
// Colours are set at runtime from a SkyPreset (GodTower.Environment.SkyView).
Shader "GodTower/Sky Gradient"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.10, 0.38, 0.88, 1)
        _HorizonColor ("Horizon", Color) = (0.62, 0.85, 1, 1)
        _GroundColor ("Ground", Color) = (0.32, 0.62, 0.95, 1)
        _HorizonHeight ("Horizon Height", Range(0, 1)) = 0.3
        _HorizonSpread ("Horizon Spread", Range(0.05, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                float _HorizonHeight;
                float _HorizonSpread;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float y = saturate(input.screenPos.y / input.screenPos.w);
                float above = saturate((y - _HorizonHeight) / _HorizonSpread);
                float below = saturate((_HorizonHeight - y) / (_HorizonSpread * 0.6));

                // Ease-out towards the zenith keeps a wide saturated top like the reference.
                half3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, 1.0 - (1.0 - above) * (1.0 - above));
                color = lerp(color, _GroundColor.rgb, below * below);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
