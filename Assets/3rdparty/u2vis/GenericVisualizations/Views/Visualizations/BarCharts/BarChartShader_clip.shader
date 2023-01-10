Shader "AugDisp/InfoVis/BarChartShader_clip"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
      //  [Toggle] _isSlicing("is Slicing", Float) = 0
            //clipping
            [HDR] _CutoffColor("Cutoff Color", Color) = (1,0,0,0)
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            // LOD 200
             Cull Off

             CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows
        //    #pragma shader_feature _ISSLICING_ON

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            sampler2D _DummyTex;
            sampler2D _MainTex;
            fixed4 _Color;

            // Clipping variables 
            float4 _Plane;
            float4 _CutoffColor;

            struct Input
            {
                float2 uv_MainTex;
                float4 color : Color;
                float facing : VFACE;

                // clipping 
                float3 worldPos;
            };



            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // ---- ENABLES CLIPPING

   // #ifdef _ISSLICING_ON
                float distance = dot(IN.worldPos, _Plane.xyz);
                distance = distance + _Plane.w;

                clip(-distance);
  //  #endif  
                //float facing = IN.facing * 0.5 + 0.5;

                // ----
                // 
                // Albedo comes from a texture tinted by color
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
