Shader "AKI/SimpleBillboardSprite_Unlit"
{
    Properties
    {
        _MainTex ("Diffuse", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)
        _Scale   ("Scale", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        LOD 100

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            float     _Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // Центр объекта в view-пространстве
                float4 centerVS = mul(UNITY_MATRIX_MV, float4(0,0,0,1));

                // Смещение вершин в плоскости экрана (XY view-пространства)
                float2 offset = v.vertex.xy * _Scale;

                centerVS.xy += offset;

                // Проекция во фрагментный шейдер
                o.vertex = mul(UNITY_MATRIX_P, centerVS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                return c;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
