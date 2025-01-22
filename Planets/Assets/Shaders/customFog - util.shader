Shader "Unlit/WorldPosFromDepth"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 projPos : TEXCOORD0;
                float3 camRelativeWorldPos : TEXCOORD1;
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.pos);
                o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;
                return o;
            }

            bool depthIsNotSky(float depth)
            {
                #if defined(UNITY_REVERSED_Z)
                return (depth > 0.0);
                #else
                return (depth < 1.0);
                #endif
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 screenUV = i.projPos.xy / i.projPos.w;

                // sample depth texture
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);

                // get linear depth from the depth
                float sceneZ = LinearEyeDepth(depth);

                // calculate the view plane vector
                // note: Something like normalize(i.camRelativeWorldPos.xyz) is what you'll see other
                // examples do, but that is wrong! You need a vector that at a 1 unit view depth, not
                // a1 unit magnitude.
                float3 viewPlane = i.camRelativeWorldPos.xyz / dot(i.camRelativeWorldPos.xyz, unity_WorldToCamera._m20_m21_m22);

                // calculate the world position
                // multiply the view plane by the linear depth to get the camera relative world space position
                // add the world space camera position to get the world space position from the depth texture
                float3 worldPos = viewPlane * sceneZ + _WorldSpaceCameraPos;
                worldPos = mul(unity_CameraToWorld, float4(worldPos, 1.0));

                half4 col = 0;

                // draw grid where it's not the sky
                if (depthIsNotSky(depth))
                    col.rgb = saturate(2.0 - abs(frac(worldPos) * 2.0 - 1.0) * 100.0);

                return col;
            }
            ENDCG
        }
    }
}