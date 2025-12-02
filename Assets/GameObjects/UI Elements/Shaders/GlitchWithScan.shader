Shader "UI/GlitchWithScanlines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 故障效果参数
        _GlitchIntensity ("Glitch Intensity", Range(0, 0.1)) = 0.02
        _GlitchSpeed ("Glitch Speed", Range(0, 10)) = 1.0
        
        // 扫描线效果参数
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.1
        _ScanlineSpeed ("Scanline Speed", Range(0, 10)) = 1.0
        _ScanlineDensity ("Scanline Density", Float) = 200.0
        _ScanlineColor ("Scanline Color", Color) = (0, 0, 0, 0.5)
        
        // UI必需属性
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 texcoord  : TEXCOORD0;
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_ST;
            
            // 故障效果参数
            float _GlitchIntensity;
            float _GlitchSpeed;
            
            // 扫描线效果参数
            float _ScanlineIntensity;
            float _ScanlineSpeed;
            float _ScanlineDensity;
            fixed4 _ScanlineColor;
            
            // 噪声函数
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                float time = _Time.y * _GlitchSpeed;
                
                // ========== 故障效果 ==========
                float glitch = random(float2(time, time)) * _GlitchIntensity;
                
                // 对RGB通道进行不同的偏移
                float2 uv = IN.texcoord;
                float r = tex2D(_MainTex, uv + float2(glitch * 0.1, 0)).r;
                float g = tex2D(_MainTex, uv + float2(glitch * -0.1, 0)).g;
                float b = tex2D(_MainTex, uv + float2(glitch * 0.05, 0)).b;
                
                half4 color = half4(r, g, b, tex2D(_MainTex, uv).a);
                color *= IN.color;
                
                // ========== 扫描线效果 ==========
                float scanlineTime = _Time.y * _ScanlineSpeed;
                
                // 创建移动的扫描线
                float scanline = sin(uv.y * _ScanlineDensity + scanlineTime * 10.0) * 0.5 + 0.5;
                scanline = 1.0 - scanline * _ScanlineIntensity;
                
                // 添加扫描线图案（间隔线条）
                float scanlinePattern = frac(uv.y * _ScanlineDensity * 0.5 + scanlineTime * 2.0);
                scanlinePattern = step(0.5, scanlinePattern);
                
                // 组合扫描线效果
                float scanlineEffect = lerp(1.0, scanline * (0.7 + 0.3 * scanlinePattern), _ScanlineIntensity);
                
                // 应用扫描线到颜色
                color.rgb *= scanlineEffect;
                
                // 可选：添加扫描线颜色叠加
                if (_ScanlineColor.a > 0)
                {
                    float scanlineOverlay = (1.0 - scanlinePattern) * _ScanlineIntensity * _ScanlineColor.a;
                    color.rgb = lerp(color.rgb, _ScanlineColor.rgb, scanlineOverlay);
                }
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}