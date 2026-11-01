// Dead soldiers (TroopSoldierViews): the same sprite and emission mask as Unlit/ModelShader, but alpha blended so the
// corpse can fade, and the mask drawn in _Color (black: no emission). The vertex colour (SpriteRenderer.color) carries
// the desaturated army colour and the fading alpha.
Shader "Unlit/ModelCorpseShader"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Emission ("Emission Mask", 2D) = "black" {}
        _Color("Mask Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex, _Emission;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                fixed4 col = texel * i.color;
                if (tex2D(_Emission, i.uv).r > 0)
                    col.rgb = _Color.rgb;
                col.a = texel.a * i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
