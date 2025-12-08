    Shader "UI/Blur"
    {
        Properties
        {
            [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
            _Color ("Tint", Color) = (1,1,1,1)
            _BlurSize ("Blur Size", Range(0, 0.01)) = 0.005 // 模糊程度
            
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
            Tags
            {
                "Queue"="Transparent"
                "IgnoreProjector"="True"
                "RenderType"="Transparent"
                "PreviewType"="Plane"
                "CanUseSpriteAtlas"="True"
            }

            Stencil
            {
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
                Name "Default"
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "UnityCG.cginc"
                #include "UnityUI.cginc"

                #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    float4 worldPosition : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                sampler2D _MainTex;
                fixed4 _Color;
                float4 _ClipRect;
                float _BlurSize;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.worldPosition = v.vertex;
                    o.vertex = UnityObjectToClipPos(o.worldPosition);
                    o.texcoord = v.texcoord;
                    o.color = v.color * _Color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    half4 color = half4(0,0,0,0);
                    int samples = 16;
                    for (int j = 0; j < samples; j++)
                    {
                        float angle = (float)j / samples * 6.283185; // 2 * PI
                        float2 offset = float2(sin(angle), cos(angle)) * _BlurSize;
                        color += tex2D(_MainTex, i.texcoord + offset);
                    }
                    color /= samples;
                    color *= i.color;

                    color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                    #ifdef UNITY_UI_ALPHACLIP
                    clip (color.a - 0.001);
                    #endif

                    return color;
                }
                ENDCG
            }
        }
    }
    