Shader "Custom/Color"
{
    Properties
    {
        _Color("Atmosphere Color at Depth", Color) = (0, 0, 0, 1)
        _NearFade("Near Fade", float) = 1
        _FarFade("Far Fade", float) = 10
        _SunlightDir("Sunlight Direction", vector) = (0,0,0,0)
        _MinimumLight("Minimum Lighting", float) = .1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        Blend[_SrcBlend][_DstBlend]
        BlendOp[_BlendOp]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha

            float4 _Color;
            float _NearFade;
            float _FarFade;
            float4 _SunlightDir;
            float _MinimumLight;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 cameraDistObjectOrigin : TEXCOORD1;
                float dayNight : TEXCOORD2;
            };


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.dayNight = clamp(dot(
                    normalize(
                        mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)) - _WorldSpaceCameraPos
                    ), 
                    normalize(_SunlightDir)
                ),0,1);
                o.cameraDistObjectOrigin = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)));
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return
                _Color
                //proximal camera Fade
                * (1 - clamp(
                    (i.cameraDistObjectOrigin - _NearFade) / (_FarFade - _NearFade)
                    , 0, 1
                ))
                //dayNightFade
                * clamp(i.dayNight, _MinimumLight,1);
                ;
            }
            ENDCG
        }
    }
}
