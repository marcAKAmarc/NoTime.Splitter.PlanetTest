Shader "Custom/ScreenSpaceFog2"
{

    //IT ALL LOOKS GOOD WITH INCREASED FOG DIST IN SPACE AND
    //THE SUNDIR POWER ON.  ON THE PLANET THAT POWER SHOULD BE OFF.  SO I GOTTA FIX THAT.
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

            const float pi = 3.14159;

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

                float3 viewForward = normalize(i.camRelativeWorldPos);
                float3 uvForward = normalize(worldPosition - _WorldSpaceCameraPos);

                //get dist to planet
                float3 fogRayStart = _WorldSpaceCameraPos + uvForward; //* _FogMinDist;
                float distPlanet = distance(fogRayStart, _PlanetWorldOrigin);

                float3 theta = acos(dot(uvForward, normalize(_PlanetWorldOrigin - fogRayStart)));

                float opposite = distPlanet * sin(theta);

                float adjacent = distPlanet * cos(theta);
                
                float distInFog = sqrt(   max(0,pow(_AtmosphereMaxRadius, 2) - pow(opposite, 2)));
                float distInPlanet = sqrt(max(0,pow(_PlanetSurfaceRadius, 2) - pow(opposite, 2)));
                
                
                //---don't need and maybe incorrect----
                float distAmtSide1 = max(0, min(adjacent, distInFog) - distInPlanet);
                float distAmtSide2 = max(0, min(distInFog, distInFog + adjacent));
                //--------------------------------------
                
                //0 if planet has value, otherwise 1
                float planetCancel = clamp(distInPlanet / .00001, 0, 1);
                float adjNegCancel = clamp(adjacent / .000001, 0, 1);


                float totalDist = distInFog + min(distInFog, adjacent) - ((distInPlanet + distInFog) * planetCancel * adjNegCancel);
                float startDistFromCenter = min(adjacent, distInFog);

                float3 startPosFog = fogRayStart + (uvForward * (adjacent - startDistFromCenter));
                float3 endPosFog = startPosFog + (uvForward * totalDist);
                float3 midPosFog = startPosFog + (uvForward * totalDist) / 2;

                //dayNight stuff
                float dayNightEnter = clamp(
                    dot(
                        normalize(
                            startPosFog - _PlanetWorldOrigin
                        ),
                        normalize(
                            -_SunlightDir
                        )
                    ), -1, 1
                );
               
                

                float dayNightExit = clamp(
                    dot(
                        normalize(
                            endPosFog - _PlanetWorldOrigin
                        ),
                        normalize(
                            -_SunlightDir
                        )
                    ), -1, 1
                );
                //convert [-1, 1] to [0, 1] - clamp to make it night at horizon, not back
                /*float midnightNight = (-clamp(dayNightEnter, -1, 0) + -clamp(dayNightExit - 1, 0)) / 2, 0, 1);
                dayNightEnter = clamp(dayNightEnter, 0, 1);
                dayNightExit = clamp(dayNightExit, 0, 1);
                float dayNight = clamp((dayNightEnter + dayNightExit) /2, 0, 1);*/

                float dayNight = (((dayNightEnter + dayNightExit) / 2) + 1) / 2;


                //depth fading stuff
                float sceneZInFog = sceneZ - max(0,adjacent - distInFog);
                float fogAmt = (max(0, min(sceneZInFog, totalDist) - _FogMinDist)) / (_FogMaxDist - _FogMinDist);
                float depthPow = lerp(_DepthPowSurface, _DepthPowSpace, i.amtInSpace);
                float depthFactor = lerp(_DepthFactorSurface, _DepthFactorSpace, i.amtInSpace);
                float depthFading = saturate((abs(pow(fogAmt, depthPow))) / depthFactor);

                ///sunset stuff
                float maxSunTravelDist = 2 * sqrt(pow(_AtmosphereMaxRadius, 2) - pow(_PlanetSurfaceRadius, 2));
                float amtTowardSun = 
                    pow(
                        clamp(
                            dot(
                                uvForward,
                                -normalize(
                                    _SunlightDir
                                )
                            ), 0, 1
                        ),
                        //this tightens things up as we leave the planet
                        1 + (
                            500 * clamp((distPlanet-(_PlanetSurfaceRadius+10)) / (2 * _AtmosphereMaxRadius), 0, 1)
                        )
                    )
                    ;

                //startPosFog sunAmt
                //ss - sunstart
                float ssTheta = acos(dot(-normalize(_SunlightDir), normalize(_PlanetWorldOrigin - startPosFog)));
                float ssDistToPlanet = distance(_PlanetWorldOrigin, startPosFog);
                float ssOpposite = ssDistToPlanet * sin(ssTheta);
                float ssAdjacent = ssDistToPlanet * cos(ssTheta);

                float ssDistInFog = sqrt(max(0, pow(_AtmosphereMaxRadius, 2) - pow(ssOpposite, 2)));
                float ssDistInPlanet = sqrt(max(0, pow(_PlanetSurfaceRadius, 2) - pow(ssOpposite, 2)));

                //0 if planet has value, otherwise 1
                float ssNoPlanetCancel = clamp(ssDistInPlanet / .000001, 0, 1);
                float ssAdjNegCancel = clamp(ssAdjacent / .000001, 0, 1);
                
                
                //float ssTotalDist = ssDistInFog + min(ssDistInFog, ssAdjacent) - ((ssDistInPlanet + ssDistInFog) * ssNoPlanetCancel * ssAdjNegCancel);
                //that worked perfectly... too perfect as there was a sudden drop off when ss stepped out of the sun light.
                //we need to fade, so we just subtract based on P
                //float ssPlanetGradualCancel = clamp(ssDistInPlanet / 50, 0, 1);
                //just increase the first number as much as you want.  the more, the quicker the transition.
                float ssPlanetGradualCancel = clamp(20*(1 - sin(acos(ssDistInPlanet / (_PlanetSurfaceRadius)))), 0, 1);

                float ssTotalDist = ssDistInFog + min(ssDistInFog, ssAdjacent) - ((ssDistInPlanet + ssDistInFog) * ssPlanetGradualCancel * ssNoPlanetCancel * ssAdjNegCancel);
                ssTotalDist = ssTotalDist * (1 - ssNoPlanetCancel);
                //float ssAmt = ssTotalDist/maxSunTravelDist;
                //this worked great, but we want full blown sunset when standing on the planet
                float ssAmt = clamp(ssTotalDist / (maxSunTravelDist * .1), 0, 1);





             //midPosFog smTotalDist
                ///////////////start here!
                float smTheta = acos(dot(-normalize(_SunlightDir), normalize(_PlanetWorldOrigin - midPosFog)));
                float smDistToPlanet = distance(_PlanetWorldOrigin, midPosFog);
                float smOpposite = smDistToPlanet * sin(smTheta);
                float smAdjacent = smDistToPlanet * cos(smTheta);

                float smDistInFog = sqrt(max(0, pow(_AtmosphereMaxRadius, 2) - pow(smOpposite, 2)));
                float smDistInPlanet = sqrt(max(0, pow(_PlanetSurfaceRadius, 2) - pow(smOpposite, 2)));

                //0 if planet has value, otherwism 1
                float smNoPlanetCancel = clamp(smDistInPlanet / .000001, 0, 1); //JUST READDED WAS 0
                float smAdjNegCancel = 1;//clamp(smAdjacent / .000001, 0, 1);

                //float smTotalDist = smDistInFog + min(smDistInFog, smAdjacent) - ((smDistInPlanet + smDistInFog) * smNoPlanetCancel * smAdjNegCancel);
                //that worked perfectly... too perfect as there was a sudden drop off when ss stepped out of the sun light.
                //we need to fade, so we just subtract basmd on P
                //float smPlanetGradualCancel = clamp(smDistInPlanet / 500, 0, 1);
                //that worked okay, but sunsmt would flicker out of existence when it crossmd into shadow instead of fade.
                //just increasm the first number as much as you want.  the more, the quicker the transition.
                float smPlanetGradualCancel = clamp(20 * (1 - sin(acos(smDistInPlanet / (_PlanetSurfaceRadius)))), 0, 1);

                float smTotalDist = smDistInFog + min(smDistInFog, smAdjacent) - ((smDistInPlanet + smDistInFog) * smPlanetGradualCancel * smNoPlanetCancel * smAdjNegCancel);
                smTotalDist = smTotalDist * smPlanetGradualCancel /*JUSTREADDED:*/ * (1 - smNoPlanetCancel);
                //float smAmt = smTotalDist / maxSunTravelDist;
                //this worked great, but we want full blown sunset when standing on the planet
                float smAmt = clamp(smTotalDist / (maxSunTravelDist * .1), 0, 1);





            //endPosFog seTotalDist
                ///////////////start here!
                float seTheta = acos(dot(-normalize(_SunlightDir), normalize(_PlanetWorldOrigin - endPosFog)));
                float seDistToPlanet = distance(_PlanetWorldOrigin, endPosFog);
                float seOpposite = seDistToPlanet * sin(seTheta);
                float seAdjacent = seDistToPlanet * cos(seTheta);

                float seDistInFog = sqrt(max(0, pow(_AtmosphereMaxRadius, 2) - pow(seOpposite, 2)));
                float seDistInPlanet = sqrt(max(0, pow(_PlanetSurfaceRadius, 2) - pow(seOpposite, 2)));

                //0 if planet has value, otherwise 1
                float seNoPlanetCancel = clamp(seDistInPlanet / .000001, 0, 1);
                float seAdjNegCancel = clamp(seAdjacent / .000001, 0, 1);

                //float seTotalDist = seDistInFog + min(seDistInFog, seAdjacent) - ((seDistInPlanet + seDistInFog) * seNoPlanetCancel * seAdjNegCancel);
                //that worked perfectly... too perfect as there was a sudden drop off when ss stepped out of the sun light.
                
                //we need to fade, so we just subtract based on P
                //float sePlanetGradualCancel = clamp(seDistInPlanet / 500, 0, 1);
                //that worked okay, but sunset would flicker out of existence when it crossed into shadow instead of fade.
                //just increase the first number as much as you want.  the more, the quicker the transition.
                float sePlanetGradualCancel = clamp(20 * (1 - sin(acos(seDistInPlanet / (_PlanetSurfaceRadius)))), 0, 1);

                float seTotalDist = seDistInFog + min(seDistInFog, seAdjacent) - ((seDistInPlanet + seDistInFog)  * sePlanetGradualCancel * seNoPlanetCancel * seAdjNegCancel);
                seTotalDist = seTotalDist * (1-seNoPlanetCancel);
                //float seAmt = seTotalDist/maxSunTravelDist;
                
                //this worked great, but we want full blown sunset when standing on the planet
                float seAmt = clamp(seTotalDist / (maxSunTravelDist * .1),0,1);

                
                //take 'er on home
                float sunsetAmt = clamp(max(max(ssAmt, smAmt) , seAmt) * amtTowardSun * depthFading, 0, 1);
                float4 sunsetColor = lerp(_RimColorNight, _RimColorDay, (cos(3.14159 * (1 - dayNight)) + 1) / 2);               
                float4 dayNightColor = lerp(_NightColor, _DayColor, (cos(3.14159*(1 - dayNight))+1)/2);
                float4 atmosphereColor = (dayNightColor*depthFading) + (sunsetColor * sunsetAmt);//lerp(dayNightColor, sunsetColor, sunsetAmt);
                return 
                    //lerp(_NightColor, _DayColor, sunsetAmt)
                    //atmosphereColor
                    //dayNightColor
                    //* depthFading    
                    //ssNoPlanetCancel +
                    seAmt
                    //atmosphereColor
                ;

                
            }
            ENDCG
        }
    }
}
