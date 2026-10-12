// Partículas de ambiente (AmbientParticles.cs): todas en una sola malla, cada una un quad que mira a
// la cámara. El tipo va en uv0.z:
//   0 polvo  : mota redonda de borde suave
//   1 ceniza : copo irregular de borde nítido que gira y "voltea" (se estrecha y cambia de tono)
//   2 brasa  : chispa alargada en la dirección en que vuela, sin halo; se suma a la escena (aditivo)
// Datos por vértice: uv0.xy esquina (-1..1), uv0.w semilla; uv1 = (tamaño, giro, volteo, aparición);
// uv2 = velocidad en mundo (estira las chispas);
// color = color de la partícula (alpha = opacidad).
Shader "Michi/AmbientParticles"
{
    Properties
    {
        _Opacity ("Opacidad general", Range(0, 1)) = 1
        _NearFade ("Desvanecer al acercarse a la camara (unidades)", Range(0.1, 5)) = 1.5
        _EmberFlicker ("Parpadeo de las chispas", Range(0, 1)) = 0.15
        _EmberStretch ("Estiramiento de las chispas con la velocidad", Range(0, 3)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Particles"
            Tags { "LightMode" = "UniversalForward" }
            // Premultiplicado: polvo y ceniza tapan con su alpha; las brasas devuelven alpha 0 y se suman.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Opacity;
                half _NearFade;
                half _EmberFlicker;
                half _EmberStretch;
            CBUFFER_END

            struct Attributes
            {
                float3 positionWS : POSITION;   // centro de la partícula (la malla está en espacio mundo)
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float3 uv2 : TEXCOORD2;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 corner : TEXCOORD0;      // xy esquina, z tipo, w semilla
                half4 color : TEXCOORD1;
                float4 extra : TEXCOORD2;       // x volteo, y aparición, z distancia a la cámara
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float size = v.uv1.x;
                float rot = v.uv1.y;
                float tumble = v.uv1.z;

                float2 c = v.uv0.xy;
                // Copo de ceniza: al voltear se estrecha en un eje.
                float2 shaped = float2(c.x * max(abs(tumble), 0.12), c.y);
                float s, co;
                sincos(rot, s, co);
                float2 r = float2(shaped.x * co - shaped.y * s, shaped.x * s + shaped.y * co);

                float3 centerVS = TransformWorldToView(v.positionWS);
                float2 offset = r * size;
                if (v.uv0.z > 1.5)
                {
                    // Chispa: eje largo a lo largo de la velocidad vista en pantalla.
                    float2 velVS = mul((float3x3)UNITY_MATRIX_V, v.uv2).xy;
                    float speed = length(velVS);
                    float2 dir = speed > 1e-4 ? velVS / speed : float2(1, 0);
                    float2 perp = float2(-dir.y, dir.x);
                    float stretch = 1.0 + speed * _EmberStretch;
                    offset = (dir * c.x * stretch + perp * c.y) * size;
                }
                float3 posVS = centerVS + float3(offset, 0);
                o.positionCS = TransformWViewToHClip(posVS);

                o.corner = float4(c, v.uv0.z, v.uv0.w);
                o.color = v.color;
                o.extra = float4(tumble, v.uv1.w, -centerVS.z, 0);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 c = i.corner.xy;
                float r = length(c);
                int kind = (int)round(i.corner.z);
                float seed = i.corner.w;

                half fade = _Opacity * i.extra.y * saturate((i.extra.z - 0.3) / _NearFade);

                if (kind == 2)
                {
                    // Chispa: trazo fino y nítido, sin halo; parpadeo muy leve.
                    half core = smoothstep(1.0, 0.45, r);
                    half t = _Time.y;
                    half flicker = 1.0 - _EmberFlicker * (0.5 + 0.5 * sin(t * 23.0 + seed * 61.0));
                    half3 col = i.color.rgb * core * flicker * fade;
                    col = MixFogColor(col, half3(0, 0, 0), i.fogFactor);
                    return half4(col, 0);
                }

                half a;
                half3 col = i.color.rgb;
                if (kind == 1)
                {
                    // Ceniza: contorno irregular de borde nítido.
                    float ang = atan2(c.y, c.x);
                    float edge = 0.78 + 0.16 * sin(3.0 * ang + seed * 6.283) + 0.09 * sin(5.0 * ang + seed * 17.0);
                    a = smoothstep(edge, edge - 0.1, r);
                    // Una cara algo más clara que la otra: al voltear cambia de tono.
                    col *= i.extra.x >= 0 ? 1.08 : 0.8;
                }
                else
                {
                    // Polvo: mota redonda de borde suave.
                    a = smoothstep(1.0, 0.35, r);
                }

                a *= i.color.a * fade;
                if (a <= 0.003) discard;
                col *= a;
                col = MixFogColor(col, unity_FogColor.rgb * a, i.fogFactor);
                return half4(col, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
