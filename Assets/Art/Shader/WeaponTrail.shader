// Estela de barrido del arma: arco aditivo con filo brillante, vetas, relleno suave y un halo
// de resplandor por fuera del filo, y rayos eléctricos que lo recorren y parpadean. Se afina y se apaga hacia la cola. Lo dibuja WeaponTrail.cs.
//   uv.x = edad (0 = junto al arma, 1 = final de la cola)
//   uv.y = posición a lo ancho (0 = borde interior, _EdgePos = filo, 1 = fin del halo exterior)
//   color de vértice = tinte e intensidad a lo largo de la estela
Shader "Michi/WeaponTrail"
{
    Properties
    {
        _Intensity ("Intensidad general", Range(0, 8)) = 1.6
        _Glow ("Resplandor (halo)", Range(0, 4)) = 1.2
        _BodyGlow ("Relleno azul", Range(0, 2)) = 0.35
        _CoreSharpness ("Nitidez del filo", Range(1, 20)) = 9
        _CoreWhite ("Blanco en el filo", Range(0, 1)) = 0.45
        _Streaks ("Numero de vetas", Range(0, 12)) = 4
        _StreakStrength ("Fuerza de las vetas", Range(0, 2)) = 0.35
        _StreakSharpness ("Nitidez de las vetas", Range(1, 30)) = 6
        _Taper ("Afinado hacia la cola", Range(0, 1)) = 0.9
        _TailFade ("Apagado hacia la cola", Range(0.2, 6)) = 1.4
        [Header(Rayos)]
        _BoltStrength ("Fuerza de los rayos", Range(0, 6)) = 2
        _BoltCount ("Numero de rayos", Range(0, 3)) = 3
        _BoltWidth ("Grosor de los rayos", Range(0.002, 0.08)) = 0.012
        _BoltJitter ("Zigzag de los rayos", Range(0, 0.6)) = 0.22
        _BoltFrequency ("Quiebros a lo largo del arco", Range(1, 40)) = 14
        _BoltSpeed ("Parpadeo (cambios por segundo)", Range(0, 60)) = 22
        _BoltWhite ("Blanco de los rayos", Range(0, 1)) = 0.75
        [HideInInspector] _EdgePos ("Posicion del filo en V", Range(0.3, 1)) = 0.75
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float _Intensity, _Glow, _BodyGlow, _CoreSharpness, _CoreWhite;
            float _Streaks, _StreakStrength, _StreakSharpness;
            float _Taper, _TailFade, _EdgePos;
            float _BoltStrength, _BoltCount, _BoltWidth, _BoltJitter, _BoltFrequency, _BoltSpeed, _BoltWhite;

            float hash21 (float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Rayos: líneas finas que recorren el arco a lo largo, con quiebros de ruido, y que
            // cambian de forma a saltos (parpadeo). xq = posición a lo ancho (1 = filo).
            float lightning (float age, float xq)
            {
                float tick = floor(_Time.y * _BoltSpeed);
                float total = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float seed = k * 17.31 + tick * 7.13;
                    float along = age * _BoltFrequency;

                    // Trazado del rayo: una deriva lenta más un zigzag fino.
                    float drift = valueNoise(float2(along * 0.35 + seed, seed));
                    float zig = valueNoise(float2(along + seed * 1.7, seed + 3.1)) - 0.5
                              + (valueNoise(float2(along * 3.0 + seed, seed + 9.7)) - 0.5) * 0.5;
                    float center = lerp(0.45, 1.05, drift) + zig * _BoltJitter;

                    float d = abs(xq - center);
                    float line_ = pow(saturate(_BoltWidth / max(d, 0.0001)), 1.6);

                    float enabled = step(k + 0.5, _BoltCount);
                    float flicker = step(0.25, hash21(float2(k + 1.0, tick)));   // a ratos se apaga
                    total += line_ * enabled * flicker;
                }
                return total;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float age = saturate(i.uv.x);
                float v = saturate(i.uv.y);

                float inside = step(v, _EdgePos);
                float x = saturate(v / _EdgePos);                              // 0 = borde interior, 1 = filo
                float g = saturate((v - _EdgePos) / max(1.0 - _EdgePos, 0.001)); // 0 = filo, 1 = fin del halo

                // La cinta se afina hacia la cola: el borde interior se acerca al filo.
                float inner = _Taper * age;
                float w = saturate((x - inner) / max(1.0 - inner, 0.001));

                // Dentro del filo: núcleo brillante, vetas y un relleno azul suave.
                float core = pow(w, _CoreSharpness) * inside;
                float bands = abs(sin(w * _Streaks * 3.14159265));
                float streaks = pow(bands, _StreakSharpness) * w * _StreakStrength * inside;
                float body = w * w * _BodyGlow * inside;

                // Halo: resplandor que se derrama por fuera del filo y un poco hacia dentro.
                float haloOut = (1.0 - inside) * pow(1.0 - g, 3.0);
                float haloIn = inside * pow(w, 3.0) * 0.6;
                float halo = (haloOut + haloIn) * _Glow;

                float head = smoothstep(0.0, 0.05, age);            // sin corte duro junto al arma
                float tail = pow(1.0 - age, _TailFade);
                float fade = head * tail * i.color.a * _Intensity;

                float3 tint = i.color.rgb;
                float3 col = tint * (body + streaks + halo)
                           + lerp(tint, float3(1, 1, 1), _CoreWhite) * core;

                // Rayos eléctricos por encima, sin salirse de la cinta ni del halo.
                float xq = v / _EdgePos;
                float boltMask = saturate(w * 4.0) * inside + (1.0 - inside) * (1.0 - g);
                float bolts = lightning(age, xq) * boltMask * _BoltStrength;
                col += lerp(tint, float3(1, 1, 1), _BoltWhite) * bolts;
                return float4(col * fade, 1);
            }
            ENDCG
        }
    }
}
