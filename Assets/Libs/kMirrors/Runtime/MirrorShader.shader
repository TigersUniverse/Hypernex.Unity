Shader "Unlit/MirrorShader"
{
    Properties
    {
        _LocalReflectionMap ("Texture", 2D) = "white" {}
        _LeftMap ("Texture", 2D) = "white" {}
        _RightMap ("Texture", 2D) = "white" {}
        _IsXR ("XR", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _LocalReflectionMap;
            sampler2D _LeftMap;
            sampler2D _RightMap;
            // UNITY_DECLARE_SCREENSPACE_TEXTURE(_LeftMap);
            // UNITY_DECLARE_SCREENSPACE_TEXTURE(_RightMap);
            float _IsXR;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = ComputeScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // sample the texture
                fixed2 uv = i.uv.xy / i.uv.w;
                // uv = UnityStereoTransformScreenSpaceTex(uv);
                if (_IsXR == 1.0)
                {
                    fixed4 colL = tex2D(_LeftMap, uv);
                    fixed4 colR = tex2D(_RightMap, uv);
                    // fixed4 colL = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_LeftMap, uv);
                    // fixed4 colR = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_RightMap, uv);
                    // colR = fixed4(0, 1, 0, 1);
                    fixed4 col = lerp(colL, colR, unity_StereoEyeIndex);
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }
                else
                {
                    fixed4 col = tex2D(_LocalReflectionMap, uv);
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }
                // apply fog
            }
            ENDCG
        }
    }
}
