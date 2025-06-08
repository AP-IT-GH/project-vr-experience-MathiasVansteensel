Shader "Example/URPUnlitShaderTessellated"
{
    Properties
    {
        _Tess("Tessellation", Range(1, 1024)) = 20
        _MaxTessDistance("Max Tess Distance", Range(1, 512)) = 128
        _Falloff("Falloff", Range(0.01, 100)) = 1
        _Weight("Displacement Amount", Range(0, 1)) = 0

        _WaveAmplitude("Wave Amplitude", Float) = 0.5
        _WaveFrequency("Wave Frequency", Float) = 1.0
        _WaveSpeed("Wave Speed", Float) = 1.0

        _SpecColor("Specular Color", Color) = (1,1,1,1)
        _Shininess("Shininess", Float) = 16
        _FresnelStrength("Fresnel Strength", Float) = 1.0

        _ShallowColor("Shallow Water Color", Color) = (0.1, 0.6, 0.8, 1)
        _DeepColor("Deep Water Color", Color) = (0.0, 0.1, 0.3, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparant" "RenderPipeline"="UniversalRenderPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma require tessellation
            #pragma vertex TessellationVertexProgram
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "DiySubdivision.hlsl"

            float _Weight;
            float _Shininess;
            float _FresnelStrength;
            float4 _SpecColor;
            float4 _ShallowColor;
            float4 _DeepColor;

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;

            float3 ApplyGerstnerWave(float3 positionWS, float2 dir, float amplitude, float frequency, float speed, float time, out float3 tangent, out float3 binormal)
            {
                float2 d = normalize(dir);
                float wavePhase = dot(d, positionWS.xz) * frequency + time * speed;

                float cosP = cos(wavePhase);
                float sinP = sin(wavePhase);

                tangent = float3(
                    -d.x * d.x * amplitude * frequency * sinP,
                    d.x * amplitude * frequency * cosP,
                    -d.x * d.y * amplitude * frequency * sinP
                );

                binormal = float3(
                    -d.x * d.y * amplitude * frequency * sinP,
                    d.y * amplitude * frequency * cosP,
                    -d.y * d.y * amplitude * frequency * sinP
                );

                float3 offset;
                offset.x = d.x * amplitude * cosP;
                offset.y = amplitude * sinP;
                offset.z = d.y * amplitude * cosP;

                return offset;
            }

            float3 ApplyGerstnerWaves(float3 pos, out float3 normal)
            {
                float3 totalOffset = 0;
                float3 t1, b1, t2, b2, t3, b3, t4, b4, t5, b5, t6, b6, t7, b7;
                float3 n = float3(0, 0, 0);

                float time = _Time.y; // Use _Time.y = time * 0.01f for better scale

                // Wave 1
                totalOffset += ApplyGerstnerWave(pos, float2(1, 0), _WaveAmplitude * 0.8, _WaveFrequency, _WaveSpeed, time, t1, b1);
                // Wave 2
                totalOffset += ApplyGerstnerWave(pos, float2(0.5, 0.5), _WaveAmplitude * 0.4, _WaveFrequency * 1.5, _WaveSpeed * 1.2, time, t2, b2);
                // Wave 3
                totalOffset += ApplyGerstnerWave(pos, float2(0.2, 1.0), _WaveAmplitude * 0.3, _WaveFrequency * 0.8, _WaveSpeed * 0.8, time, t3, b3);
                // Wave 4
                totalOffset += ApplyGerstnerWave(pos, float2(-0.5, 0.4), _WaveAmplitude * 0.15, _WaveFrequency * 4.35, _WaveSpeed * 4, time, t4, b4);
                // Wave 5
                totalOffset += ApplyGerstnerWave(pos, float2(0.4, 0.8), _WaveAmplitude * 0.096, _WaveFrequency * 9.35, _WaveSpeed * 0.66, time, t5, b5);

                totalOffset += ApplyGerstnerWave(pos, float2(0.8, 0.4), _WaveAmplitude * 0.1, _WaveFrequency * 15, _WaveSpeed * 0.9, time, t6, b6);
                totalOffset += ApplyGerstnerWave(pos, float2(0.7, -0.2), _WaveAmplitude * 0.08, _WaveFrequency * 12, _WaveSpeed * 3, time, t7, b7);
                

                n = normalize(cross(t1 + t2 + t3 + t4 + t5 + t6 + t7, b1 + b2 + b3 + b4 + b5 + b6 + b7));
                normal = n;

                return totalOffset;
            }

            ControlPoint TessellationVertexProgram(Attributes v)
            {
                ControlPoint p;
                p.vertex = v.vertex;
                p.uv = v.uv;
                p.normal = v.normal;
                p.color = v.color;
                return p;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.vertex.xyz);
                float3 waveNormal;
                float3 waveOffset = ApplyGerstnerWaves(worldPos, waveNormal);
                worldPos += waveOffset;

                float3 worldNormal = normalize(waveNormal);
                output.vertex = TransformWorldToHClip(worldPos);
                output.color = input.color;
                output.normalWS = worldNormal;
                output.worldPos = worldPos;
                output.uv = input.uv;

                return output;
            }

            [UNITY_domain("tri")]
            Varyings domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
            {
                Attributes v;

                #define DomainPos(fieldName) v.fieldName = \
                    patch[0].fieldName * barycentricCoordinates.x + \
                    patch[1].fieldName * barycentricCoordinates.y + \
                    patch[2].fieldName * barycentricCoordinates.z;

                DomainPos(vertex)
                DomainPos(uv)
                DomainPos(color)
                DomainPos(normal)

                return vert(v);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(GetCameraPositionWS() - input.worldPos);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 4.0);
                float3 halfwayDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfwayDir)), _Shininess);
                float3 specColor = lightColor * _SpecColor.rgb * spec;

                float depthFactor = saturate(length(input.worldPos - GetCameraPositionWS()) * 0.05);
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);

                float3 finalColor = waterColor;//lerp(waterColor, float3(1,1,1), fresnel * _FresnelStrength);
                finalColor += specColor * 0.5;

                finalColor = finalColor * clamp(dot(normal, lightDir), 0.66, 1.0);

                return half4(finalColor, 0.9);
            }

            ENDHLSL
        }
    }
}
