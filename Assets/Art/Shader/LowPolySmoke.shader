// Humo estilizado (cartoon) para URP: bocanadas lisas con sombreado en dos tonos de corte nítido
// (cara iluminada clara, sombra gris), que en vez de volverse transparentes se DISUELVEN.
// La disolución no hace agujeros al azar: se come primero los bordes (lo que la cámara ve de canto)
// y la parte de arriba, y el núcleo aguanta hasta el final, así la bocanada se deshilacha de fuera
// hacia dentro. El ruido sube con el humo. Por los huecos se ve el interior, sombreado, no vacío.
// Son opacas y escriben profundidad, así las bocanadas que se tocan se funden en una sola masa.
// Llevan línea de contorno oscura (casco invertido, grosor fijo en píxeles, como Michi/Toon); como
// cada bocanada es un racimo de bultos, la línea también marca los bultos que se solapan.
// Las mallas y los valores por bocanada los pone LowPolySmokeEmitter.cs:
//   _Alpha     1 = entera, 0 = disuelta del todo
//   _PuffColor tono de la bocanada (blanca, gris clara, gris oscura...)
//   _GlowFade  resplandor desde abajo (solo las bocanadas jóvenes)
//   _Seed      desplaza el ruido para que cada bocanada se disuelva distinto
Shader "Michi/StylizedSmoke"
{
    Properties
    {
        [Header(Color)]
        _Color ("Tinte general (multiplica el tono de cada bocanada)", Color) = (1, 1, 1, 1)
        _ShadeColor ("Tinte de la sombra (multiplica)", Color) = (0.6, 0.6, 0.68, 1)
        _ShadeThreshold ("Umbral luz-sombra", Range(0, 1)) = 0.48
        _ShadeSmooth ("Suavidad del corte", Range(0.001, 0.3)) = 0.03
        _CoreDarken ("Oscurecer la parte de abajo", Range(0, 1)) = 0.25
        _InnerDarken ("Interior visto por los huecos (multiplica la sombra)", Range(0, 1)) = 0.8
        [HDR] _RimColor ("Brillo de contorno", Color) = (0.12, 0.12, 0.14, 1)
        _RimAmount ("Umbral del contorno", Range(0, 1)) = 0.75
        [HDR] _GlowColor ("Resplandor desde abajo", Color) = (1.0, 0.45, 0.14, 1)
        _GlowStrength ("Fuerza del resplandor", Range(0, 3)) = 0.3

        [Header(Contorno)]
        _OutlineColor ("Color de la linea", Color) = (0.05, 0.05, 0.07, 1)
        _OutlineWidth ("Grosor en pixeles", Range(0, 8)) = 2.5
        _OutlineDepthOffset ("Empujar la linea hacia atras (unidades)", Range(0, 0.3)) = 0.06

        [Header(Disolucion)]
        _DissolveScale ("Tamano de las mordidas (mas alto = mas pequenas)", Range(0.5, 12)) = 3
        _DissolveScroll ("Velocidad a la que sube el ruido", Range(0, 2)) = 0.35
        _RimErode ("Se come antes los bordes", Range(0, 2)) = 0.9
        _TopErode ("Se come antes la parte de arriba", Range(0, 2)) = 0.5
        _EdgeColor ("Tinte del filo al disolverse (multiplica)", Color) = (0.8, 0.83, 0.95, 1)
        _EdgeWidth ("Grosor del filo", Range(0, 0.15)) = 0.04

        [Header(Movimiento)]
        _Wobble ("Deformacion", Range(0, 0.3)) = 0.06
        _WobbleSpeed ("Velocidad de deformacion", Range(0, 3)) = 0.7

        [HideInInspector] _Alpha ("Cuanto queda de esta bocanada", Range(0, 1)) = 1
        [HideInInspector] _GlowFade ("Resplandor de esta bocanada", Range(0, 1)) = 1
        [HideInInspector] _PuffColor ("Tono de esta bocanada", Color) = (0.9, 0.9, 0.9, 1)
        [HideInInspector] _Seed ("Semilla del ruido", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _ShadeColor;
            half4 _RimColor;
            half4 _GlowColor;
            half4 _EdgeColor;
            half4 _PuffColor;
            half4 _OutlineColor;
            float4 _Seed;
            half _OutlineWidth;
            half _OutlineDepthOffset;
            half _ShadeThreshold;
            half _ShadeSmooth;
            half _CoreDarken;
            half _InnerDarken;
            half _RimAmount;
            half _GlowStrength;
            half _DissolveScale;
            half _DissolveScroll;
            half _RimErode;
            half _TopErode;
            half _EdgeWidth;
            half _Wobble;
            half _WobbleSpeed;
            half _Alpha;
            half _GlowFade;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
        };

        float hash31(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float noise3(float3 x)
        {
            float3 i = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(
                lerp(lerp(hash31(i + float3(0, 0, 0)), hash31(i + float3(1, 0, 0)), f.x),
                     lerp(hash31(i + float3(0, 1, 0)), hash31(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(hash31(i + float3(0, 0, 1)), hash31(i + float3(1, 0, 1)), f.x),
                     lerp(hash31(i + float3(0, 1, 1)), hash31(i + float3(1, 1, 1)), f.x), f.y),
                f.z);
        }

        // Bulto suave animado a lo largo de la normal (el ruido es continuo, la malla no se abre).
        float3 SmokePosition(Attributes v)
        {
            float w = noise3(v.positionOS.xyz * 3.0 + _Seed.xyz + _Time.y * _WobbleSpeed) - 0.5;
            return v.positionOS.xyz + v.normalOS * (w * 2.0 * _Wobble);
        }

        // Cuánto "de canto" ve la cámara este punto: 0 de frente, 1 en la silueta.
        float RimFactor(float3 normalWS, float3 positionWS)
        {
            float3 viewDir = normalize(GetWorldSpaceViewDir(positionWS));
            return 1.0 - abs(dot(normalize(normalWS), viewDir));
        }

        // > 0: este punto sigue; < 0: ya se lo ha comido la disolución.
        // Los bordes y la parte de arriba tienen un umbral más alto, así desaparecen antes y la
        // bocanada se deshilacha de fuera hacia dentro en vez de llenarse de agujeros.
        float DissolveMask(float3 positionOS, float rim)
        {
            float3 q = positionOS * _DissolveScale + _Seed.xyz;
            q.y -= _Time.y * _DissolveScroll;
            float n = noise3(q) * 0.6 + noise3(q * 2.17 + 17.0) * 0.28 + noise3(q * 4.9 + 41.0) * 0.12;

            float erosion = 1.0 - _Alpha;
            float height = saturate(positionOS.y + 0.5);
            float bias = _RimErode * rim * rim + _TopErode * height;
            float cut = erosion * (1.15 + bias) - 0.06;
            return n - cut;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            // Se dibujan también las caras de dentro: es lo que se ve a través de los huecos.
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionOS = SmokePosition(v);
                VertexPositionInputs p = GetVertexPositionInputs(positionOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                bool front = IS_FRONT_VFACE(face, true, false);
                float rim = RimFactor(i.normalWS, i.positionWS);
                float mask = DissolveMask(i.positionOS, rim);
                clip(mask);

                float3 n = normalize(i.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));

                half3 lit = _PuffColor.rgb * _Color.rgb;
                half3 shade = lit * _ShadeColor.rgb;
                half3 col;
                half litAmount = 0;

                if (front)
                {
                    Light mainLight = GetMainLight();
                    // Dos tonos con corte nítido.
                    half h = dot(n, mainLight.direction) * 0.5h + 0.5h;
                    litAmount = smoothstep(_ShadeThreshold - _ShadeSmooth, _ShadeThreshold + _ShadeSmooth, h);
                    // La sombra se oscurece un poco más hacia abajo, como el núcleo denso del humo.
                    half3 coreShade = shade * (1.0h - _CoreDarken * saturate(-n.y));
                    col = lerp(coreShade, lit, litAmount);

                    half fresnel = 1.0h - saturate(dot(n, viewDir));
                    col += _RimColor.rgb * smoothstep(_RimAmount - 0.03h, _RimAmount + 0.03h, fresnel) * litAmount;

                    half low = saturate(0.5h - i.positionOS.y);
                    col += _GlowColor.rgb * (low * low * _GlowStrength * _GlowFade * saturate(-n.y + 0.3h));
                }
                else
                {
                    // Interior: un tono liso de sombra, algo más oscuro, que da volumen al hueco.
                    col = shade * _InnerDarken;
                }

                // Filo donde la disolución está mordiendo: el mismo color, algo más frío y oscuro.
                if (_Alpha < 0.999h && mask < _EdgeWidth)
                    col *= _EdgeColor.rgb;

                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ Línea de contorno
        Pass
        {
            Name "Outline"
            // URP lo dibuja después del pase UniversalForward del mismo objeto.
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(SmokePosition(v));
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = positionWS;
                o.normalWS = normalWS;

                // Un poco hacia atrás: así el interior que se ve por los huecos tapa la línea.
                positionWS -= normalize(GetWorldSpaceViewDir(positionWS)) * _OutlineDepthOffset;
                float4 positionCS = TransformWorldToHClip(positionWS);

                // Empuja hacia fuera un número fijo de píxeles en pantalla.
                float2 dirPx = mul((float3x3)GetWorldToHClipMatrix(), normalWS).xy * _ScreenParams.xy;
                dirPx /= max(length(dirPx), 1e-5);
                positionCS.xy += dirPx * (2.0 * _OutlineWidth / _ScreenParams.xy) * positionCS.w;

                o.positionCS = positionCS;
                o.positionOS = v.positionOS.xyz;
                o.fogFactor = ComputeFogFactor(positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // La línea se disuelve con la bocanada.
                clip(DissolveMask(i.positionOS, RimFactor(i.normalWS, i.positionWS)));
                return half4(MixFog(_OutlineColor.rgb, i.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(SmokePosition(v));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half frag(Varyings i) : SV_Target
            {
                clip(DissolveMask(i.positionOS, RimFactor(i.normalWS, i.positionWS)));
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(SmokePosition(v));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                clip(DissolveMask(i.positionOS, RimFactor(i.normalWS, i.positionWS)));
                float3 n = normalize(i.normalWS);
                n = IS_FRONT_VFACE(face, n, -n);
                return half4(n, 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
