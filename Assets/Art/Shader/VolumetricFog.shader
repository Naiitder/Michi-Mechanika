// Niebla volumétrica del foso. La dibuja VolumetricFog.cs en un quad pegado a la cámara, después
// de los objetos opacos y antes de partículas y estelas.
// Por cada píxel recorre el rayo de la cámara por debajo de la altura _FogTop (hasta la geometría
// que haya detrás, leída de la textura de profundidad) y acumula niebla con ruido 3D animado.
// La niebla es violeta arriba y se vuelve ascua hacia el fondo, y las luces puntuales que le pasa
// el script (los hornos) la iluminan desde dentro.
Shader "Michi/VolumetricFog"
{
    Properties
    {
        [Header(Color)]
        _TopColor ("Color arriba (bruma)", Color) = (0.30, 0.22, 0.42, 1)
        _DeepColor ("Color en el fondo (resplandor)", Color) = (0.62, 0.27, 0.10, 1)
        _GlowDepth ("Profundidad a la que domina el resplandor", Range(1, 60)) = 16

        [Header(Forma)]
        _Density ("Densidad", Range(0, 0.4)) = 0.06
        _Falloff ("Suavidad del borde superior", Range(0.5, 30)) = 7
        _MaxOpacity ("Opacidad maxima", Range(0, 1)) = 0.85
        _MaxDistance ("Distancia maxima", Range(20, 300)) = 60

        [Header(Movimiento)]
        _NoiseScale ("Tamano de las volutas", Range(0.01, 0.5)) = 0.07
        _NoiseStrength ("Fuerza de las volutas", Range(0, 1)) = 0.6
        _TopRoughness ("Irregularidad de la superficie", Range(0, 8)) = 1.5
        _Wind ("Viento (unidades por segundo)", Vector) = (0.35, 0.04, 0.18, 0)

        [Header(Luces)]
        _LightScatter ("Luz de los hornos en la niebla", Range(0, 4)) = 1

        [HideInInspector] _FogTop ("Altura de la superficie", Float) = -8.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define FOG_STEPS 16
            #define FOG_LIGHTS 4

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float4 _TopColor, _DeepColor, _Wind;
            float _GlowDepth, _Density, _Falloff, _MaxOpacity, _MaxDistance;
            float _NoiseScale, _NoiseStrength, _TopRoughness, _LightScatter, _FogTop;

            // Pasos de raymarching que fija el ajuste gráfico (GraphicsOptions). 0 = sin fijar: el máximo.
            float _MichiFogSteps;

            // xyz = posición, w = alcance / rgb = color ya multiplicado por la intensidad
            float4 _FogLightPos[FOG_LIGHTS];
            float4 _FogLightColor[FOG_LIGHTS];

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

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

            fixed4 frag(v2f i) : SV_Target
            {
                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(i.worldPos - ro);

                // Hasta dónde llega el rayo antes de chocar con la escena.
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float eyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
                float3 camForward = -UNITY_MATRIX_V[2].xyz;
                float sceneDistance = eyeDepth / max(dot(rd, camForward), 0.0001);

                // Tramo del rayo que queda por debajo de la superficie de la niebla.
                float top = _FogTop + _TopRoughness;
                float t0 = 0.0;
                float t1 = min(sceneDistance, _MaxDistance);
                if (abs(rd.y) > 0.00001)
                {
                    float tPlane = (top - ro.y) / rd.y;
                    if (rd.y < 0.0) t0 = max(t0, tPlane);
                    else t1 = min(t1, tPlane);
                }
                else if (ro.y > top)
                {
                    t1 = -1.0;
                }
                if (t1 <= t0) return fixed4(0, 0, 0, 0);

                // Desfase por píxel para que los pasos no se vean como bandas.
                float jitter = frac(52.9829189 * frac(dot(i.pos.xy, float2(0.06711056, 0.00583715))));
                int steps = _MichiFogSteps < 0.5 ? FOG_STEPS : (int)clamp(_MichiFogSteps, 4.0, FOG_STEPS);
                float dt = (t1 - t0) / steps;
                float3 wind = _Wind.xyz * _Time.y * _NoiseScale;

                float transmittance = 1.0;
                float3 light = 0.0;

                for (int k = 0; k < FOG_STEPS; k++)
                {
                    // Corta al agotar los pasos del ajuste o cuando la niebla ya tapa lo de detrás.
                    if (k >= steps || transmittance < 0.02) break;

                    float3 p = ro + rd * (t0 + (k + jitter) * dt);

                    float n = noise3(p * _NoiseScale + wind) * 0.65
                            + noise3(p * _NoiseScale * 2.7 - wind * 1.6) * 0.35;

                    float below = _FogTop + (n - 0.5) * 2.0 * _TopRoughness - p.y;
                    float h = saturate(below / _Falloff);
                    h = h * h * (3.0 - 2.0 * h);

                    float density = _Density * h * lerp(1.0, n * 2.0, _NoiseStrength);
                    float a = 1.0 - exp(-density * dt);

                    float3 c = lerp(_TopColor.rgb, _DeepColor.rgb, saturate(below / _GlowDepth));
                    for (int j = 0; j < FOG_LIGHTS; j++)
                    {
                        float3 toLight = p - _FogLightPos[j].xyz;
                        float range = max(_FogLightPos[j].w, 0.001);
                        float att = saturate(1.0 - dot(toLight, toLight) / (range * range));
                        c += _FogLightColor[j].rgb * (att * att * _LightScatter);
                    }

                    light += transmittance * a * c;
                    transmittance *= 1.0 - a;
                }

                float alpha = 1.0 - transmittance;
                float limit = min(alpha, _MaxOpacity) / max(alpha, 0.0001);
                return fixed4(light * limit, alpha * limit);
            }
            ENDCG
        }
    }
}
