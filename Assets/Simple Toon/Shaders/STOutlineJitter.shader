Shader "Simple Toon/SToon Outline Jitter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Colorize)][Space(5)]
        _Color ("Color", COLOR) = (1,1,1,1)
        [HideInInspector] _ColIntense ("Intensity", Range(0,3)) = 1
        [HideInInspector] _ColBright ("Brightness", Range(-1,1)) = 0
        _AmbientCol ("Ambient", Range(0,1)) = 0

        [Header(Detail)][Space(5)]
        [Toggle] _Segmented ("Segmented", Float) = 1
        _Steps ("Steps", Range(1,25)) = 3
        _StpSmooth ("Smoothness", Range(0,1)) = 0
        _Offset ("Lit Offset", Range(-1,1.1)) = 0

        [Header(Light)][Space(5)]
        [Toggle] _Clipped ("Clipped", Float) = 0
        _MinLight ("Min Light", Range(0,1)) = 0
        _MaxLight ("Max Light", Range(0,1)) = 1
        _Lumin ("Luminocity", Range(0,2)) = 0

        [Header(Outline)][Space(5)]
        _OtlColor ("Color", COLOR) = (0,0,0,1)
        _OtlWidth ("Width", Range(0,5)) = 1
        
        // --- OUTLINE JITTER & WOBBLE ---
        _OtlJitter ("Jitter Crunchiness", Range(1, 512)) = 100
        _OtlVariance ("Thickness Variance", Range(0, 1)) = 0.5
        _OtlWobbleSpeed ("Wobble Speed", Range(0, 50)) = 15
        _OtlWobbleFreq ("Wobble Frequency", Range(0, 50)) = 10

        [Header(Shine)][Space(5)]
        [HDR] _ShnColor ("Color", COLOR) = (1,1,0,1)
        [Toggle] _ShnOverlap ("Overlap", Float) = 0
        _ShnIntense ("Intensity", Range(0,1)) = 0
        _ShnRange ("Range", Range(0,1)) = 0.15
        _ShnSmooth ("Smoothness", Range(0,1)) = 0

    }

    SubShader
    {
        // ==========================================
        // PASS 1: DIRECT LIGHT (Your Original Code)
        // ==========================================
        Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
        Pass
        {
            Name "DirectLight"
            LOD 80

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            #include "STCore.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                LIGHTING_COORDS(0,1)
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                half3 worldNormal : NORMAL;
                float3 viewDir : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);

                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                _MaxLight = max(_MinLight, _MaxLight);
                _Steps = _Segmented ? _Steps : 1;
                _StpSmooth = _Segmented ? _StpSmooth : 1;

                _DarkColor = fixed4(0,0,0,1);
                _MaxAtten = 1.0;

                float3 normal = normalize(i.worldNormal);
                float3 light_dir = normalize(_WorldSpaceLightPos0.xyz);
                float3 view_dir = normalize(i.viewDir);
                float3 halfVec = normalize(light_dir + view_dir);
                float3 forward = mul((float3x3)unity_CameraToWorld, float3(0,0,1));

                float NdotL = dot(normal, light_dir);
                float NdotH = dot(normal, halfVec);
                float VdotN = dot(view_dir, normal);
                float FdotV = dot(forward, -view_dir);

                fixed atten = SHADOW_ATTENUATION(i);
                float toon = Toon(NdotL, atten);

                fixed4 shadecol = _DarkColor;
                fixed4 litcol = ColorBlend(_Color, _LightColor0, _AmbientCol);
                fixed4 texcol = tex2D(_MainTex, i.uv) * litcol * _ColIntense + _ColBright;

                float4 blendCol = ColorBlend(shadecol, texcol, toon);
                float4 postCol = PostEffects(blendCol, toon, atten, NdotL, NdotH, VdotN, FdotV);

                postCol.a = 1.;
                return _LightColor0.a > 0 ? postCol : 0;
            }
            ENDCG
        }

        // ==========================================
        // PASS 2: SPOT LIGHT (Your Original Code)
        // ==========================================
        Tags { "RenderType" = "Opaque" "LightMode" = "ForwardAdd" }
        Pass
        {
            Name "SpotLight"
            BlendOp Max
            LOD 100

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            #include "STCore.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                LIGHTING_COORDS(0,1)
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : WORLD;
                half3 worldNormal : NORMAL;
                float3 viewDir : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = WorldSpaceViewDir(v.vertex);

                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                _MaxLight = max(_MinLight, _MaxLight);
                _Steps = _Segmented ? _Steps : 1;
                _StpSmooth = _Segmented ? _StpSmooth : 1;

                _DarkColor = fixed4(0,0,0,1);
                _MaxAtten = 1.0;

                float3 normal = normalize(i.worldNormal);
                float3 light_dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
                float3 view_dir = normalize(i.viewDir);
                float3 halfVec = normalize(light_dir + view_dir);
                float3 forward = mul((float3x3)unity_CameraToWorld, float3(0,0,1));

                float NdotL = dot(normal, light_dir);
                float NdotH = dot(normal, halfVec);
                float VdotN = dot(view_dir, normal);
                float FdotV = dot(forward, -view_dir);

                float atten = LIGHT_ATTENUATION(i);
                float toon = Toon(NdotL, atten);

                fixed4 shadecol = _DarkColor;
                fixed4 litcol = ColorBlend(_Color, _LightColor0, _AmbientCol);
                fixed4 texcol = tex2D(_MainTex, i.uv) * litcol * _ColIntense + _ColBright;

                float4 blendCol = ColorBlend(shadecol, texcol, toon);
                float4 postCol = PostEffects(blendCol, toon, atten, NdotL, NdotH, VdotN, FdotV);

                postCol.a = 1.;
                return postCol;
            }
            ENDCG
        }

        // ==========================================
        // PASS 3: SHADOWS (Your Original Code)
        // ==========================================
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

        // ==========================================
        // PASS 4: OUTLINE SINE WOBBLE + PSX JITTER
        // ==========================================
        Pass
        {
            Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
            Blend Off
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "STCore.cginc"

            float4 _OtlColor;
            float _OtlWidth;
            float _OtlJitter;
            float _OtlVariance;
            float _OtlWobbleSpeed;
            float _OtlWobbleFreq;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                
                // 1. Smooth Sine Wobble (Boiling Line)
                // We sum the X, Y, and Z of the vertex to get a unique offset per area of the mesh.
                // We multiply by Frequency to determine how tightly clustered the thick/thin waves are.
                float spatialOffset = (v.vertex.x + v.vertex.y + v.vertex.z) * _OtlWobbleFreq;
                
                // Add _Time.y (time in seconds) multiplied by Speed to animate it
                float timeOffset = _Time.y * _OtlWobbleSpeed;
                
                // sin() returns a value between -1.0 and 1.0. 
                float wobble = sin(spatialOffset + timeOffset);
                
                // Map it to our width multiplier. 
                // If variance is 0.5, multiplier waves smoothly between 0.5 and 1.5.
                float widthMult = 1.0 + (wobble * _OtlVariance);
                
                // 2. Extrude the hull
                float4 vPos = v.vertex;
                vPos.xyz += normalize(v.normal.xyz) * _OtlWidth * widthMult * 0.008;
                o.pos = UnityObjectToClipPos(vPos);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(-negz(_OtlWidth));
                return _OtlColor;
            }
            ENDCG
        }
    }
}