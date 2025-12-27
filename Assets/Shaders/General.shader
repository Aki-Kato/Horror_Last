Shader "AKI/RetroUnlit_GlobalLights"
{
    Properties
    {
        _BaseMap ("Diffuse", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)

        _DiffuseIntensity ("Diffuse Brightness", Range(0,4)) = 1
        _AmbientIntensity ("Ambient / Emissive Floor", Range(0,1)) = 0.2
        _ShadowIntensity ("Shadow Intensity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _BaseMap;
            float4 _Tint;

            float _DiffuseIntensity;
            float _AmbientIntensity;
            float _ShadowIntensity;

            int _GlobalLightCount;
            float4 _GlobalLightPos[256];
            float4 _GlobalLightDir[256];
            float4 _GlobalLightColor[256];
            float4 _GlobalLightParam[256];
            // param.x = range
            // param.y = type (0 dir, 1 point, 2 spot)
            // param.z = spot cos

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wsPos : TEXCOORD0;
                float3 wsNrm : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wsPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wsNrm = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 N = normalize(i.wsNrm);

                float3 baseCol =
                    tex2D(_BaseMap, i.uv).rgb *
                    _Tint.rgb *
                    _DiffuseIntensity;

                // --- базовая яркость (эмиссив / амбиент) ---
                float3 lighting = _AmbientIntensity;

                [loop]
                for (int l = 0; l < _GlobalLightCount; l++)
                {
                    float type = _GlobalLightParam[l].y;
                    float3 L;
                    float atten = 1;

                    if (type == 0) // directional
                    {
                        L = normalize(_GlobalLightDir[l].xyz);
                    }
                    else
                    {
                        float3 toLight = _GlobalLightPos[l].xyz - i.wsPos;
                        float dist = length(toLight);
                        if (dist > _GlobalLightParam[l].x) continue;

                        L = toLight / dist;
                        atten = saturate(1 - dist / _GlobalLightParam[l].x);

                        if (type == 2) // spot
                        {
                            float spot = dot(L, _GlobalLightDir[l].xyz);
                            if (spot < _GlobalLightParam[l].z) continue;
                            atten *= smoothstep(_GlobalLightParam[l].z, 1, spot);
                        }
                    }

                    float ndl = saturate(dot(N, L));

                    // 🔥 интенсивность тени:
                    // ndl=0 → тень (ослабляем)
                    // ndl=1 → полный свет
                    float shadowMask = lerp(1.0 - _ShadowIntensity, 1.0, ndl);

                    lighting += shadowMask * atten * _GlobalLightColor[l].rgb;
                }

                return float4(baseCol * lighting, 1);
            }
            ENDCG
        }
    }
}
