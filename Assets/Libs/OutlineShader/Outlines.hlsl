#ifndef BACKFACEOUTLINES_INCLUDED
#define BACKFACEOUTLINES_INCLUDED

// Include helper functions from URP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Data from the meshes
struct Attributes {
    float4 positionOS       : POSITION; // Position in object space
    float3 normalOS         : NORMAL; // Normal vector in object space
    UNITY_VERTEX_INPUT_INSTANCE_ID
#ifdef USE_PRECALCULATED_OUTLINE_NORMALS
    float3 smoothNormalOS   : TEXCOORD1; // Calculated "smooth" normals to extrude along in object space
#endif
};

// Output from the vertex function and input to the fragment function
struct VertexOutput {
    float4 positionCS   : SV_POSITION; // Position in clip space
    UNITY_VERTEX_OUTPUT_STEREO
};

// Properties
float _Thickness;
float4 _Color;
float _DepthOffset;

VertexOutput Vertex(Attributes input) {
    UNITY_SETUP_INSTANCE_ID(input); //Insert
    VertexOutput output = (VertexOutput)0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); //Insert 

    float3 normalOS = input.normalOS;
#ifdef USE_PRECALCULATED_OUTLINE_NORMALS
    normalOS = input.smoothNormalOS;
#else
    normalOS = input.normalOS;
#endif

    // Extrude the object space position along a normal vector
    float3 posOS = input.positionOS.xyz + TransformWorldToObjectDir(TransformObjectToWorldDir(normalOS, true), false) * _Thickness;
    // Convert this position to world and clip space
    output.positionCS = GetVertexPositionInputs(posOS).positionCS;

    float depthOffset = _DepthOffset;
    // If depth is reversed on this platform, reverse the offset
#ifdef UNITY_REVERSED_Z
    depthOffset = -depthOffset;
#endif
    output.positionCS.z += depthOffset;

    return output;
}

float4 Fragment(VertexOutput input) : SV_Target{
    return _Color;
}

#endif
