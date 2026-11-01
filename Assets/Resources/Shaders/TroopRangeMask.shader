// Range mask of a ranged troop (TroopRangeMask, Decisions D-54). Drawn on one quad that covers every shooter's reach.
// Per pixel it evaluates each shooter's reach: a sector (pie) centred on the shooter, radius = its weapon range,
// half-angle = the front arc around the troop's facing. This is the same test as RangedRules.CanReach.
//   Fill: the union of the sectors. Its opacity and darkness grow with the fire that can reach the pixel (the sum of
//   the covering shooters' fire rates over the troop's total), so the fill is a heat map of fire concentration.
//   Outline: a band of constant screen width just inside the union's edge (signed distance, anti-aliased).
Shader "Troops/RangeMask"
{
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
            #pragma target 3.5
            #include "UnityCG.cginc"

            #define MAX_SHOOTERS 128

            float4 _Shooters[MAX_SHOOTERS]; // xy: world position, z: range, w: fire rate
            float _ShooterCount;
            float _TotalWeight;             // sum of the fire rates
            float4 _Facing;                 // xy: troop facing (unit), z: sin(half arc), w: cos(half arc)
            float4 _FillColor;
            float4 _OutlineColor;
            float _OutlinePixels;
            float _MinFillAlpha;
            float _MaxFillAlpha;
            float _HeatDarken;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 world : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            // Signed distance to a sector of radius r opening along +y with half-angle a, c = (sin a, cos a).
            // Negative inside. (Inigo Quilez, sdPie.)
            float SectorDistance(float2 p, float2 c, float r)
            {
                p.x = abs(p.x);
                float l = length(p) - r;
                float m = length(p - c * clamp(dot(p, c), 0.0, r));
                return max(l, m * sign(c.y * p.x - c.x * p.y));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 forward = _Facing.xy;
                float2 right = float2(forward.y, -forward.x);
                float pixel = max(length(float2(ddx(i.world.x), ddy(i.world.x))), 1e-6);

                float distance = 1e6;
                float heat = 0;
                int count = (int)_ShooterCount;
                [loop]
                for (int k = 0; k < count; k++)
                {
                    float4 shooter = _Shooters[k];
                    float2 d = i.world - shooter.xy;
                    float2 p = float2(dot(d, right), dot(d, forward));
                    float sd = SectorDistance(p, _Facing.zw, shooter.z);
                    distance = min(distance, sd);
                    heat += shooter.w * saturate(0.5 - sd / pixel);
                }

                float inside = saturate(0.5 - distance / pixel);
                if (inside <= 0) discard;

                float t = saturate(heat / max(_TotalWeight, 1e-5));
                float3 fill = _FillColor.rgb * lerp(1.0, 1.0 - _HeatDarken, t);
                float fillAlpha = lerp(_MinFillAlpha, _MaxFillAlpha, t);
                float outline = saturate(distance / pixel + _OutlinePixels + 0.5);
                float3 rgb = lerp(fill, _OutlineColor.rgb, outline);
                float alpha = lerp(fillAlpha, _OutlineColor.a, outline) * inside;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
