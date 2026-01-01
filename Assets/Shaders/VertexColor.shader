Shader "AKI/Stylized_Emissive_VertexColor"
{
    Properties
    {
        _EmissiveTex ("Emissive Texture (sRGB)", 2D) = "white" {}
        _EmissiveTint ("Emissive Tint", Color) = (1,1,1,1)
        _EmissiveBrightness ("Emissive Brightness", Range(0,4)) = 1

        _LightInfluence ("Light Influence", Range(0,1)) = 0.8

        [Header(Stylized Shadow)]
        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.5
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        sampler2D _EmissiveTex;
        float4 _EmissiveTex_ST;
        float4 _EmissiveTint;
        float _EmissiveBrightness;
        float _LightInfluence;
        float4 _ShadowColor;
        float _ShadowStrength;
        float _ShadowThreshold;

        struct appdata {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        struct v2f {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 worldPos : TEXCOORD1;
            float3 worldNorm : TEXCOORD2;
            float3 vcol : TEXCOORD3;
            SHADOW_COORDS(4)
        };

        v2f vert (appdata v) {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _EmissiveTex);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.worldNorm = UnityObjectToWorldNormal(v.normal);
            o.vcol = v.color.rgb;
            TRANSFER_SHADOW(o);
            return o;
        }
        ENDCG

        // --- FORWARD BASE ---
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBase
            #pragma multi_compile_fwdbase

            float4 fragBase (v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNorm);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);

                // 1. Стилизованная маска тени (NdotL + тени от объектов)
                float ndl = dot(N, L) * 0.5 + 0.5;
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                float lightMask = smoothstep(_ShadowThreshold - 0.02, _ShadowThreshold + 0.02, ndl * atten);

                // 2. Базовый эмиссив с вертекс-колором
                float3 baseEmissive = tex2D(_EmissiveTex, i.uv).rgb * _EmissiveTint.rgb * _EmissiveBrightness * i.vcol;

                // 3. Освещение
                float3 ambient = ShadeSH9(float4(N, 1.0));
                float3 totalLight = ambient + _LightColor0.rgb * lightMask;

                // 4. Смешивание (как в вашем оригинале, но с новой маской)
                // litBase - когда объект на свету
                float3 litBase = baseEmissive * lerp(1.0, totalLight, _LightInfluence);
                
                // shadowBase - когда объект в тени (умножаем на ShadowColor)
                float3 shadowBase = litBase * _ShadowColor.rgb;

                // Финальный lerp по силе тени
                float3 finalShadow = lerp(litBase, shadowBase, _ShadowStrength);
                float3 finalCol = lerp(finalShadow, litBase, lightMask);

                return float4(finalCol, 1);
            }
            ENDCG
        }

        // --- FORWARD ADD ---
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAdd
            #pragma multi_compile_fwdadd_fullshadows

            float4 fragAdd (v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNorm);
                float3 L = ( _WorldSpaceLightPos0.w == 0 ) ? normalize(_WorldSpaceLightPos0.xyz) : normalize(_WorldSpaceLightPos0.xyz - i.worldPos);

                float ndl = dot(N, L) * 0.5 + 0.5;
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                float lightMask = smoothstep(_ShadowThreshold - 0.02, _ShadowThreshold + 0.02, ndl * atten);

                float3 baseEmissive = tex2D(_EmissiveTex, i.uv).rgb * _EmissiveTint.rgb * _EmissiveBrightness * i.vcol;
                
                // Для доп. источников считаем только добавку света
                float3 addLight = _LightColor0.rgb * lightMask;
                return float4(baseEmissive * addLight * _LightInfluence, 1);
            }
            ENDCG
        }

        // --- SHADOW CASTER ---
        UsePass "VertexLit/SHADOWCASTER"
    }
    FallBack "Diffuse"
}