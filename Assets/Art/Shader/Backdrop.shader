// Telón de fondo pintado con parallax por profundidad. Lo dibuja BackdropParallax.cs en un quad
// que rellena la pantalla justo delante del plano lejano de la cámara.
//   _MainTex  = la pintura
//   _DepthTex = mapa de profundidad de la pintura (blanco = cerca, negro = lejos)
// El encuadre y el desplazamiento de parallax los pasa el script (_UVRect y _Parallax).
// El resto son ajustes de aspecto: apagar y difuminar el fondo para que el nivel destaque,
// niebla en la parte baja para que parezca que el nivel flota sobre un foso, y parpadeo de luces.
Shader "Michi/Backdrop"
{
    Properties
    {
        _MainTex ("Pintura", 2D) = "black" {}
        _DepthTex ("Profundidad (blanco = cerca)", 2D) = "gray" {}

        [Header(Aspecto)]
        _Brightness ("Brillo", Range(0, 2)) = 0.6
        _Saturation ("Saturacion", Range(0, 1.5)) = 0.85
        _Blur ("Desenfoque (px de la pintura)", Range(0, 8)) = 1.5

        [Header(Bruma de distancia)]
        _HazeColor ("Color de la bruma", Color) = (0.20, 0.16, 0.34, 1)
        _HazeAmount ("Bruma en lo lejano", Range(0, 1)) = 0.35

        [Header(Foso inferior)]
        _BottomColor ("Color del foso", Color) = (0.07, 0.035, 0.05, 1)
        _BottomFade ("Fuerza del foso", Range(0, 1)) = 0.85
        _BottomHeight ("Altura del foso en pantalla", Range(0.05, 1)) = 0.55
        _MistAmount ("Niebla en movimiento", Range(0, 1)) = 0.35
        _MistSpeed ("Velocidad de la niebla", Range(0, 0.2)) = 0.02

        [Header(Luces)]
        _GlowBoost ("Brillo extra de ventanas y farolas", Range(0, 3)) = 0.9
        _Flicker ("Parpadeo", Range(0, 1)) = 0.25
        _FlickerSpeed ("Velocidad del parpadeo", Range(0, 10)) = 2.5

        [HideInInspector] _UVRect ("Encuadre (escala xy, centro zw)", Vector) = (1, 1, 0, 0)
        [HideInInspector] _Parallax ("Parallax (desplazamiento xy, factor lejano z)", Vector) = (0, 0, 0.5, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite On

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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex, _DepthTex;
            float4 _MainTex_TexelSize;
            float _Brightness, _Saturation, _Blur;
            float4 _HazeColor, _BottomColor;
            float _HazeAmount, _BottomFade, _BottomHeight, _MistAmount, _MistSpeed;
            float _GlowBoost, _Flicker, _FlickerSpeed;
            float4 _UVRect, _Parallax;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
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

            // Pintura con un desenfoque suave de 9 muestras.
            float3 samplePainting(float2 uv)
            {
                float2 r = _MainTex_TexelSize.xy * _Blur;
                float3 c = tex2D(_MainTex, uv).rgb * 2.0;
                c += tex2D(_MainTex, uv + r * float2( 1.0,  0.0)).rgb;
                c += tex2D(_MainTex, uv + r * float2(-1.0,  0.0)).rgb;
                c += tex2D(_MainTex, uv + r * float2( 0.0,  1.0)).rgb;
                c += tex2D(_MainTex, uv + r * float2( 0.0, -1.0)).rgb;
                c += tex2D(_MainTex, uv + r * float2( 0.7,  0.7)).rgb;
                c += tex2D(_MainTex, uv + r * float2(-0.7,  0.7)).rgb;
                c += tex2D(_MainTex, uv + r * float2( 0.7, -0.7)).rgb;
                c += tex2D(_MainTex, uv + r * float2(-0.7, -0.7)).rgb;
                return c / 10.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Encuadre: qué trozo de la pintura cae en cada punto de la pantalla.
                float2 baseUV = 0.5 + _UVRect.zw + (i.uv - 0.5) * _UVRect.xy;

                // Parallax: lo cercano (profundidad alta) se desplaza más que lo lejano.
                float depth = tex2D(_DepthTex, baseUV).r;
                float2 uv = baseUV + _Parallax.xy * lerp(_Parallax.z, 1.0, depth);
                depth = tex2D(_DepthTex, uv).r;
                uv = baseUV + _Parallax.xy * lerp(_Parallax.z, 1.0, depth);
                depth = tex2D(_DepthTex, uv).r;
                uv = baseUV + _Parallax.xy * lerp(_Parallax.z, 1.0, depth);

                float3 col = samplePainting(uv);
                float lum = dot(col, float3(0.299, 0.587, 0.114));

                // Luces cálidas (ventanas, farolas): se detectan por color para mantenerlas vivas.
                float warm = saturate((col.r - col.b) * 3.0 - 0.35) * saturate(lum * 3.0 - 0.5);
                float2 cell = floor(uv * float2(28.0, 16.0));
                float phase = hash21(cell) * 6.2831;
                float t = _Time.y * _FlickerSpeed;
                float flick = 0.6 * sin(t + phase) + 0.4 * sin(t * 2.7 + phase * 1.7);
                float3 glow = col * warm * _GlowBoost * (1.0 + _Flicker * flick);

                // Apagar y desaturar el fondo para que no compita con el nivel.
                col = lerp(lum.xxx, col, _Saturation) * _Brightness;

                // Bruma de distancia: lo lejano se funde con el color del aire.
                col = lerp(col, _HazeColor.rgb, _HazeAmount * (1.0 - depth));

                col += glow;

                // Foso: la parte baja de la pantalla se hunde en niebla oscura.
                float pit = 1.0 - smoothstep(0.0, _BottomHeight, i.uv.y);
                float mist = valueNoise(float2(i.uv.x * 3.0 + _Time.y * _MistSpeed, i.uv.y * 5.0 - _Time.y * _MistSpeed * 0.5));
                mist = mist * 0.65 + 0.35 * valueNoise(float2(i.uv.x * 7.0 - _Time.y * _MistSpeed * 1.7, i.uv.y * 11.0));
                pit = saturate(pit * (1.0 + _MistAmount * (mist - 0.5) * 2.0));
                col = lerp(col, _BottomColor.rgb, pit * _BottomFade);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
