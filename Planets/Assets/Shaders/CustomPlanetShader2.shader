// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/CustomPlanetShader2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AtmDepthColor ("Atmosphere Color at Depth", Color) = (0, 0, 0, 1)
        _AtmSurfaceColor ("Atmosphere Color at Surface", Color) = (0, 0, 0, 1)
        _AtmDepth ("Atmosphere Depth", float) = 100
        _SurfaceAdjust("Surface Adjust", float) = 200
        _SurfaceAlphaAdjust("Surface Alpha Adjust", float) = 2
        _HaloAdjust ("Halo Adjust", float) = 100
        _HaloAlphaAdjust ("Halo Alpha Adjust", float) = 1
        _Fudge("Fudge", float) = 1
        _AlphaFudge("Alpha Fudge", float) = 1
        _SunlightDir("Sunlight Direction", vector) = (0,0,0,0)
        _m("m", float) = 0
        _b("b", float) = 0
        _fade("back of planet fade", float) = 0

        _DistanceNearFade("near camera fade dist", float) = 30
        _DistanceFarFade("far camera fade dist", float) = 300

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType" = "Transparent"}
        Blend[_SrcBlend][_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha
            static const float PI = 3.14159;
            float4 _AtmDepthColor;
            float4 _AtmSurfaceColor;
            float _AtmDepth;

            float4 _finalColor;
            float _theta;
            float _opposite;
            float _height;
            float _backHeight;
            float _tOpposite;
            float _tHeight;
            float _fogDepth;
            float _maxFogDepth;
            float _atmRadius;
            float _test;
            float _Fudge;
            float _SurfaceAdjust;
            float _SurfaceAlphaAdjust;
            float _HaloAdjust;
            float _HaloAlphaAdjust;
            float _AlphaFudge;
            float _darkAlpha;
            float _m;
            float _b;
            float _fade;
            vector _SunlightDir;
            float _DistanceNearFade;
            float _DistanceFarFade;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 texelPosition : TEXCOORD1;
                float4 objectOrigin: TEXCOORD2;
                float cameraDistObjectOrigin : TEXCOORD3;
                float3 camerafwd: TEXCOORD4;
                float cameraDistTexel : TEXCOORD5;
                float texelDistObject : TEXCOORD6;


            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                /*o.uv = TRANSFORM_TEX(v.uv, _MainTex);*/
                o.texelPosition = mul(unity_ObjectToWorld, v.vertex);
                o.objectOrigin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                o.cameraDistObjectOrigin = distance(_WorldSpaceCameraPos, o.objectOrigin);
                o.cameraDistTexel = distance(_WorldSpaceCameraPos, o.texelPosition);
                o.texelDistObject = distance(o.texelPosition, o.objectOrigin);
                o.camerafwd = unity_CameraToWorld._m02_m12_m22;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                /*static const ?   once per draw*/
                _atmRadius = length(i.texelPosition - i.objectOrigin);

                // apply fog
                _theta = acos(dot(normalize(i.objectOrigin - _WorldSpaceCameraPos), normalize(i.texelPosition - _WorldSpaceCameraPos)));
                //_opposite = tan(_theta) * (i.cameraDistObjectOrigin);
                //_opposite = 2 * i.cameraDistObjectOrigin * ( -1 / tan(_theta) +  1/ sin(_theta));
                _opposite = (_atmRadius - _AtmDepth) * cos(  PI / 2 + _theta - asin(tan(_theta) * i.cameraDistObjectOrigin * sin(PI / 2 - _theta) / (_atmRadius - _AtmDepth)));
                _opposite = clamp(_opposite, 0, (_atmRadius - _AtmDepth));
                _height =   (_atmRadius - _AtmDepth) * sin(  PI / 2 + _theta - asin(tan(_theta) * i.cameraDistObjectOrigin * sin(PI / 2 - _theta) / (_atmRadius - _AtmDepth)));
                _height = clamp(_height, 0, _atmRadius - _AtmDepth);
                _tOpposite = (_atmRadius)            * cos(  PI / 2 + _theta - asin(tan(_theta) * i.cameraDistObjectOrigin * sin(PI / 2 - _theta) / (_atmRadius)));
                _tHeight =   (_atmRadius)            * sin(  PI / 2 + _theta - asin(tan(_theta) * i.cameraDistObjectOrigin * sin(PI / 2 - _theta) / (_atmRadius)));
                
                //_oppositeBack = (_atmRadius - _AtmDepth) * cos(PI / 2 - t - asin(tan(t) * i.cameraDistObjectOrigin * sin(PI / 2 - t) / (_atmRadius - _AtmDepth)));
                //_height = sin(acos(clamp(_opposite / (_atmRadius - _AtmDepth), 0, 1)));
                //_height = cos(theta) * (o.cameraDistObjectOrigin - _atmRadius);
                //_backHeight = sin(acos(_opposite))*_atmRadius;
                _backHeight = _atmRadius * 2 * dot(normalize(_WorldSpaceCameraPos - i.objectOrigin), normalize(i.texelPosition - i.objectOrigin));//_atmRadius * sin(PI / 2 - _theta - asin(tan(_theta) * i.cameraDistObjectOrigin * sin(PI / 2 - _theta) / _atmRadius));
                //_height *= (_atmRadius - _AtmDepth);

                //_height = lerp(atmRadius-_height, _backHeight, pow(1 - _height / (_atmRadius - _AtmDepth), _Fudge));
                /*static const?  once ever?*/
                _maxFogDepth = _atmRadius * 2 * sin(acos((_atmRadius - _AtmDepth) / _atmRadius));
                _fogDepth = lerp(pow(sqrt(pow(_tHeight - _height, 2) + pow(_tOpposite - _opposite, 2))/_SurfaceAdjust, _SurfaceAlphaAdjust), pow(_backHeight / _HaloAdjust, _HaloAlphaAdjust), pow(1 - _height / (_atmRadius - _AtmDepth), _Fudge));
                
                //_fogDepth = (_atmRadius - sqrt(pow(_opposite,2)+pow(_height,2))) / _maxFogDepth;
                //_fogDepth = distance(i.objectOrigin + (normalize(- i.camerafwd) * _height) + (-i) _opposite, i.texelPosition) / _maxFogDepth;
                
                _finalColor = lerp(_AtmSurfaceColor, _AtmDepthColor, _fogDepth);
                _finalColor.a = clamp(pow(_finalColor.a, _AlphaFudge), .00001, .99999);
                
                _darkAlpha = pow(
                    clamp(
                        _m *
                        (
                            .5 * dot(
                                normalize(
                                    i.objectOrigin - i.texelPosition
                                ), normalize(
                                    _SunlightDir
                                )
                            ) + .5
                        )
                        + _b, .0001, 1
                    ), _fade
                );
                _finalColor.a = _finalColor.a * _darkAlpha;
                
                //camera proximal fade
                _finalColor.a *= pow(
                    clamp(
                        (length(i.objectOrigin - _WorldSpaceCameraPos) - _DistanceNearFade) / (_DistanceFarFade - _DistanceNearFade)
                        , 0, 1
                    )
                ,2);

                return _finalColor;
            }
            ENDCG
        }
    }
}
