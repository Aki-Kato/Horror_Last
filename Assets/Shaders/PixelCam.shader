Shader "AKI/HDRP/PixelateFullScreen"
{
    Properties
    {
        _PixelSize   ("Pixel Size (screen px)", Float) = 6
        _PaletteR    ("Palette Steps R", Float) = 32
        _PaletteG    ("Palette Steps G", Float) = 32
        _PaletteB    ("Palette Steps B", Float) = 24
        _DitherAmp   ("Dither Amount (0..1)", Range(0,1)) = 1
        _DarkenStrength ("Darken Strength", Range(0,1)) = 0.8
        _DarkenStart01  ("Darken Start (0..1 depth)", Range(0,1)) = 0.2
        _DarkenEnd01    ("Darken End (0..1 depth)", Range(0,1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Overlay" }

        Pass
        {
            Name "PixelatePass"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            Texture2D _InputTexture;
            SamplerState sampler_InputTexture
            {
                Filter = MIN_MAG_MIP_LINEAR;
                AddressU = Clamp;
                AddressV = Clamp;
            };

            Texture2D _CameraDepthTexture;
            SamplerState sampler_CameraDepthTexture
            {
                Filter = MIN_MAG_MIP_POINT;
                AddressU = Clamp;
                AddressV = Clamp;
            };

            float4 _InputTexture_TexelSize;
            float  _PixelSize;
            float  _PaletteR;
            float  _PaletteG;
            float  _PaletteB;
            float  _DitherAmp;
            float  _DarkenStrength;
            float  _DarkenStart01;
            float  _DarkenEnd01;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 pos : SV_Position; float2 uv : TEXCOORD0; };

            Varyings Vert (Attributes i)
            {
                Varyings o;
                float2 uv = float2((i.vertexID << 1) & 2, i.vertexID & 2);
                o.uv = uv;
                o.pos = float4(uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            float Bayer4(int2 p)
            {
                int x = p.x & 3;
                int y = p.y & 3;
                const float m[16] = {
                    0,8,2,10,
                    12,4,14,6,
                    3,11,1,9,
                    15,7,13,5
                };
                return (m[y*4+x]+0.5)/16.0;
            }

            float3 LinearToSRGB(float3 c) { return pow(saturate(c), 1.0/2.2); }
            float3 SRGBToLinear(float3 c) { return pow(saturate(c), 2.2); }

            float4 Frag (Varyings i) : SV_Target
            {
                float2 uv = float2(i.uv.x, 1.0 - i.uv.y);

                float2 res = _InputTexture_TexelSize.zw;
                float2 pix = uv * res;
                float bs = max(_PixelSize, 1.0);
                pix = floor(pix / bs) * bs;
                float2 snappedUV = pix / res;

                float4 col = _InputTexture.Sample(sampler_InputTexture, snappedUV);

                float2 ipix = floor(pix);
                float d = Bayer4(int2(ipix)) * _DitherAmp;

                float3 srgb = LinearToSRGB(col.rgb);
                float rS = max(_PaletteR, 1.0);
                float gS = max(_PaletteG, 1.0);
                float bS = max(_PaletteB, 1.0);
                srgb.r = floor(srgb.r * rS + d) / rS;
                srgb.g = floor(srgb.g * gS + d) / gS;
                srgb.b = floor(srgb.b * bS + d) / bS;
                col.rgb = SRGBToLinear(srgb);

                float depth01 = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uv).r;
                float start01 = _DarkenStart01;
                float end01   = max(_DarkenEnd01, start01 + 1e-5);
                float t = saturate((depth01 - start01) / (end01 - start01));
                float darken = lerp(1.0, 1.0 - _DarkenStrength, t);
                col.rgb *= darken;

                return col;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
