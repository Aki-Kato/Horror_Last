Shader "AKI/PixelationColorLimiter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(1, 512)) = 100
        _ColorLevels ("Color Levels", Range(2, 256)) = 8
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _PixelSize;
            float _ColorLevels;

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Пикселизация через округление UV-координат
                float2 uv = i.uv;
                uv.x = floor(uv.x * _PixelSize) / _PixelSize;
                uv.y = floor(uv.y * _PixelSize) / _PixelSize;

                fixed4 col = tex2D(_MainTex, uv);

                // 2. Ограничение цветов (Posterization)
                // Округляем каждый цветовой канал до ближайшего уровня
                col.rgb = floor(col.rgb * _ColorLevels) / _ColorLevels;

                return col;
            }
            ENDCG
        }
    }
}