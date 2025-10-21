Shader "Hidden/SpriteCropNDI"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
        Cull Off ZWrite Off ZTest Always
        Blend One Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;    // (1/w,1/h,w,h)
            float2 _UVOffset;             // (x,y)
            float2 _UVScale;              // (sx,sy)

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _UVScale + _UVOffset;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                return c; // 알파 포함 그대로
            }
            ENDHLSL
        }
    }
        Fallback Off
}
