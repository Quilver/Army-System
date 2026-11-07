// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/ModelShader"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Emission ("Texture", 2D) = "white" {}
        [HDR]_Color("Color", Color) = (0, 32, 4.592667, 1)

        _HitForce("_HitForce", Vector) = (0,0,0,0)

        // Hit squash and stretch (set per soldier by TroopSoldierViews through a property block).
        _HitDir("Hit Push Direction (world xy)", Vector) = (0,1,0,0)
        _Deform("Squash (-1) / Stretch (+1)", Range(-1, 1)) = 0
        _SquashAmount("Squash Amount", Range(0, 1)) = 0.8
        _StretchAmount("Stretch Amount", Range(0, 1)) = 0.7
        _TaperAmount("Stretch Taper", Range(0, 1)) = 0.8
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
            float4 _HitDir;
            float _Deform, _SquashAmount, _StretchAmount, _TaperAmount;

            // Squash and stretch along the push direction. In object space the sprite spans about -0.5..0.5, so
            // a = -1 is the front (toward the attacker) and a = +1 the back. Squash (_Deform < 0): the front is pushed
            // in toward the back and the sides bulge a little. Stretch (_Deform > 0): the back stretches out away from
            // the attacker and widens while the front narrows, giving a tapered (triangle-like) shape.
            float4 SquashStretch(float4 vertex)
            {
                if (abs(_Deform) < 0.0001) return vertex;
                float2 h = mul((float3x3)unity_WorldToObject, float3(_HitDir.xy, 0)).xy;
                float lengthH = length(h);
                if (lengthH < 0.00001) return vertex;
                h /= lengthH;
                float2 l = float2(-h.y, h.x);
                float2 p = vertex.xy * 2.0;
                float a = dot(p, h);
                float s = dot(p, l);
                if (_Deform < 0)
                {
                    float q = -_Deform;
                    a = 1.0 - (1.0 - a) * (1.0 - q * _SquashAmount);
                    s *= 1.0 + q * _SquashAmount * 0.5;
                }
                else
                {
                    float k = _Deform;
                    float a0 = a;
                    a = a0 + k * _StretchAmount * (a0 + 1.0);
                    s *= 1.0 + k * _TaperAmount * a0;
                }
                vertex.xy = (h * a + l * s) * 0.5;
                return vertex;
            }
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
                UnityObjectToClipPos(SquashStretch(v.vertex));
                

                
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
