Shader "Custom/Fog"
{
    Properties
    {
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius ("Inner Radius", Float) = 0.25
        _OuterRadius ("Outer Radius", Float) = 0.45
        _Color ("Fog Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            float4 _Center;
            float _InnerRadius;
            float _OuterRadius;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 diff = i.uv - _Center.xy;
                // 화면 비율을 역방향으로 보정해야 정원이 되냥
                diff.x *= _ScreenParams.x / _ScreenParams.y;
                float dist = length(diff);

                float alpha = smoothstep(_InnerRadius, _OuterRadius, dist);
                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}