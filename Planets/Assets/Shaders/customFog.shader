Shader "Custom/ScreenSpaceFog"
{
    Properties
    {
        _DayColor("Sunshine Color", Color) = (0, 0, 0, 1)
        _RimColorDay("Rim Color Day", Color) = (0, 0, 0, 1)
        _RimColorNight("Rim Color Night", Color) = (0, 0, 0, 1)
        _NightColor("Night Color", Color) = (0, 0, 0, 1)
        _DayPow("Day Power", float) = 1
        _NightPow("Night Power", float) = 1
        _FogMinDist("Fog Transparent Dist", float) = 1
        _FogMaxDist("Fog Opaque Dist", float) = 10
        _NearFade("Planet Distance Near Fade", float) = 220
        _FarFade("Planet Distance Far Fade", float) = 250
        _PlanetWorldOrigin("Planet Position", vector) = (0,0,0,0)
        _PlanetSurfaceRadius("Planet Surface Radius", float) = 200
        _AtmosphereMaxRadius("Atmosphere Radius", float) = 250
        _SunlightDir("Sunlight Direction", vector) = (0,0,0,0)
        _MinimumLight("Minimum Lighting", float) = .1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", float) = 1

        _DepthFactorSurface("Depth Factor Surface", float) = 1
        _DepthPowSurface("Depth Pow Surface", float) = 1
        _DepthFactorSpace("Depth Factor Space", float) = .07
        _DepthPowSpace("Depth Pow Space", float) = 1.93
    }
    SubShader
    {
        //Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        Tags { "RenderType" = "Transparent" "IgnoreProjector" = "True" "Queue" = "Transparent" }
        Blend[_SrcBlend][_DstBlend]
        //Blend SrcAlpha OneMinusSrcAlpha
        BlendOp[_BlendOp]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert //alpha
            #pragma fragment frag //alpha
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            #include "UnityCG.cginc"

            float4 _DayColor;
            float4 _RimColorDay;
            float4 _RimColorNight;
            float4 _NightColor;
            float _DayPow;
            float _NightPow;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float _DepthFactorSurface;
            float _DepthPowSurface;
            float _DepthFactorSpace;
            float _DepthPowSpace;

            float _NearFade;
            float _FarFade;
            float _FogMinDist;
            float _FogMaxDist;
            float3 _PlanetWorldOrigin;
            float _AtmosphereMaxRadius;
            float _PlanetSurfaceRadius;
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
                float4 pos : SV_POSITION;
                float4 projPos : TEXCOORD1;
                float3 camRelativeWorldPos : TEXCOORD2;
                float amtInSpace : TEXCOORD3;
                float dayNight : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float3 uvPos : TEXCOORD6;
            };



            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uvPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.projPos = ComputeScreenPos(o.pos);
                o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;
                o.dayNight = clamp(dot(
                    normalize(
                        _PlanetWorldOrigin-_WorldSpaceCameraPos
                    ), 
                    normalize(_SunlightDir)
                ),-1,1);
                
                o.amtInSpace = clamp((distance(_WorldSpaceCameraPos, _PlanetWorldOrigin) - _PlanetSurfaceRadius) / (_AtmosphereMaxRadius - _PlanetSurfaceRadius), 0, 1);
                
                o.screenPos = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.screenPos.z);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {

                float2 screenUV = i.projPos.xy / i.projPos.w;

                // sample depth texture
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);

                // get linear depth from the depth
                float sceneZ = LinearEyeDepth(depth);

                float3 worldPosition = (sceneZ * normalize(i.uvPos.xyz - _WorldSpaceCameraPos.xyz)) + _WorldSpaceCameraPos.xyz;

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
                
               
                ////////////
                /*float3 pixToCamSized = _WorldSpaceCameraPos.xyz - worldPos;
                float3 pixToPlanetSized;
                float3 pixToSun = -_SunlightDir;*/
                //////////


                //float4 dist = distance(_WorldSpaceCameraPos, worldPos);
                //this is just scenez

                //get dist to planet
                float distPlanet = distance(_WorldSpaceCameraPos, _PlanetWorldOrigin);

                float3 viewForward = normalize(i.camRelativeWorldPos);
                float3 uvForward = normalize(worldPosition - _WorldSpaceCameraPos);
                    //unity_CameraToWorld._m02_m12_m22;

                float3 theta = acos(dot(uvForward, normalize(_PlanetWorldOrigin - _WorldSpaceCameraPos)));

                float opposite = distPlanet * sin(theta);

                float adjacent = distPlanet * cos(theta);

                float midPointToEdge = sqrt(max(0, pow(_AtmosphereMaxRadius,2) - pow(opposite,2)));
                float distRayThroughCircle = max(0, min(adjacent, midPointToEdge) + midPointToEdge); //max(0, adjacent - midPointToEdge) + max(0, adjacent + midPointToEdge);
                float distThroughFog = min(distRayThroughCircle, sceneZ);
                float fogAmt = (max(0, distThroughFog - _FogMinDist)) / (_FogMaxDist - _FogMinDist);
                //_DayColor*=  depth;
                //fixed4 col = _DayColor;

                float depthPow = lerp(_DepthPowSurface, _DepthPowSpace, i.amtInSpace);
                float depthFactor = lerp(_DepthFactorSurface, _DepthFactorSpace, i.amtInSpace);

                fixed depthFading = saturate((abs(pow(fogAmt, depthPow))) / depthFactor);//saturate(distThroughFog/_AtmosphereMaxRadius);//saturate((abs(pow(depth, _DepthPowSurface))) / _DepthFactorSurface);
                //col *= depthFading;
                
                /*this doesn't work that well.  we need to do get the back and front dayNight of the sphere,
                and then lerp to match our ray start and depth*/
                /*float dayNight = clamp(dot(
                    normalize(
                        _PlanetWorldOrigin - worldPosition
                    ),
                    normalize(_SunlightDir)
                ), 0, 1);*/
                float3 mpEnterPos = (_WorldSpaceCameraPos + (uvForward * (adjacent - midPointToEdge))).xyz;
                float3 mpExitPos =  (_WorldSpaceCameraPos + (uvForward * (adjacent + midPointToEdge))).xyz;

                float dayNightEnter = clamp(
                    dot(
                        normalize(
                            mpEnterPos - _PlanetWorldOrigin
                        ),
                        normalize(
                            -_SunlightDir
                        )
                    ), -1, 1
                );

                float dayNightExit = clamp(
                    dot(
                        normalize(
                            mpExitPos - _PlanetWorldOrigin
                        ),
                        normalize(
                            -_SunlightDir
                        )
                    ), -1, 1
                );

                float scalarStartPointInFog = (-adjacent + midPointToEdge) / (2 * midPointToEdge);
                float sceneZInFog = sceneZ - (adjacent - midPointToEdge);//-min(0, adjacent - midPointToEdge);
                float scalarEndPointInFog = scalarStartPointInFog + (sceneZInFog / (2 * midPointToEdge));

                scalarStartPointInFog = max(0, scalarStartPointInFog);
                scalarEndPointInFog = min(1, scalarEndPointInFog);

                float midPointInFog = ((scalarEndPointInFog - scalarStartPointInFog) / 2) + scalarStartPointInFog;

                //should be just float???
                float mpDayNight = ((dayNightExit - dayNightEnter) * midPointInFog) + dayNightEnter;
                


                //calc sunset fx
                float3 mpPos = _WorldSpaceCameraPos + (uvForward * adjacent);
                float3 samplingMpPos = mpPos + (((midPointInFog * 2)-1) * 2 * midPointToEdge);
                float sunTheta = acos(dot( -normalize(_SunlightDir), normalize(_PlanetWorldOrigin - samplingMpPos)));
                float sunOpp = opposite * sin(sunTheta);
                float sunAdj = opposite * cos(sunTheta);
                float sunMidPointToEdge = sqrt(max(0, pow(_AtmosphereMaxRadius, 2) - pow(sunOpp, 2)));
                //float sunMpEnterPos = (mpPos + (-_SunlightDir * (sunAdj - sunMidPointToEdge))).xyz;
                //float sunMpExitPos = (samplingMpPos + (-normalize(_SunlightDir) * (sunAdj + sunMidPointToEdge))).xyz;
                float sunScalarStartPoint = (1 + (-sunAdj / sunMidPointToEdge)) / 2;//1/18 and prev: (-sunAdj + sunMidPointToEdge) / (2 * sunMidPointToEdge);
                //we gotta fudge a sunSceneZ here
                float sunSceneZ = pow(_PlanetSurfaceRadius, 2) - pow(sunOpp, 2);
                //this just makes sure that when we go negative, we don't get an imaginary number.  
                //instead we get a super large one
                sunSceneZ = sunSceneZ + max(pow(-sunSceneZ + 1.001, 999999), 0);
                sunSceneZ = sqrt(sunSceneZ);

                float sunSceneZStartClipped = sunSceneZ - min(0, sunAdj - sunMidPointToEdge);

                float sunScalarEndPoint = sunScalarStartPoint + (sunSceneZ / (2 * sunMidPointToEdge));
                sunScalarStartPoint = clamp(sunScalarStartPoint,0,1);
                sunScalarEndPoint = clamp(sunScalarEndPoint,0,1);
                //float sunScalarMidPoint = ((sunScalarEndPoint - sunScalarStartPoint) / 2) + sunScalarStartPoint;
                float sunTravelDist = (2 * sunMidPointToEdge) * (sunScalarEndPoint - sunScalarStartPoint) 
                    //* clamp((distance(samplingMpPos, _PlanetWorldOrigin) - _PlanetSurfaceRadius) * 9999999, 0, 1);
                    ;
                float maxSunTravelDist =2 * sqrt(pow(_AtmosphereMaxRadius,2) - pow(_PlanetSurfaceRadius, 2));
                float minSunTravelDist = 0;
                float sunSetPixelAmt = sunTravelDist/ maxSunTravelDist
                    //clamp((sunTravelDist - minSunTravelDist) / (maxSunTravelDist - minSunTravelDist),0,1) 
                    *
                    pow(
                        clamp(
                            dot(
                                uvForward, 
                                -normalize(
                                    _SunlightDir
                                )
                            ),0,1
                        ),
                        //this tightens things up as we leave the planet
                        1//max(distPlanet/(_AtmosphereMaxRadius),1)
                    )
                    ;
                

                /*float4 rimColor = lerp(_RimColorDay, _RimColorNight, clamp(-(2 * i.dayNight - 1), 0, 1));
                float4 nightColor = lerp(rimColor, _NightColor, pow(clamp(1 - (2*mpDayNight), 0, 1), _NightPow));
                float4 dayColor = lerp(rimColor, _DayColor, pow(clamp(-(2 * i.dayNight - 2), 0, 1), _DayPow));
                float4 DayNightColor = lerp(nightColor, dayColor, pow(clamp(2*mpDayNight-1, 0, 1), _DayPow));
                */


                float newSunDist = sunAdj + sqrt(pow(_AtmosphereMaxRadius, 2) - pow(sunOpp, 2));
                //clamp in case planet is in the way
                //newSunDist = newSunDist * clamp((distance(samplingMpPos, _PlanetWorldOrigin) - _PlanetSurfaceRadius) * 9999999, 0, 1);

                
                float4 dayColor = lerp(_DayColor, _RimColorDay, /*(newSunDist / maxSunTravelDist));*/sunSetPixelAmt);
                float4 nightColor = lerp(_NightColor, _RimColorNight, /*(newSunDist / maxSunTravelDist));*/sunSetPixelAmt);
                float4 DayNightColor = lerp(_NightColor, _DayColor, mpDayNight);
                float4 sunsetColor = lerp(DayNightColor, _RimColorDay, 1 - abs(8*mpDayNight-.5));
                float4 color = lerp(
                    DayNightColor,
                    sunsetColor,
                    clamp(
                        dot(
                            uvForward,
                            -normalize(
                                _SunlightDir
                            )
                        ), 0, 1
                    )
                );
                
                return
                    //day night
                    DayNightColor
                    //lerp(lerp( lerp(_RimColorDay, _RimColorNight, clamp(-i.dayNight, 0, 1)), _NightColor, pow(clamp(1-(dayNight + 1), 0, 1), _NightPow)), _DayColor, pow(clamp(dayNight, 0, 1), _DayPow))
                    //times depth for fog
                     * depthFading
                    //clamp( dot(normalize(worldPosition - _WorldSpaceCameraPos), viewForward)/2 + .5 , 0, 1)
                //dayNightFade
                 //clamp(dayNight, _MinimumLight,1)
                //proximal camera Fade
                /**(1 - clamp(
                    (i.cameraDistObjectOrigin - _NearFade) / (_FarFade - _NearFade)
                    , 0, 1
                ))*/
                ;
            }
            ENDCG
        }
    }
}
