Shader "Custom/SquareGradient" {
    Properties{
        _direction("Direction", Range(0, 1)) = 0
        _color1("Color 1", Color) = (1, 0.5, 0.5, 1)
        _color2("Color 2", Color) = (0.5, 1, 1, 1)
        _alphaWeight("Alpha Weight", float) = 4
        _colorWeight("Color Weight", float) = 0.5
    }
        SubShader{
            Tags {"Queue" = "Overlay" /*"IgnoreProjector" = "True"*/ /*"RenderType" = "Transparent"*/}
            Blend SrcAlpha OneMinusSrcAlpha


            Pass {
                CGPROGRAM
                #pragma vertex vert alpha
                #pragma fragment frag alpha

                float4 _color1;
                float4 _color2;
                float4 _finalColor;
                float _direction;
                float _alphaWeight;
                float _colorWeight;

                float _HorizontalSpeed;
                float _VerticalSpeed;

                struct appdata {
                    float4 vertex : POSITION;
                    float4 tex : TEXCOORD0;
                    float3 normal : NORMAL;
                };

                struct v2f {
                    float4 vertex : TEXCOORD;
                    float4 pos : POSITION;
                    float4 color : COLOR;
                };

                v2f vert(appdata v, v2f o) {
                    o.vertex = v.tex;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    return o;
                };
                half4 frag(v2f i) : COLOR{
                    _finalColor.a = _color1.a * 2 * max(abs(i.vertex.y - .5), abs(i.vertex.x - .5)) + _color2.a * 2 * max(1 - abs(i.vertex.y - .5), 1 - abs(i.vertex.x - .5));
                        
                    _finalColor.a = pow(_finalColor.a, _alphaWeight);
                    _finalColor.r = _color1.r * 2 * max(abs(i.vertex.y - .5), abs(i.vertex.x - .5)) + _color2.r * 2 * max(1 - abs(i.vertex.y - .5), 1 - abs(i.vertex.x - .5));;
                    _finalColor.g = _color1.g * 2 * max(abs(i.vertex.y - .5), abs(i.vertex.x - .5)) + _color2.g * 2 * max(1 - abs(i.vertex.y - .5), 1 - abs(i.vertex.x - .5));;
                    _finalColor.b = _color1.b * 2 * max(abs(i.vertex.y - .5), abs(i.vertex.x - .5)) + _color2.b * 2 * max(1 - abs(i.vertex.y - .5), 1 - abs(i.vertex.x - .5));;
                    _finalColor.r = pow(_finalColor.r, _colorWeight);
                    _finalColor.g = pow(_finalColor.g, _colorWeight);
                    _finalColor.b = pow(_finalColor.b, _colorWeight);
                    return _finalColor;
                };
                ENDCG

            }
    }
        FallBack "Diffuse"
}