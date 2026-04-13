Shader "UI/ProceduralLayer"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _Width ("Width", Float) = 100
        _Height ("Height", Float) = 100
        _Radius ("Radius", Float) = 10
        
        _BorderWidth ("Border Width", Float) = 0
        _BorderColor ("Border Color", Color) = (0,0,0,1)
        
        _EdgeSoftness ("Edge Softness", Float) = 1.0
        _Margin ("Margin", Float) = 0.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 worldPosition : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BorderColor;
            float _Width;
            float _Height;
            float _Radius;
            float _BorderWidth;
            float _EdgeSoftness;
            float _Margin;
            float4 _ClipRect;

            v2f vert(appdata_t IN) {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float sdRoundBox(float2 p, float2 b, float r) {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target {
                float2 uv = IN.texcoord - 0.5;
                float2 pixelPos = uv * float2(_Width, _Height);
                float2 halfSize = float2(_Width, _Height) * 0.5 - _Margin;

                float dist = sdRoundBox(pixelPos, halfSize, _Radius);
                
                // Anti-aliasing width
                float aa = max(_EdgeSoftness, 0.001);
                float halfAa = aa * 0.5;

                // Outer alpha (shape of the box)
                float alpha = 1.0 - smoothstep(-halfAa, halfAa, dist);

                fixed4 texColor = tex2D(_MainTex, IN.texcoord);
                fixed4 fill = texColor * IN.color; 
                
                fixed4 finalColor = fill;

                if (_BorderWidth > 0.001) {
                    // Inner alpha (shape of the inside of the border)
                    float innerDist = dist + _BorderWidth;
                    float innerAlpha = 1.0 - smoothstep(-halfAa, halfAa, innerDist);
                    
                    // Blend from border color to fill color
                    finalColor = lerp(_BorderColor, fill, innerAlpha);
                }

                finalColor.a *= alpha;
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                return finalColor;
            }
            ENDCG
        }
    }
}