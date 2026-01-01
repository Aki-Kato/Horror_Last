Shader "AKI/Stylized_AllLights_Final"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,4)) = 1
        
        [Header(Stylized Shadow)]
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.3, 1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5 

        [Toggle(_USE_TRIPLANAR)] _USE_TRIPLANAR ("Use Triplanar", Float) = 0
        _TriplanarScale ("Triplanar Scale", Float) = 1
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "AutoLight.cginc"
    #include "Lighting.cginc"

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _Tint;
    float _Brightness;
    float4 _ShadowColor;
    float _ShadowStrength;
    float _ShadowThreshold;
    float _TriplanarScale;

    struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
    };

    struct v2f {
        float4 pos : SV_POSITION;
        float3 worldNormal : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float2 uv : TEXCOORD3;
        SHADOW_COORDS(4)
    };

    float3 CalculateAlbedo(v2f i) {
        #if _USE_TRIPLANAR
            float3 blending = abs(normalize(i.worldNormal));
            blending /= (blending.x + blending.y + blending.z + 1e-5);
            float3 p = i.worldPos * _TriplanarScale;
            float3 xaxis = tex2D(_MainTex, p.yz).rgb;
            float3 yaxis = tex2D(_MainTex, p.xz).rgb;
            float3 zaxis = tex2D(_MainTex, p.xy).rgb;
            return xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;
        #else
            return tex2D(_MainTex, i.uv).rgb;
        #endif
    }

    v2f vert (appdata v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.worldNormal = UnityObjectToWorldNormal(v.normal);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        TRANSFER_SHADOW(o);
        return o;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // --- PASS 1: DIRECTIONAL LIGHT + AMBIENT ---
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile _ _USE_TRIPLANAR

            float4 frag (v2f i) : SV_Target {
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                
                float ndl = dot(N, L) * 0.5 + 0.5;
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                
                // --- ДОБАВЛЕННЫЙ БЛОК: Фикс дистанции теней ---
                // Вычисляем расстояние от камеры до объекта
                float dist = distance(i.worldPos, _WorldSpaceCameraPos);
                // shadowDistance — это системная переменная Unity (расстояние из Quality Settings)
                // Создаем коэффициент затухания аппаратной тени
                float shadowFade = saturate(dist / _ProjectionParams.z); // Очень грубое приближение
                // Если стандартный atten равен 1 из-за дистанции, 
                // мы подмешиваем ndl, чтобы имитировать тень даже вдали
                float stylizedAtten = min(atten, smoothstep(0.4, 0.6, ndl));
                // ----------------------------------------------

                // Резкая маска тени (используем наш новый stylizedAtten вместо atten)
                float lightMask = smoothstep(_ShadowThreshold - 0.02, _ShadowThreshold + 0.02, ndl * stylizedAtten);
                
                float3 albedo = CalculateAlbedo(i) * _Tint.rgb * _Brightness;
                float3 ambient = ShadeSH9(float4(N, 1.0)) * albedo;

                float3 litPart = albedo * _LightColor0.rgb;
                float3 shadowPart = albedo * _ShadowColor.rgb;
                
                float3 finalShadow = lerp(litPart, shadowPart, _ShadowStrength);
                float3 result = lerp(finalShadow, litPart, lightMask) + ambient;

                return float4(result, 1.0);
            }
            ENDCG
        }

        // --- PASS 2: POINT / SPOT LIGHTS ---
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile _ _USE_TRIPLANAR

            float4 frag (v2f i) : SV_Target {
                float3 N = normalize(i.worldNormal);
                
                #if defined(POINT) || defined(SPOT) || defined(POINT_COOKIE)
                    float3 L = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                #else
                    float3 L = normalize(_WorldSpaceLightPos0.xyz);
                #endif

                float ndl = dot(N, L) * 0.5 + 0.5;
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                // Те же пороги для стилизации Point-лайтов
                float lightMask = smoothstep(_ShadowThreshold - 0.02, _ShadowThreshold + 0.02, ndl * atten);
                
                float3 albedo = CalculateAlbedo(i) * _Tint.rgb * _Brightness;
                
                // Для дополнительных источников мы не добавляем ShadowColor (иначе сцена пересветится),
                // мы просто отсекаем их свет по нашей стилизованной маске.
                return float4(albedo * _LightColor0.rgb * lightMask, 1.0);
            }
            ENDCG
        }

        UsePass "VertexLit/SHADOWCASTER"
    }
    Fallback "Diffuse"
}