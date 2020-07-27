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
        //Cull Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "UnityGlobalIllumination.cginc"

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

				//float2 samplePos = worldPos.xz / (unitLen) + .5;

				
				//float width, height;
				//_Displacement.GetDimensions(width, height);
				float2 samplePos = frac(worldPos.xz / (unitLen));

				//x = (nLx / N, mLz / M)

				float4 displacement = tex2Dlod(_Displacement, float4(samplePos, 0.0, 0.0));


                worldPos.y += displacement.y * _Scale;
				o.worldPos = worldPos;
                worldPos.xz -= float2(displacement.x, displacement.z) * _DisplacementScale;
                //o.worldPos = worldPos;

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

				float2 samplePos = frac(i.worldPos.xz / (unitLen));
				float4 normalData = tex2D(_Normal, samplePos);
				//i.worldNormal = normalData.xyz;
				col.xyz = i.worldNormal / 2 + 0.5;


				float3 light = _LightColor0.xyz;
				float3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 worldPos = i.worldPos;
				float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				
				//Fresnel
				float cosTheta = dot(normalize(i.worldNormal.xyz), viewDir);
				float reflective = Fresnel(1, 1.325, cosTheta);
				reflective = smoothstep(0, 1, reflective);

				//diffuse			
				Unity_GlossyEnvironmentData glossyEnvData = UnityGlossyEnvironmentSetup(.9, i.worldNormal.xyz, i.worldNormal.xyz, 1.325);
				glossyEnvData.reflUVW = i.worldNormal.xyz;
				float3 diffuse = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), float4(1, 1, 1, 1), glossyEnvData).xyz * light;

				//Specular
				float3 reflectDir = reflect(viewDir, i.worldNormal);
				float3 dirLightSpecular = pow(saturate(dot(-reflectDir, _WorldSpaceLightPos0.xyz)), 1) * light;
				glossyEnvData.reflUVW = -reflectDir;
				glossyEnvData.reflUVW.y = abs(glossyEnvData.reflUVW.y);
				half3 skyData = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), float4(1, 1, 1, 1), glossyEnvData).xyz;				


				//Subsurface Scattering
				float scatterWidth = 0.5;
				float3 scatterColor = float3(0.0, 0.5, 0.0);

				float3 H = normalize(-i.worldNormal.xyz + _WorldSpaceLightPos0.xyz);
				float ViewDotH = pow(saturate(dot(viewDir, -H)), 1) * 30 * _SSS_Wrap;
				float3 waveColor = saturate(_SSS_Tint.xyz * ViewDotH * light);
				scatterColor = waveColor;



				//Blend Color
				float3 specular = (dirLightSpecular + skyData.xyz * length(light)) * reflective;
                col.xyz = (diffuse + scatterColor) * ( 1 - reflective) + specular;

				//col.xyz += length(i.worldNormal.xz);

				return col;
			}
			ENDCG
		}
	}
}
