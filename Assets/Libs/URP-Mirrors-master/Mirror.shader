Shader "VirtualBrightPlayz/Mirror"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AltTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        // LOD 100
        // ZTest Off
        // Cull Off

        // Stencil
        // {
        //     Ref 1
        //     Comp equal
        //     Pass keep
        // }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                // UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            // UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            float4 _MainTex_ST;
            sampler2D _AltTex;
            // UNITY_DECLARE_SCREENSPACE_TEXTURE(_AltTex);
            float2 _Offset;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                // UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 offset = _Offset;
                screenUV.x *= -1;
                // screenUV.y *= -1;
                if (unity_StereoEyeIndex)
                {
                    // offset.x *= -1;
                }
                fixed4 col = float4(0, 0, 0, 1);
                if (unity_StereoEyeIndex == 1)
                {
                    // col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_AltTex, screenUV);
                    col = tex2D(_AltTex, screenUV);
                }
                else
                {
                    // col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, screenUV);
                    col = tex2D(_MainTex, screenUV);
                }
                return col;
            }
            ENDCG
        }
    }
}
