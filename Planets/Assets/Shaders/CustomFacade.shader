// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Facade" {
    Properties{
      _MainTex("Texture", 2D) = "white" {}
      _MainColor("Main Color", Color) = (0, 0, 0, 0)
      _MainBlend("Texture / Color Blend", Range(0,1)) = .5
      _HeightMap("Height Map", 2D) = "white" {}
      _HeightPower("Height Power", Range(0,.125)) = 0
      _TinyColor("Tiny Color", Color) = (0, 0, 0, 0)
      _SmallColor("Small Color", Color) = (0, 0, 0, 0)
      _MediumColor("Medium Color", Color) = (0, 0, 0, 0)
      _LargeColor("Larg Color", Color) = (0, 0, 0, 0)
      _BumpMap("Bumpmap", 2D) = "bump" {}
      _BumpLevel("Bump Level", Range(0,10)) = 1.0
      _RimColor("Rim Color", Color) = (0.26,0.19,0.16,0.0)
      _RimPower("Rim Power", Range(0.5,8.0)) = 3.0
      _TilingTiny("Tiling Tiny (t,d)", vector) = (1,1,0)
      _TilingSmall("Tiling Small (t,d)", vector) = (1,1,0)
      _TilingMedium("Tiling Medium (t,d)", vector) = (1,1,0)
      _TilingLarge("Tiling Large (t,d)", vector) = (1,1,0)
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        #pragma target 3.0
        #include "UnityStandardUtils.cginc"
        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            float3 worldPos;
            //float2 custom_uv;
            float camD;
            float2 uv_HeightMap;
        };
        sampler2D _MainTex;
        float4 _MainColor;
        float _MainBlend;
        sampler2D _BumpMap;
        float _BumpLevel;
        float4 _RimColor;
        float _RimPower;
        float2 _TilingTiny;
        float2 _TilingSmall;
        float2 _TilingMedium;
        float2 _TilingLarge;
        float _alphaTiny;
        float _alphaSmall;
        float _alphaMedium;
        float _alphaLarge;

        float4 _TinyColor;
        float4 _SmallColor;
        float4 _MediumColor;
        float4 _LargeColor;

        sampler2D _HeightMap;
        float _HeightPower;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            // copy the unmodified texture coordinates (aka UVs)
            //o.custom_uv = v.texcoord.xy;
            o.camD = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)));
        }

        void surf(Input IN, inout SurfaceOutput o) {

            //float camDist = o.camDist;//distance(IN.objectPos, _WorldSpaceCameraPos);
            _alphaTiny = clamp(min(
                1,

                -(IN.camD - _TilingTiny.y) / (_TilingSmall.y - _TilingTiny.y) + 1
            ),0,1);
            _alphaSmall = clamp(min(
                (IN.camD - _TilingSmall.y) / (_TilingSmall.y - _TilingTiny.y) + 1,
                -(IN.camD - _TilingSmall.y) / (_TilingMedium.y - _TilingSmall.y) + 1
            ),0,1);
            _alphaMedium = clamp(min(
                (IN.camD - _TilingMedium.y) / (_TilingMedium.y - _TilingSmall.y) + 1,
                -(IN.camD - _TilingMedium.y) / (_TilingLarge.y - _TilingMedium.y) + 1
            ),0,1);
            _alphaLarge = clamp(min(
                (IN.camD - _TilingLarge.y) / (_TilingLarge.y - _TilingMedium.y) + 1,
                1
            ),0,1);

            /*_alphaTiny = .5;
            _alphaSmall = 0;
            _alphaMedium = 0;
            _alphaLarge = .5;*/

            float2 texOffset = ParallaxOffset(
                (
                    tex2D(_HeightMap, IN.uv_HeightMap * _TilingTiny.x).r * _alphaTiny
                    +
                    tex2D(_HeightMap, IN.uv_HeightMap * _TilingSmall.x).r * _alphaSmall
                    +
                    tex2D(_HeightMap, IN.uv_HeightMap * _TilingMedium.x).r * _alphaMedium
                    +
                    tex2D(_HeightMap, IN.uv_HeightMap * _TilingLarge.x).r * _alphaLarge
                    )
                , _HeightPower / (
                    _TilingTiny.x * _alphaTiny
                    +
                    _TilingSmall.x * _alphaSmall
                    +
                    _TilingMedium.x * _alphaMedium
                    +
                    _TilingLarge.x * _alphaLarge
                ), IN.viewDir);

            o.Albedo = .6667 * (tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingTiny.x).rgb +
                tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingSmall.x ).rgb +
                tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingMedium.x ).rgb +
                tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingLarge.x ).rgb) / 4
                //_TinyColor;
                //_TinyColor * (IN.camD / _TilingTiny.y);
                /*_TinyColor * _alphaTiny +
                _SmallColor * _alphaSmall +
                _MediumColor * _alphaMedium +
                _LargeColor * _alphaLarge;*/
                +
                .3333 * (tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingTiny.x).rgb * _alphaTiny +
                tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingSmall.x ).rgb * _alphaSmall +
                tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingMedium.x ).rgb * _alphaMedium +
                tex2D(_MainTex, (IN.uv_MainTex + texOffset) * _TilingLarge.x ).rgb * _alphaLarge);
                /*
                tex2D(_MainTex, IN.uv_MainTex * _TilingTiny.x).r * _alphaTiny +
                tex2D(_MainTex, IN.uv_MainTex * _TilingSmall.x).r * _alphaSmall +
                tex2D(_MainTex, IN.uv_MainTex * _TilingMedium.x).r * _alphaMedium + 
                tex2D(_MainTex, IN.uv_MainTex * _TilingLarge.x).r * _alphaLarge
                +
                tex2D(_MainTex, IN.uv_MainTex * _TilingTiny.x).b * _alphaTiny +
                tex2D(_MainTex, IN.uv_MainTex * _TilingSmall.x).b * _alphaSmall +
                tex2D(_MainTex, IN.uv_MainTex * _TilingMedium.x).b * _alphaMedium +
                tex2D(_MainTex, IN.uv_MainTex * _TilingLarge.x).b * _alphaLarge
                +
                tex2D(_MainTex, IN.uv_MainTex * _TilingTiny.x).g * _alphaTiny +
                tex2D(_MainTex, IN.uv_MainTex * _TilingSmall.x).g * _alphaSmall +
                tex2D(_MainTex, IN.uv_MainTex * _TilingMedium.x).g * _alphaMedium +
                tex2D(_MainTex, IN.uv_MainTex * _TilingLarge.x).g * _alphaLarge
                ;*/
            o.Albedo = o.Albedo * (1 - _MainBlend) + _MainColor * _MainBlend;

            o.Normal = UnpackScaleNormal(
                 (
                    tex2D(_BumpMap,(IN.uv_BumpMap + texOffset) * _TilingTiny.x) * _alphaTiny + 
                    tex2D(_BumpMap,(IN.uv_BumpMap + texOffset) * _TilingSmall.x) * _alphaSmall +
                    tex2D(_BumpMap,(IN.uv_BumpMap + texOffset) * _TilingMedium.x) * _alphaMedium +
                    tex2D(_BumpMap,(IN.uv_BumpMap + texOffset) * _TilingLarge.x) * _alphaLarge
                )
                ,_BumpLevel
            );

            o.Normal = lerp(o.Normal,float3(0,0,1),1-_BumpLevel);

            /*o.Normal = UnpackScaleNormal(
                (
                    tex2D(_BumpMap, IN.uv_BumpMap * _TilingMedium.x) 
                    )
                , _BumpLevel
            );*/
            //half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            //o.Emission = _RimColor.rgb * pow(rim, _RimPower);
        }
        ENDCG
    }
    Fallback "Diffuse"
}



