Shader "Hidden/FoamUpdate"
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

			sampler2D  _MainTex;
			sampler2D  _Foam;
			sampler2D _FoamData;
			//SamplerState sampler_Foam;
			float foamScale;
			float foamExistTime;
			float deltaTime;

			#define M_PI 3.1415926
			float erf(float x) {
				float a = 0.140012;
				float x2 = x * x;
				float ax2 = a * x2;
				return sign(x) * sqrt(1.0 - exp(-x2 * (4.0 / M_PI + ax2) / (1.0 + ax2)));
			}


			float whitecapCoverage(float epsilon, float mu, float sigma2) {
				return 0.5*erf((0.5*sqrt(2.0)*(epsilon - mu)*rsqrt(sigma2))) + 0.5;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				//float4 col = tex2D(_FoamData, i.uv);
				const int size = 512.0;

				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				col += tex2Dlod(_FoamData, float4(i.uv, 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(0, 1.0 / size), 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(0, -1.0 / size), 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(1.0 / size, 0.0), 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(-1.0 / size, 0.0), 0, 3));

				col += tex2Dlod(_FoamData, float4(i.uv + float2(1.0 / size, 1.0 / size), 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(1.0 / size, -1.0 / size), 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(-1.0 / size, -1.0 / size), 0, 3));
				col += tex2Dlod(_FoamData, float4(i.uv + float2(-1.0 / size, 1.0 / size), 0, 3));


				col /= 9.0;
				
				//float4 col = _MainTex.Sample(sampler_Foam, i.uv);
				float2 foam = float2(col.x, col.y);
				float jsigma2 = max(foam.y - foam.x * foam.x, 0.0);
			
				float w = whitecapCoverage(foamScale, foam.x, jsigma2);
				w = abs(w);
				w = isnan(w) ? 0.0 : w;
				w = saturate(w);
				//w = jsigma2 * 10000.0;
				//w = foamScale;
				//w = foam.x;
				float foamFadeInterval = deltaTime / foamExistTime;

				float4 foamData = tex2D(_Foam, i.uv);
				//float4 foamData = _Foam.Sample(sampler_Foam, i.uv);
				foamData -= float4(foamFadeInterval, foamFadeInterval, foamFadeInterval, 0);
				foamData = saturate(foamData);
				foamData = max(foamData, float4(w, w, w, 1.0));
				//foamData += float4(w, w, w, 1.0);
				//foamData = float4(w, w, w, 1.0);
				

                return foamData;
            }
            ENDCG
        }
    }
}
