// Toon shader para URP (Unity 6, Forward+): luz en bandas planas con color de sombra propio,
// brillo especular recortado, luz de contorno (rim) y linea de contorno por casco invertido.
// La luz total se limita a _MaxBrightness para que las zonas planas no superen el umbral del bloom
// de la escena (si no, el personaje y el arma "brillan" como si fueran metal pulido).
// Usa los mismos nombres de textura que URP/Lit (_BaseMap, _BumpMap, _OcclusionMap), asi que
// al cambiar un material de Lit a este shader conserva sus texturas.
// Pases: ForwardLit (color), Outline (contorno), ShadowCaster, DepthOnly y DepthNormals (para el SSAO).
Shader "Michi/Toon"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap ("Textura base", 2D) = "white" {}
        [MainColor] _BaseColor ("Color base", Color) = (1, 1, 1, 1)
        [Normal] [NoScaleOffset] _BumpMap ("Normal map", 2D) = "bump" {}
        _BumpScale ("Fuerza del normal map", Range(0, 2)) = 1
        [NoScaleOffset] _OcclusionMap ("Oclusion (AO)", 2D) = "white" {}
        _OcclusionStrength ("Oclusion: empuja los huecos a sombra", Range(0, 1)) = 0.5
        [HDR] _EmissionColor ("Emision", Color) = (0, 0, 0, 1)
        [NoScaleOffset] _EmissionMap ("Mapa de emision", 2D) = "white" {}

        [Header(Sombreado toon)]
        _ShadeColor ("Color de la sombra", Color) = (0.42, 0.4, 0.58, 1)
        _ShadeThreshold ("Umbral luz-sombra", Range(0, 1)) = 0.5
        _ShadeSmooth ("Suavidad del corte", Range(0.001, 0.5)) = 0.02
        [IntRange] _Bands ("Numero de bandas", Range(1, 4)) = 1
        _ShadowStrength ("Sombras recibidas", Range(0, 1)) = 1
        _AmbientStrength ("Luz ambiente", Range(0, 2)) = 0.6
        _AddLightStrength ("Luces secundarias", Range(0, 1)) = 0.5
        _AddLightSmooth ("Suavidad de las luces secundarias", Range(0.001, 0.5)) = 0.2
        _MaxBrightness ("Brillo maximo (por debajo del bloom)", Range(0.5, 3)) = 0.95

        [Header(Brillo)]
        [HDR] _ToonSpecColor ("Color del brillo", Color) = (0, 0, 0, 1)
        _SpecSize ("Tamano del brillo", Range(0, 1)) = 0.1
        _SpecSmooth ("Suavidad del brillo", Range(0.001, 0.5)) = 0.02

        [Header(Luz de contorno)]
        [HDR] _RimColor ("Color del rim", Color) = (0, 0, 0, 1)
        _RimAmount ("Umbral del rim", Range(0, 1)) = 0.7
        _RimSmooth ("Suavidad del rim", Range(0.001, 0.5)) = 0.03

        [Header(Linea de contorno)]
        _OutlineColor ("Color de la linea", Color) = (0.08, 0.06, 0.1, 1)
        _OutlineWidth ("Grosor en pixeles", Range(0, 10)) = 2
        _OutlineTexMix ("Tenir la linea con la textura", Range(0, 1)) = 0.5
        // Silueta: la linea solo se pinta fuera del personaje. Completo: tambien en nariz, brazos, etc.
        [Enum(Silueta,6,Completo,8)] _OutlineStencilComp ("Donde se dibuja la linea", Float) = 6

        [Header(Otros)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Caras que se dibujan", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Todas las propiedades en un unico bloque e identico en todos los pases: compatible con SRP Batcher.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _ShadeColor;
            half4 _ToonSpecColor;
            half4 _RimColor;
            half4 _OutlineColor;
            half _BumpScale;
            half _OcclusionStrength;
            half _ShadeThreshold;
            half _ShadeSmooth;
            half _Bands;
            half _ShadowStrength;
            half _AmbientStrength;
            half _AddLightStrength;
            half _AddLightSmooth;
            half _MaxBrightness;
            half _OutlineTexMix;
            half _SpecSize;
            half _SpecSmooth;
            half _RimAmount;
            half _RimSmooth;
            half _OutlineWidth;
            half _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);
        TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
        ENDHLSL

        // ------------------------------------------------------------------ Color
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            // Marca en el stencil todos los pixeles que ocupa el personaje (cuerpo y arma comparten marca).
            Stencil
            {
                Ref 128
                WriteMask 128
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;   // w = signo de la bitangente
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.tangentWS = float4(nrm.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            // Convierte una iluminacion continua (0..1, con el corte en 0.5) en _Bands escalones planos.
            half ToonRamp(half x, half smoothness)
            {
                half bands = max(_Bands, 1.0);
                half v = saturate(x) * bands;
                half whole = floor(v);
                // El ancho del corte nunca baja de un pixel de pantalla (fwidth): borde sin dientes de sierra.
                half s = clamp(max(smoothness * bands, fwidth(v)), 1e-4, 0.5);
                half t = smoothstep(0.5 - s, 0.5 + s, v - whole);
                return saturate((whole + t) / bands);
            }

            // Luces secundarias (segunda direccional, puntuales, focos): corte mas suave que la principal para que
            // no se crucen dos bordes duros de sombra, y atenuadas con _AddLightStrength.
            half3 ToonAdditionalLight(Light light, half3 normalWS)
            {
                half halfLambert = dot(normalWS, light.direction) * 0.5 + 0.5;
                half ramp = ToonRamp(halfLambert - _ShadeThreshold + 0.5, _AddLightSmooth);
                half atten = light.distanceAttenuation * lerp(1.0, light.shadowAttenuation, _ShadowStrength);
                return light.color * (ramp * atten * _AddLightStrength);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;

                // Normal en espacio de mundo a partir del normal map.
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                float3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS));
                normalWS = NormalizeNormalPerPixel(normalWS);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half4 shadowMask = half4(1, 1, 1, 1);

                // La oclusion horneada no ensucia el color: solo adelanta la sombra en los huecos.
                half ao = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g, _OcclusionStrength);

                // Luz principal: half-lambert escalonado.
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                half shadow = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                half ndotl = dot(normalWS, mainLight.direction);
                // La sombra recibida se aplica despues del escalonado, con su penumbra suave: si se escalona
                // tambien, se ven los texels del mapa de sombras como escalones.
                half ramp = ToonRamp((ndotl * 0.5 + 0.5) * ao - _ShadeThreshold + 0.5, _ShadeSmooth) * shadow;
                half3 mainColor = mainLight.color * mainLight.distanceAttenuation;

                // Ambiente plano (media del entorno, sin degradado por la normal) + luces secundarias.
                half3 lighting = max(SampleSH(half3(0, 0, 0)), 0.0) * _AmbientStrength;

                // Luces adicionales (en Forward+ se recorren por clusters).
                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS)
                {
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_FORWARD_PLUS
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dirLight = GetAdditionalLight(dirIndex, input.positionWS, shadowMask);
                        lighting += ToonAdditionalLight(dirLight, normalWS);
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        lighting += ToonAdditionalLight(light, normalWS);
                    LIGHT_LOOP_END
                }
                #endif

                // Exposicion: se escala todo para que la zona a plena luz no pase de _MaxBrightness.
                // Se calcula sobre la zona iluminada, asi el contraste luz/sombra se conserva.
                half3 litTotal = mainColor + lighting;
                half exposure = min(1.0, _MaxBrightness / max(max(litTotal.r, litTotal.g), max(litTotal.b, 1e-4)));
                mainColor *= exposure;
                lighting = lighting * exposure + mainColor * lerp(_ShadeColor.rgb, half3(1, 1, 1), ramp);

                half3 color = albedo * lighting;

                // Brillo especular recortado, solo en la zona iluminada.
                half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                half ndoth = saturate(dot(normalWS, halfDir));
                half specEdge = 1.0 - _SpecSize;
                half spec = smoothstep(specEdge - _SpecSmooth, specEdge + _SpecSmooth, ndoth) * ramp;
                color += _ToonSpecColor.rgb * mainColor * spec;

                // Rim: borde iluminado en el lado que mira a la luz.
                half rimDot = (1.0 - saturate(dot(viewDirWS, normalWS))) * ramp;
                half rim = smoothstep(_RimAmount - _RimSmooth, _RimAmount + _RimSmooth, rimDot);
                color += _RimColor.rgb * mainColor * rim;

                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ Linea de contorno
        // Casco invertido: se dibujan las caras traseras empujadas hacia fuera a lo largo de la normal,
        // un numero fijo de pixeles en pantalla (no cambia con la distancia ni con la escala del modelo).
        Pass
        {
            Name "Outline"
            // UniversalForwardOnly: URP lo dibuja despues del pase UniversalForward del mismo objeto,
            // asi el stencil ya esta escrito cuando llega la linea.
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Front
            ZWrite On

            // En modo Silueta (NotEqual) la linea no se pinta encima de pixeles del propio personaje.
            Stencil
            {
                Ref 128
                ReadMask 128
                Comp [_OutlineStencilComp]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(positionWS);

                // Direccion de la normal en pantalla, medida en pixeles para que el grosor sea igual en X e Y.
                float2 dirPx = mul((float3x3)GetWorldToHClipMatrix(), normalWS).xy * _ScreenParams.xy;
                dirPx /= max(length(dirPx), 1e-5);
                positionCS.xy += dirPx * (2.0 * _OutlineWidth / _ScreenParams.xy) * positionCS.w;

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Linea tenida con una version oscura del color de la superficie: integra mejor que un negro puro.
                half3 surface = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half3 color = lerp(_OutlineColor.rgb, surface * 0.3, _OutlineTexMix);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ Sombras proyectadas
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Los rellena URP al dibujar el mapa de sombras de cada luz.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ Profundidad
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ Profundidad + normales (lo usa el SSAO)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
