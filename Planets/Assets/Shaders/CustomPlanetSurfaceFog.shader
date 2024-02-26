// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/CustomPlanetSurfaceFog"
{
    Properties{
        _offset("Offset", float) = 0
        _color1("Color 1", Color) = (1, 0.5, 0.5, 1)
        _color2("Color 2", Color) = (0.5, 1, 1, 1)
        _alphaWeight("Alpha Weight", float) = 4
        _colorWeight("Color Weight", float) = 0.5
        _sunlightDir("Sunlight Direction", vector) = (0,0,0,0)
        _m("m", float) = 0
        _b("b", float) = 0
        _fade("back of planet fade", float) = 0
        _distanceNearFade("near camera fade dist", float) = 30
        _distanceFarFade("far camera fade dist", float) = 300

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", float) = 1
    }
        SubShader{
            Tags {"Queue" = "Overlay" /*"IgnoreProjector" = "True"*/ "RenderType" = "Transparent"}
            Blend[_SrcBlend][_DstBlend]


            Pass {
                CGPROGRAM
                #pragma vertex vert alpha
                #pragma fragment frag alpha

                float4 _color1;
                float4 _color2;
                float4 _finalColor;
                float _offset;
                float _alphaWeight;
                float _colorWeight;
                vector _sunlightDir;
                float _HorizontalSpeed;
                float _VerticalSpeed;
                float _darkAlpha;
                float _m;
                float _b;
                float _fade;
                float _distanceNearFade;
                float _distanceFarFade;
                struct appdata {
                    float4 vertex : POSITION;
                    float4 tex : TEXCOORD0;
                    float3 normal : NORMAL;
                };

                struct v2f {
                    float4 vertex : TEXCOORD;
                    float4 pos : POSITION;
                    float4 color : COLOR;
                    float4 worldSpacePosition : TEXCOORD1;
                    float4 objectOrigin: TEXCOORD2;
                    float cameraDist: TEXCOORD3;
                };

                v2f vert(appdata v, v2f o) {
                    o.vertex = v.tex;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
                    o.objectOrigin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                    o.cameraDist = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));
                    return o;
                };

                float colorAdjust(float val, float val2, float4 vertex) : TEXCOORD4{
                    return 
                    (
                        (
                            val * 2 * pow(
                                _offset * (
                                    vertex.y - .5
                                ),
                                2
                            ) 
                            + 
                            val * 2 * pow(
                                2 * _offset * (
                                    vertex.x - .5
                                ),
                                2
                            )
                            + 
                            val2 * (
                                1 
                                - 
                                2 * pow(
                                    _offset * (
                                        vertex.y - .5
                                    ),
                                    2
                                )
                            )
                            + 
                            val2 * (
                                1 
                                - 
                                2 * pow(
                                    2 * _offset * (
                                        vertex.x - .5
                                    ), 
                                    2
                                )
                            )
                        )
                    );
                }

                half4 frag(v2f i) : COLOR{
                    _finalColor.a = colorAdjust(_color1.a, _color2.a, i.vertex);
                    _finalColor.r = colorAdjust(_color1.r, _color2.r, i.vertex);
                    _finalColor.g = colorAdjust(_color1.g, _color2.g, i.vertex);
                    _finalColor.b = colorAdjust(_color1.b, _color2.b, i.vertex);
                    _finalColor.a = clamp(pow(_finalColor.a, _alphaWeight), .0001, 1);
                    _finalColor.r = clamp(pow(_finalColor.r, _colorWeight), .0001, 1);
                    _finalColor.g = clamp(pow(_finalColor.g, _colorWeight), .0001, 1);
                    _finalColor.b = clamp(pow(_finalColor.b, _colorWeight), .0001, 1);
                    
                    _darkAlpha = pow(clamp(
                        _m * 
                            (.5 * dot(normalize(i.objectOrigin - i.worldSpacePosition), normalize(_sunlightDir)) + .5)
                        + _b, .0001, 1),_fade);
                    _finalColor.a = _finalColor.a * _darkAlpha;
                    
                    //camera proximal fade
                    _finalColor.a *= clamp(
                        (i.cameraDist - _distanceNearFade) / (_distanceFarFade - _distanceNearFade)
                        ,0 , 1
                    );
                    
                    return _finalColor;
                    
                };
                ENDCG

            }
    }
    FallBack "Diffuse"
}
