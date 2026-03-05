// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/ModelShader"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Emission ("Texture", 2D) = "white" {}
        [HDR]_Color("Color", Color) = (0, 32, 4.592667, 1)

        _HitForce("_HitForce", Vector) = (0,0,0,0)
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct shapeData
            {
                float4 vertex : POSITION;
                float4 color    : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color    : COLOR;

            };

            sampler2D _MainTex, _Emission;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _HitForce;
            float4 HitData(float4 vertex){

                if(length(_HitForce) > 0){
                    float4 avoidPoint = mul(unity_ObjectToWorld, float4(0.0,0.0,0.0,1.0) ) + normalize(_HitForce) * 1.5;
                    float4 dir = UnityObjectToClipPos(vertex) - avoidPoint;
                    return UnityObjectToClipPos(vertex) + dir/length(dir);
                    if(length(dir) < 1){
                        vertex += dir/length(dir);
                    }
                }
                return UnityObjectToClipPos(vertex);
            }
            v2f vert (shapeData v)
            {
                v2f o;
                o.vertex = //HitData(v.vertex);
                UnityObjectToClipPos(v.vertex);
                

                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                if(tex2D(_Emission, i.uv).r > 0)
                    col = _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
