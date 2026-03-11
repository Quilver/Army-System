Shader "Unlit/ProjectedTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Duration("Predicted time", Float) = 2.0
        _Steps("Simulation steps", Int) = 16//recommends 16-32 
        _Velocity("Velocity", Vector)= (1,0)
        _Rotation("Rotation", Float)= 0
        _ShapeSize("Shape Size", Float) = 0.1
    }
    //1.get spheres as Shape
    //2.for each pixel, create the line in clip space where it intersects
    //3.for steps, check if it intersects with the shape, if it does, add to the opacity and color based on the time step
    SubShader
    {
        Tags { "Queue"="Transparent", "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int _Steps;
            float _Duration;
            float2 _LinearVel;
            float _AngularVel;
            float _ShapeSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            bool contacted(float timeStep, v2f i){
                float2 pos = _LinearVel * timeStep;
                float2 samplePos = i.uv - pos;
                float dist = sdBox(samplePos, _ShapeSize);
                return dist < 0;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);
                int hits = 0;
                float lerpValue = 0;
                for(int timeStep =0; timeStep < _Duration; timeStep+=_Duration/_Steps){
                    if(!contacted(timeStep, i))continue;
                    hits++;
                    lerpValue+=timeStep;
                }
                if (hits==0) discard;
                float opacity = hits / (float)_Steps;
                lerpValue /= hits;
                fixed4 col = fixed4(1-lerpValue, 0, lerpValue, opacity);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
