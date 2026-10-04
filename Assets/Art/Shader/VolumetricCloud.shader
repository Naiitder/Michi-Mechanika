// Nube volumétrica suelta (humo o polvo). Se pone en un Cubo normal de Unity: la nube ocupa el
// elipsoide inscrito en el cubo, así que se coloca, gira y escala con el Transform.
// Por cada píxel recorre el rayo de la cámara por dentro del elipsoide (parando en la geometría
// que haya, leída de la textura de profundidad) y acumula densidad con ruido 3D animado.
// La cara que mira a la luz principal queda clara, la contraria en sombra, y la parte baja puede
// recibir un resplandor (los hornos del foso).
Shader "Michi/VolumetricCloud"
{
    Properties
    {
        [Header(Color)]
        _Color ("Color iluminado", Color) = (0.62, 0.52, 0.50, 1)
        _ShadeColor ("Color en sombra", Color) = (0.16, 0.11, 0.17, 1)
        _GlowColor ("Resplandor desde abajo", Color) = (1.0, 0.45, 0.14, 1)
        _GlowStrength ("Fuerza del resplandor", Range(0, 3)) = 0.6

        [Header(Forma)]
        _Density ("Densidad", Range(0, 3)) = 0.5
        _MaxOpacity ("Opacidad maxima", Range(0, 1)) = 0.8
        _Wispiness ("Bordes deshilachados", Range(0, 1.5)) = 0.9
        _NoiseScale ("Tamano de las volutas", Range(0.02, 1.5)) = 0.28

        [Header(Movimiento)]
        _Scroll ("Desplazamiento (unidades por segundo)", Vector) = (0.1, 0.7, 0.05, 0)
        _Churn ("Remolino interno", Range(0, 2)) = 0.35

        [Header(Luz)]
        _Shadowing ("Sombra propia", Range(0, 8)) = 3

        // Los rellena CloudDrift.cs por nube; sin ese script valen esto.
        [HideInInspector] _Fade ("Opacidad de esta nube", Range(0, 1)) = 1
        [HideInInspector] _Seed ("Semilla del ruido", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        // Se dibujan las caras de atrás y sin test de profundidad para que funcione también con la
        // cámara dentro de la nube; la geometría que la tapa se resuelve con la textura de profundidad.
        Cull Front
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define CLOUD_STEPS 14

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

            float4 _Color, _ShadeColor, _GlowColor, _Scroll, _Seed;
            float _Fade, _GlowStrength, _Density, _MaxOpacity, _Wispiness, _NoiseScale, _Churn, _Shadowing;

            // Dirección de la luz principal; la rellena URP.
            float4 _MainLightPosition;

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

            // Densidad en un punto: 1 en el centro del elipsoide y 0 en el borde, comida por el ruido.
            float cloudDensity(float3 worldPos, float3 objPos)
            {
                float r2 = dot(objPos, objPos) * 4.0;
                float shape = saturate(1.0 - r2);

                // El ruido viaja con la nube (se mide desde su centro), así al moverla no "resbala".
                float3 center = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float3 q = (worldPos - center) * _NoiseScale - _Scroll.xyz * (_Time.y * _NoiseScale) + _Seed.xyz;
                float n = noise3(q) * 0.6
                        + noise3(q * 2.3 + _Time.y * _Churn * 0.17) * 0.28
                        + noise3(q * 5.1 - _Time.y * _Churn * 0.31) * 0.12;

                return saturate(shape * 1.3 - (1.0 - n) * _Wispiness);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Fade <= 0.001) return fixed4(0, 0, 0, 0);

                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(i.worldPos - ro);

                float2 uv = i.screenPos.xy / i.screenPos.w;
                float eyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
                float3 camForward = -UNITY_MATRIX_V[2].xyz;
                float sceneDistance = eyeDepth / max(dot(rd, camForward), 0.0001);

                // Rayo en espacio del objeto: ahí la nube es una esfera de radio 0.5.
                float3 roObj = mul(unity_WorldToObject, float4(ro, 1.0)).xyz;
                float3 rdObj = mul((float3x3)unity_WorldToObject, rd);

                float a = dot(rdObj, rdObj);
                float b = dot(roObj, rdObj);
                float c = dot(roObj, roObj) - 0.25;
                float disc = b * b - a * c;
                if (disc <= 0.0) return fixed4(0, 0, 0, 0);

                float root = sqrt(disc);
                float t0 = max((-b - root) / a, 0.0);
                float t1 = min((-b + root) / a, sceneDistance);
                if (t1 <= t0) return fixed4(0, 0, 0, 0);

                float jitter = frac(52.9829189 * frac(dot(i.pos.xy, float2(0.06711056, 0.00583715))));
                float dt = (t1 - t0) / CLOUD_STEPS;

                float3 lightDir = normalize(_MainLightPosition.xyz + float3(0.0, 0.0001, 0.0));
                float3 lightDirObj = mul((float3x3)unity_WorldToObject, lightDir);
                // Un paso hacia la luz de un sexto del tamaño de la nube, para la sombra propia.
                float probe = 0.16 / max(length(lightDirObj), 0.0001);

                float transmittance = 1.0;
                float3 light = 0.0;

                for (int k = 0; k < CLOUD_STEPS; k++)
                {
                    float t = t0 + (k + jitter) * dt;
                    float3 pw = ro + rd * t;
                    float3 po = roObj + rdObj * t;

                    float d = cloudDensity(pw, po);
                    if (d > 0.001)
                    {
                        float towardLight = cloudDensity(pw + lightDir * probe, po + lightDirObj * probe);
                        float lit = exp(-towardLight * _Shadowing);

                        float3 col = lerp(_ShadeColor.rgb, _Color.rgb, lit);
                        float low = saturate(0.5 - po.y);
                        col += _GlowColor.rgb * (low * low * _GlowStrength);

                        float alpha = 1.0 - exp(-d * _Density * dt);
                        light += transmittance * alpha * col;
                        transmittance *= 1.0 - alpha;
                    }
                }

                float opacity = 1.0 - transmittance;
                float limit = min(opacity, _MaxOpacity) / max(opacity, 0.0001) * _Fade;
                return fixed4(light * limit, opacity * limit);
            }
            ENDCG
        }
    }
}
