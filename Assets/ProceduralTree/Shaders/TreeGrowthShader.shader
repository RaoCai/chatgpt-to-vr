Shader "Custom/TreeGrowthShader"
{
    Properties
    {
        _MainTex ("Bark Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [NoScaleOffset] _BranchData ("Branch Data", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow nolightmap nometa noforwardadd
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        sampler2D _BranchData;

        struct Input
        {
            float2 uv_MainTex : TEXCOORD0;
            float height : TEXCOORD1;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.height = saturate(v.vertex.y / 10.0);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 branchUV = float2(IN.height, 0.5);
            fixed4 branchData = tex2D(_BranchData, branchUV);

            if (branchData.r < IN.height)
                discard;

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}