// Estela de barrido del arma: arco aditivo con filo brillante, vetas, relleno suave y un halo
// de resplandor por fuera del filo. Se afina y se apaga hacia la cola. Lo dibuja WeaponTrail.cs.
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
                return float4(col * fade, 1);
            }
            ENDCG
        }
    }
}
