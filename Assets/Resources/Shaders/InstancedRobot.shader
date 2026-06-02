// =============================================================================
// GPU Instanced Robot Renderer
// Renders 4096+ robot instances using GPU instancing for maximum performance
// =============================================================================

Shader "MorpheusGPU/InstancedRobot"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        
        [Header(Environment Coloring)]
        _EnvColorEnabled ("Enable Env Color", Float) = 1
        _ColorVariation ("Color Variation", Range(0,1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup
        #pragma target 4.5

        sampler2D _MainTex;
        sampler2D _NormalMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
        };

        half _Metallic;
        half _Smoothness;
        half _BumpScale;
        fixed4 _Color;
        float _EnvColorEnabled;
        float _ColorVariation;

        // GPU instancing buffers
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct RigidBodyState
            {
                float3 position;
                float _padding0;
                float4 orientation;
                
                float3 linearVelocity;
                float _padding1;
                float3 angularVelocity;
                float _padding2;
                
                float mass;
                float invMass;
                float3 inertiaTensor;
                float _padding3;
                float3 invInertiaTensor;
                float _padding4;
                
                int bodyIndex;
                int envIndex;
                uint flags;
                float _padding5;
            };
            
            StructuredBuffer<RigidBodyState> _RigidBodyStates;
            StructuredBuffer<float4> _EnvironmentColors;
            
            int _MaxBodiesPerEnv;
            int _BodyIndexOffset; // Which body within the robot this mesh represents
            float4x4 _LocalToBodyMatrix; // Transform from mesh local to body local
        #endif

        float4 QuatToMatrix(float4 q, float3 pos)
        {
            float xx = q.x * q.x;
            float yy = q.y * q.y;
            float zz = q.z * q.z;
            float xy = q.x * q.y;
            float xz = q.x * q.z;
            float yz = q.y * q.z;
            float wx = q.w * q.x;
            float wy = q.w * q.y;
            float wz = q.w * q.z;

            float4x4 m;
            m._m00 = 1 - 2 * (yy + zz);
            m._m01 = 2 * (xy - wz);
            m._m02 = 2 * (xz + wy);
            m._m03 = pos.x;
            
            m._m10 = 2 * (xy + wz);
            m._m11 = 1 - 2 * (xx + zz);
            m._m12 = 2 * (yz - wx);
            m._m13 = pos.y;
            
            m._m20 = 2 * (xz - wy);
            m._m21 = 2 * (yz + wx);
            m._m22 = 1 - 2 * (xx + yy);
            m._m23 = pos.z;
            
            m._m30 = 0;
            m._m31 = 0;
            m._m32 = 0;
            m._m33 = 1;
            
            return m;
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint instanceID = unity_InstanceID;
                
                // Calculate which environment and body this instance represents
                uint envIndex = instanceID; // One instance per environment for this body
                uint bodyIndex = _BodyIndexOffset;
                uint stateIndex = envIndex * _MaxBodiesPerEnv + bodyIndex;
                
                RigidBodyState state = _RigidBodyStates[stateIndex];
                
                // Build transformation matrix from body state
                float4x4 bodyMatrix = QuatToMatrix(state.orientation, state.position);
                
                // Combine with local offset matrix
                float4x4 finalMatrix = mul(bodyMatrix, _LocalToBodyMatrix);
                
                unity_ObjectToWorld = finalMatrix;
                unity_WorldToObject = unity_ObjectToWorld; // Simplified inverse
            #endif
        }

        // Hash function for per-environment color variation
        float hash(uint n)
        {
            n = (n << 13U) ^ n;
            n = n * (n * n * 15731U + 789221U) + 1376312589U;
            return float(n & 0x7fffffffU) / float(0x7fffffff);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                if (_EnvColorEnabled > 0.5)
                {
                    uint envIndex = unity_InstanceID;
                    
                    // Generate unique color for each environment
                    float h = hash(envIndex);
                    float3 envColor = float3(
                        0.5 + 0.5 * sin(h * 6.28318 + 0.0),
                        0.5 + 0.5 * sin(h * 6.28318 + 2.094),
                        0.5 + 0.5 * sin(h * 6.28318 + 4.188)
                    );
                    
                    c.rgb = lerp(c.rgb, c.rgb * envColor, _ColorVariation);
                }
            #endif
            
            o.Albedo = c.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_NormalMap, IN.uv_NormalMap), _BumpScale);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
