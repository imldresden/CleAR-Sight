Shader "Unlit/ColorPickerShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SelectedColor("Selected Color", Color) = (1,1,1,1)
        _InnerRadius("Circle Inner Radius", Range(0.0, 1.0)) = 0.75
        _OuterRadius("Circle Outer Radius", Range(0.0, 1.0)) = 1
        [Toggle] _ShowSelection("Show Selected Color on Color Wheel", float) = 1
        [Toggle] _ShowSaturation("Show Saturation on Color Wheel", float) = 1
        [Toggle] _ShowBrightness("Show Brightness on Color Wheel", float) = 1
    }
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _SelectedColor;
            float _ShowSelection;
            float _ShowSaturation;
            float _ShowBrightness;
            float _InnerRadius;
            float _OuterRadius;

            float4 hsv2rgb(float4 c) {
                float3 rgb = clamp(abs(fmod(c.x * 6.0 + float3(0.0, 4.0, 2.0), 6) - 3.0) - 1.0, 0, 1);
                rgb = rgb * rgb * (3.0 - 2.0 * rgb);
                return float4(c.z * lerp(float3(1, 1, 1), rgb, c.y), c.w);
            }

            float4 rgb2hsv(float4 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float4(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x, c.w);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float inner_radius = _InnerRadius; // inner radius of the ring
                float outer_radius = _OuterRadius; // outer radius of the ring
                fixed4 col; // output/result color

                float2 center = float2(0.5,0.5) - i.uv; // shift center of current fragment position for correct color space representation          
                float radius = length(center) * 2.0; // compute the radial distance of the polar coordinates of the current fragment 

                // leave early if the fragment is not in the ring
                if (radius < inner_radius || radius > outer_radius)
                {
                    return fixed4(0,0,0,0);
                }

                // convert the currently selected color to HSV color space
                float4 selected_color_hsv = rgb2hsv(_SelectedColor);

                // compute position of currently selected color in ring
                float selected_color_angle = (selected_color_hsv.x - 0.5) * UNITY_TWO_PI; // h = angle/2pi + 0.5 --> angle = (h-0.5)*2pi
                float selected_color_radius = 0.5 * (inner_radius + ((outer_radius - inner_radius) / 2));
                float2 selected_color_pos = float2(0.5, 0.5) - float2(selected_color_radius * cos(selected_color_angle), selected_color_radius * sin(selected_color_angle));

                //float s = (radius - inner_radius) / (outer_radius - inner_radius); // normalized radius

                if (_ShowSaturation == 0)
                {
                    selected_color_hsv.y = 1;
                }

                if (_ShowBrightness == 0)
                {
                    selected_color_hsv.z = 1;
                }

                float angle = atan2(center.y, center.x); // compute angle of the polar coordinates of the current fragment
                float4 hsv = float4((angle / UNITY_TWO_PI) + 0.5, selected_color_hsv.y, selected_color_hsv.z, selected_color_hsv.w);
                col = hsv2rgb(hsv);

                // if _ShowSelection is true, draw a circle around the currently selected hue
                if (_ShowSelection == 1)
                {
                    float distance = length(i.uv - selected_color_pos);
                    if (distance < (outer_radius - inner_radius) / 4 && distance >(outer_radius - inner_radius) / 4 - 0.01)
                    {
                        return fixed4(1, 1, 1, selected_color_hsv.w);
                    }
                }                

                return col;
            }

            ENDCG
        }
    }
}
