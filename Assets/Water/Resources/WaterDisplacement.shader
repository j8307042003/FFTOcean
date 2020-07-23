// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/WaterDisplacement"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _BaseColor("BaseColor", COLOR) = (1.0, 1.0, 1.0, 1.0)
		_Scale("Scale", Float) = 0
        _DisplacementScale("DisplacementScale", Float) = 0.5
		_SSS_Wrap("SSS_Wrap", Range(0, 1)) = 0
		_SSS_Tint("SSS tint", COLOR) = (1.0, 1.0, 1.0, 1.0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        Cull Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
            

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
                fixed4 color : COLOR;
                float3 worldNormal : TEXCOORD1; 
                float3 worldPos : TEXCOORD2;
			};

			sampler2D _MainTex;
            sampler2D _Height;
			sampler2D _Displacement;
            sampler2D _Normal;
			float4 _MainTex_ST;
            float4 _BaseColor;
			float4 _SSS_Tint;
			float _Scale;
            float _DisplacementScale;
			float unitLen;
			float _SSS_Wrap;
			
			v2f vert (appdata v)
			{
				v2f o;
				
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				float sampleScale = 2.0;
                float4 height = tex2Dlod(_Height, float4(o.uv / sampleScale, 0.0, 0.0));
                

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

				float2 samplePos = worldPos.xz / (unitLen) + .5;

				//x = (nLx / N, mLz / M)

				float4 displacement = tex2Dlod(_Displacement, float4(samplePos, 0.0, 0.0));


                worldPos.y += displacement.y * _Scale;
                worldPos.xz += float2(displacement.x, displacement.z) * _DisplacementScale;
                o.worldPos = worldPos;

                v.vertex = mul(unity_WorldToObject, worldPos);
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.color.xyz = height.x * _Scale;


                float4 normalData = tex2Dlod(_Normal, float4(samplePos, 0.0, 0.0));
				float3 normal = normalData.xyz;
                o.worldNormal = normal;
                
				return o;
			}

           
            float Phong(float3 normal, float3 viewdir, float3 lightdir) 
            {                
                float3 halfway = (viewdir + lightdir) / length(viewdir + lightdir);
                float3 HNDot = dot(halfway, normal);
                float intensity = pow(saturate(HNDot), 1);

                return intensity;
            }


			float Fresnel(float etaI, float etaT, float cosThetaI) {
				float sinThetaI = sqrt(max(0, 1 - cosThetaI * cosThetaI));
				float sinThetaT = etaI / etaT * sinThetaI;
				if (sinThetaT >= 1) return 1;
				float cosThetaT = sqrt(max(0, 1 - sinThetaT * sinThetaT));

				float Rparl = (etaT * cosThetaI - etaI * cosThetaT) /
					(etaT * cosThetaI + etaI * cosThetaT);
				float Rparp = (etaI * cosThetaI - etaT * cosThetaT) /
					(etaI * cosThetaI + etaT * cosThetaT);

				return clamp((Rparl * Rparl + Rparp * Rparp) / 2, 0, 1);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				UNITY_APPLY_FOG(i.fogCoord, col);
				col.xyz = float3(i.uv, 0.0);
				col.xyz = i.worldNormal;


				float3 light = _LightColor0.xyz;
				float3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 worldPos = i.worldPos;
				float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				
				float cosTheta = dot(normalize(i.worldNormal.xyz), viewDir);
				float reflective = Fresnel(1, 1.325, cosTheta);

				//diffuse
                float NLDot = saturate(dot(lightDir, i.worldNormal.xyz));
                float intensity = saturate(.5 + NLDot);
                float3 diffuse = saturate(intensity * light * _BaseColor);

				float3 reflectDir = reflect(viewDir, i.worldNormal);
				half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDir);

				float scatterWidth = 0.5;
				float3 scatterColor = float3(0.0, 0.5, 0.0);
				float shininess = 40.0;

				float wrap_diffuse = max(0, (NLDot + _SSS_Wrap) / (1 + _SSS_Wrap));

				float scatter = smoothstep(0.0, scatterWidth, wrap_diffuse) *
					smoothstep(scatterWidth * 2.0, scatterWidth,
						wrap_diffuse);


				scatterColor = scatter * _SSS_Tint.xyz;// *diffuse;
				//scatter = wrap_diffuse;
                //intensity = Phong(i.worldNormal.xyz, viewDir, lightDir);
                //float3 specular = saturate(intensity * light);
				//reflective = 0.0;
				//diffuse = 0;
				float3 specular = skyData.xyz * reflective;
				//scatterColor = 0;
                col.xyz = (diffuse + scatterColor) + specular;
				//col.xyz = reflectDir;
				//col.xyz = reflective;
                //float l = length(_WorldSpaceCameraPos.xz - worldPos.xz) / 100.0;
                //col.xyz = l;
                //col.xyz = n_dir;
                //col.xyz = float;
				return col;
			}
			ENDCG
		}
	}
}
