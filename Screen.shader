Shader "Boddle/Icon Generator/Screen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ImageSize;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            bool IsEmpty(half4 pixel)
            {
                return pixel.a < 0.09;
            }

            fixed4 Outline(fixed4 input, float2 uv)
            {
                half sizeX = (1.0 / _ImageSize.x) * _OutlineThickness;
                half sizeY = (1.0 / _ImageSize.y) * _OutlineThickness;
                if (IsEmpty(input))
                {
                    const float pi = 3.141592653589793238462;
                    const int density = 64;
                    for (int i = 0; i < density; i++)
                    {
                        float radians = (i / (float)density) * pi * 2;
                        float offsetX = cos(radians) * sizeX;
                        float offsetY = sin(radians) * sizeY;
                        half4 left = tex2D(_MainTex, float2(uv.x + offsetX, uv.y + offsetY));
                        if (!IsEmpty(left))
                        {
                            return _OutlineColor;
                        }
                    }
                }

                return input;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return Outline(col, i.uv);
            }
            ENDCG
        }
    }
}
