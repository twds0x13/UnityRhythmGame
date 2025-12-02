Shader "UI/PerlinNoiseWithScanlines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 柏林噪声参数
        _NoiseScale ("Noise Scale", Range(0.1, 100)) = 2.0
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 1.0
        _NoiseDirection ("Noise Direction", Vector) = (1, 1, 0, 0)
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.5
        
        // 噪声颜色参数
        _NoiseColor1 ("Noise Color 1", Color) = (0.2, 0.5, 1.0, 1.0)
        _NoiseColor2 ("Noise Color 2", Color) = (0.8, 0.2, 1.0, 1.0)
        _NoiseColor3 ("Noise Color 3", Color) = (0.1, 0.9, 0.3, 1.0)
        _ColorBlend ("Color Blend", Range(0, 2)) = 1.0
        
        // 扫描线效果参数
        _ScanlineWaveIntensity ("Scanline Wave Intensity", Range(0, 1)) = 0.3
        _ScanlinePatternIntensity ("Scanline Pattern Intensity", Range(0, 1)) = 0.5
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
            
            // 柏林噪声参数
            float _NoiseScale;
            float _NoiseSpeed;
            float2 _NoiseDirection;
            float _NoiseIntensity;
            
            // 噪声颜色参数
            fixed4 _NoiseColor1;
            fixed4 _NoiseColor2;
            fixed4 _NoiseColor3;
            float _ColorBlend;
            
            // 扫描线效果参数
            float _ScanlineWaveIntensity;
            float _ScanlinePatternIntensity;
            float _ScanlineSpeed;
            float _ScanlineDensity;
            fixed4 _ScanlineColor;
            
            // 随机函数
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 柏林噪声函数
            float perlinNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // 四个角的随机值
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                
                // 平滑插值
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            
            // 分形布朗运动（FBM）用于更自然的噪声
            float fbm(float2 uv, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * perlinNoise(uv * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            // 彩色噪声函数 - 生成与噪声同步移动的颜色
            float3 coloredNoise(float2 uv, float timeOffset)
            {
                float2 noiseUV = uv * _NoiseScale;
                noiseUV += _NoiseDirection * (_Time.y + timeOffset) * _NoiseSpeed;
                
                // 生成三个不同频率的噪声用于RGB通道
                float noise1 = fbm(noiseUV, 3);
                float noise2 = fbm(noiseUV * 1.5 + float2(10.0, 10.0), 2);
                float noise3 = fbm(noiseUV * 2.0 + float2(20.0, 20.0), 2);
                
                // 混合颜色
                float3 color1 = lerp(_NoiseColor1.rgb, _NoiseColor2.rgb, noise1);
                float3 color2 = lerp(color1, _NoiseColor3.rgb, noise2);
                
                return color2 * (0.8 + 0.2 * noise3);
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
                float time = _Time.y;
                float2 uv = IN.texcoord;
                
                // ========== 彩色柏林噪声 ==========
                float2 noiseUV = uv * _NoiseScale;
                
                // 添加时间移动
                noiseUV += _NoiseDirection * time * _NoiseSpeed;
                
                // 生成基础噪声用于透明度
                float baseNoise = fbm(noiseUV, 3);
                
                // 生成彩色噪声
                float3 coloredNoiseRGB = coloredNoise(uv, 0.0);
                
                // 应用噪声强度到透明度
                float alpha = baseNoise * _NoiseIntensity;
                
                // 创建彩色噪声效果
                fixed4 noiseColor = fixed4(coloredNoiseRGB, alpha);
                
                // ========== 扫描线效果 ==========
                float scanlineTime = _Time.y * _ScanlineSpeed;
                
                // 创建移动的扫描线波浪
                float scanlineWave = sin(uv.y * _ScanlineDensity + scanlineTime * 10.0) * 0.5 + 0.5;
                scanlineWave = 1.0 - scanlineWave * _ScanlineWaveIntensity;
                
                // 添加扫描线图案（间隔线条）
                float scanlinePattern = frac(uv.y * _ScanlineDensity * 0.5 + scanlineTime * 2.0);
                scanlinePattern = step(0.5, scanlinePattern);
                
                // 修改：确保当强度为1时，黑色部分达到完全黑色
                float patternEffect = lerp(1.0, scanlinePattern, _ScanlinePatternIntensity);
                
                // 组合扫描线效果
                float scanlineEffect = scanlineWave * patternEffect;
                
                // ========== 组合所有效果 ==========
                fixed4 finalColor = noiseColor;
                
                // 应用扫描线到噪声颜色
                finalColor.rgb *= scanlineEffect;
                
                // 可选：添加扫描线颜色叠加
                if (_ScanlineColor.a > 0)
                {
                    float scanlineOverlay = (1.0 - scanlinePattern) * _ScanlinePatternIntensity * _ScanlineColor.a;
                    finalColor.rgb = lerp(finalColor.rgb, _ScanlineColor.rgb, scanlineOverlay);
                }
                
                // 应用顶点颜色
                finalColor *= IN.color;
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
}