Shader "Hidden/GaussianFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float4 iResolution;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                //col.rgb = 1 - col.rgb;

				// GAUSSIAN BLUR SETTINGS {{{
				float Directions = 32.0; // BLUR DIRECTIONS (Default 16.0 - More is better but slower)
				float Quality = 16.0; // BLUR QUALITY (Default 4.0 - More is better but slower)
				float Size = 8.0; // BLUR SIZE (Radius)
				// GAUSSIAN BLUR SETTINGS }}}			
				

				float2 Radius = Size / iResolution.xy;

				// Normalized pixel coordinates (from 0 to 1)
				//float2 uv = fragCoord / iResolution.xy;
				// Pixel colour
				float4 Color = tex2D(_MainTex, i.uv);

				// Blur calculations
				const float Pi = 3.1415926;
				for (float d = 0.0; d < Pi; d += Pi / Directions)
				{
					for (float j = 1.0 / Quality; j <= 1.0; j += 1.0 / Quality)
					{
						Color += tex2D(_MainTex, i.uv + float2(cos(d), sin(d))*Radius*j);
					}
				}

				// Output to screen
				Color /= Quality * Directions - 15.0;
				col = Color;


                return col;
            }
            ENDCG
        }
    }
}
