Shader "Aegis/WaveGridDistortion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Distortion ("Distortion in Pixels", Float) = 0
        _Phase ("Wave Phase", Float) = 0
        _TileSeed ("Tile Seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distortion;
            float _Phase;
            float _TileSeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Grid.png has roughly 74-pixel cells, starting at x=65, y=72.
                float2 cell = floor((i.uv * _MainTex_TexelSize.zw - float2(65.0, 72.0)) / 74.0);
                float seed = frac(dot(cell + _TileSeed, float2(0.173, 0.317)));
                float phase = _Phase * 18.85 + seed * 6.2832;
                float2 shift = float2(sin(phase), cos(phase * 1.17)) *
                    _Distortion * _MainTex_TexelSize.xy;
                return tex2D(_MainTex, i.uv + shift) * i.color;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
