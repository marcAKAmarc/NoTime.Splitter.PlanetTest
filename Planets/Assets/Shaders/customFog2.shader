Shader "Custom/ScreenSpaceFog2"
{

    //after muddling through this on my own, I came back to tweak and understand it.  I couldn't have picked it back up without also looking at this:
    //https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection.html
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

            static const float PI = 3.14159;

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
                float3 viewDir : TEXCOORD7;
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
                o.viewDir = unity_CameraToWorld._m02_m12_m22;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 uvForward = normalize(i.uvPos.xyz - _WorldSpaceCameraPos.xyz);
                // get linear depth from the depth
                
                //hint: the /dot(uvForward, i.viewDir) is to take LinearEyeDepth from View depth (orthogonic - from plane to plane) to radial (from camera to point)
                //...this comes from trig - Linear eye depth is adjacent, then we calculate the theta between uvForward and viewDir acos(dot(...)) and then we do trig -
                //...Adj/cos(theta) = H (h is radial distance).  Cos and Acos cancel eachother out, so we are left with dot(...);
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.projPos.xy / i.projPos.w))/dot(uvForward, i.viewDir);

                

                //get dist to planet
                float3 fogRayStart = _WorldSpaceCameraPos;
                float distPlanet = distance(fogRayStart, _PlanetWorldOrigin);

                float3 theta = acos(dot(uvForward, normalize(_PlanetWorldOrigin - fogRayStart)));

                float opposite = distPlanet * sin(theta);

                float adjacent = distPlanet * cos(theta); //could be optimized to just dot(uvFor, normal(_planetworkdorgin - fograystart));
                
                float distInFog = sqrt(   max(0,pow(_AtmosphereMaxRadius, 2) - pow(opposite, 2)));
                
                float atmoDensity = ((1 - (
                    (clamp(opposite, _PlanetSurfaceRadius, _AtmosphereMaxRadius) - _PlanetSurfaceRadius)
                    / (_AtmosphereMaxRadius - _PlanetSurfaceRadius)
                    )));
                


                float totalDist = distInFog + min(distInFog, adjacent);

                float3 startPosFog = fogRayStart + (uvForward * (adjacent - min(adjacent, distInFog)));
                float3 endPosFog = startPosFog + (uvForward * totalDist);
                float3 midPosFog = startPosFog + (uvForward * totalDist) / 2;
                
                //depth fading
                float sceneZInFog = sceneZ - max(0, adjacent - distInFog);
                float fogAmt = (max(0, min(/*start scene z in fog*/sceneZ - max(0, adjacent - distInFog)/*end scene z in fog*/, totalDist) - _FogMinDist)) / (_FogMaxDist - _FogMinDist);
                float depthPow = lerp(_DepthPowSurface, _DepthPowSpace, i.amtInSpace);
                float depthFactor = lerp(_DepthFactorSurface, _DepthFactorSpace, i.amtInSpace);
                float depthFading = 
                    saturate(
                        (
                            abs(
                                pow(
                                    /*start fog amt*/(
                                        max(
                                            0, 
                                            min(
                                                /*start scene z in fog*/sceneZ - max(
                                                    0, adjacent - distInFog
                                                )/*end scene z in fog*/
                                                , totalDist
                                            ) - _FogMinDist
                                        )
                                    ) / (_FogMaxDist - _FogMinDist)  
                                    /*end fog amt*/, 
                                    depthPow
                                )
                            )
                        ) / depthFactor
                    );






                float3 endPosFogClipped = ((endPosFog - startPosFog) * depthFading) + startPosFog;
                //dayNight stuff
                float dayNight = (
                    (
                        (
                            /*day night enter*/
                            clamp(
                                dot(
                                    normalize(
                                        startPosFog - _PlanetWorldOrigin
                                    ),
                                    normalize(
                                        -_SunlightDir
                                    )
                                ), -1, 1
                            )
                            +
                            /*day night exit*/
                            clamp(
                                dot(
                                    normalize(
                                        endPosFogClipped - _PlanetWorldOrigin
                                    ),
                                    normalize(
                                        -_SunlightDir
                                    )
                                ), -1, 1
                            )
                        ) / 2
                    ) + 1
                ) / 2;




                ///sunset stuff
                float maxSunTravelDist = 2 * sqrt(pow(_AtmosphereMaxRadius, 2) - pow(_PlanetSurfaceRadius, 2));
                float amtTowardSun = pow(
                    (
                        dot(
                            uvForward,
                            -normalize(
                                _SunlightDir
                            )
                        ) + 1
                        ) / 2,
                    //this tightens things up as we leave the planet
                    1 + (
                        500 * clamp((distPlanet - (_PlanetSurfaceRadius + 10)) / (2 * _AtmosphereMaxRadius), 0, 1)
                        )
                );
                

                //startPosFog sunAmt
                //ss - sunstart
                float ssTheta = acos(dot(-normalize(_SunlightDir), normalize(_PlanetWorldOrigin - startPosFog)));
                float ssDistToPlanet = distance(_PlanetWorldOrigin, startPosFog);
                float ssOpposite = ssDistToPlanet * sin(ssTheta);
                float ssAdjacent = ssDistToPlanet * cos(ssTheta);

                float ssDistInFog = sqrt(max(0, pow(_AtmosphereMaxRadius, 2) - pow(ssOpposite, 2)));

                //0 if planet has value, otherwise 1
                float ssNoPlanetCancel = clamp((_PlanetSurfaceRadius - ssOpposite) / 20, 0, 1);              
                float ssTotalDist = ((min(ssDistInFog, ssAdjacent) + ssDistInFog)) * (1 - ssNoPlanetCancel);
                float ssAmt = clamp(ssTotalDist/maxSunTravelDist,0,1);






             //midPosFog smTotalDist
                ///////////////start here!
                float smTheta = acos(dot(-normalize(_SunlightDir), normalize(_PlanetWorldOrigin - midPosFog)));
                float smDistToPlanet = distance(_PlanetWorldOrigin, midPosFog);
                float smOpposite = smDistToPlanet * sin(smTheta);
                float smAdjacent = smDistToPlanet * cos(smTheta);

                float smDistInFog = sqrt(max(0, pow(_AtmosphereMaxRadius, 2) - pow(smOpposite, 2)));

                //0 if planet has value, otherwism 1
                
                float smTotalDist =
                    ((min(smDistInFog, smAdjacent) + smDistInFog)) * (/*planet cancel*/1 - clamp((_PlanetSurfaceRadius - smOpposite) / 20, 0, 1));

                float smAmt = clamp(smTotalDist / maxSunTravelDist, 0, 1);



                
                //take 'er on home
                float sunsetAmt = clamp(max(ssAmt, smAmt) * amtTowardSun , 0, 1);
                float4 sunsetColor = lerp(_RimColorNight, _RimColorDay, pow(max(0, 1.25 * (dayNight - 1) + 1), 2));//(cos(PI * (1 - dayNight)) + 1) / 2);               
                float4 dayNightColor = lerp(_NightColor, _DayColor, pow(max(0, 1.25 * (dayNight - 1) + 1),2));//(cos(PI*(1 - dayNight))+1)/2);
                float4 atmosphereColor = (dayNightColor*(1-sunsetAmt)) + (sunsetColor * sunsetAmt);
                return
                    //atmosphereColor;
                    //atmosphereColor * atmoDensity * float4(1,1,1,depthFading);
                //old good one    
                atmosphereColor * atmoDensity * depthFading;
            }
            ENDCG
        }
    }
}
