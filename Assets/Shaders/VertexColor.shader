Shader "VCZ/HDRP/Unlit_VC_Simple"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base (RGB)", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1,1,1,1)

        _UseTriplanar ("Use Triplanar (0=UV,1=Tri)", Float) = 0
        _UV_Tiling    ("UV Tiling (xy), Offset (zw)", Vector) = (1,1,0,0)
        _TriTiling    ("Triplanar Tiling", Float) = 1.0
        _TriSharpness ("Triplanar Sharpness", Range(0.1,8)) = 2.0

        [HDR]_EmissionColor ("Emission Tint", Color) = (1,1,1,1)
        _EmissionStrength   ("Emission Strength", Range(0,20)) = 0

        _AlphaClip ("Alpha Clip (0=off)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags{ "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 300
        //Cull Back
        ZWrite On
        //ZTest LEqual
        //ColorMask RGBA

        Pass
        {
            Name "Forward"
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ USE_TRIPLANAR

            // ---- Без include'ов ----
            Texture2D    _BaseMap;
            SamplerState sampler_BaseMap;

            cbuffer UnityPerMaterial
            {
                float4 _BaseColor;
                float4 _BaseMap_ST;    // tiling/offset
                float4 _UV_Tiling;     // extra tiling/offset
                float  _TriTiling;
                float  _TriSharpness;
                float  _UseTriplanar;
                float4 _EmissionColor;
                float  _EmissionStrength;
                float  _AlphaClip;
            };

            // Матрицы: из правильных SRP-буферов
            cbuffer UnityPerDraw  { float4x4 unity_ObjectToWorld; float4x4 unity_WorldToObject; };
            cbuffer UnityPerView  { float4x4 unity_MatrixVP; };   // ВАЖНО: View * Projection

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
                float2 uv0    : TEXCOORD0;
            };

            struct v2f {
                float4 posHCS : SV_POSITION;
                float3 posWS  : TEXCOORD0;
                float3 nrmWS  : TEXCOORD1;
                float2 uv     : TEXCOORD2;
                float4 color  : COLOR;
            };

            float3 TransformObjectToWorld(float3 p) { return mul(unity_ObjectToWorld, float4(p,1)).xyz; }
            float4 TransformWorldToHClip (float3 p) { return mul(unity_MatrixVP, float4(p,1)); }
            float3 TransformObjectToWorldNormal(float3 n)
            {
                // приблизительно (достаточно, если нет экзотического non-uniform scale)
                float3x3 m = (float3x3)unity_ObjectToWorld;
                return normalize(mul(m, n));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 posWS = TransformObjectToWorld(v.vertex.xyz);
                o.posHCS = TransformWorldToHClip(posWS);
                o.posWS  = posWS;
                o.nrmWS  = TransformObjectToWorldNormal(v.normal);

                float2 uv = v.uv0 * _BaseMap_ST.xy + _BaseMap_ST.zw;
                uv = uv * _UV_Tiling.xy + _UV_Tiling.zw;
                o.uv = uv;

                o.color = v.color;
                return o;
            }

            float4 SampleBaseUV(float2 uv) { return _BaseMap.Sample(sampler_BaseMap, uv); }

            float4 SampleBaseTri(float3 posWS, float3 nrmWS)
            {
                float3 n = normalize(nrmWS);
                float3 w = pow(abs(n), _TriSharpness.xxx);
                w /= max(w.x + w.y + w.z, 1e-5);

                float s = max(_TriTiling, 1e-5);
                float2 uvX = posWS.zy * s;
                float2 uvY = posWS.xz * s;
                float2 uvZ = posWS.xy * s;

                float4 cx = _BaseMap.Sample(sampler_BaseMap, uvX);
                float4 cy = _BaseMap.Sample(sampler_BaseMap, uvY);
                float4 cz = _BaseMap.Sample(sampler_BaseMap, uvZ);
                return cx * w.x + cy * w.y + cz * w.z;
            }

            float4 frag(v2f i) : SV_Target
            {
                bool useTri = false;
                #if defined(USE_TRIPLANAR)
                    useTri = true;
                #endif
                if (_UseTriplanar >= 0.5) useTri = true;

                float4 texC    = useTri ? SampleBaseTri(i.posWS, i.nrmWS) : SampleBaseUV(i.uv);
                float4 baseCol = texC * i.color * _BaseColor;

                //if (_AlphaClip > 0 && baseCol.a < _AlphaClip) discard;

                float3 emissive = baseCol.rgb * _EmissionColor.rgb * _EmissionStrength;

                // Жёстко непрозрачный вывод
                return float4(baseCol.rgb + emissive, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
