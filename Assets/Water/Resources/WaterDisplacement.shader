// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/WaterDisplacement"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _BaseColor("BaseColor", COLOR) = (1.0, 1.0, 1.0, 1.0)
		_Scale("Scale", Float) = 0
        _DisplacementScale("DisplacementScale", Float) = 0.5
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
			float _Scale;
            float _DisplacementScale;
			
			v2f vert (appdata v)
			{
				v2f o;
				
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				float4 height = tex2Dlod(_Height, float4(o.uv / 5.0, 0.0, 0.0));
                float4 displacement = tex2Dlod(_Displacement, float4(o.uv / 5.0, 0.0, 0.0));

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                worldPos.y += height.x * _Scale;
                worldPos.xz += float2(displacement.x, displacement.z) * _DisplacementScale;
                o.worldPos = worldPos;
                v.vertex = mul(unity_WorldToObject, worldPos);
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.color.xyz = height.x * _Scale;


                float4 normalData = tex2Dlod(_Normal, float4(o.uv / 5.0, 0.0, 0.0));
                float2 n = float2(normalData.x, normalData.z);
                float3 n_dir = float3(n.x, 0, n.y)*70000;
                
                //float3 normal = (float3(0, 1, 0) - n_dir) / sqrt(1+dot(n_dir, n_dir));
                float3 normal = normalize(float3(0, 1, 0) - n_dir);
                normal.z = 0;
                normal.y = 0;
                //float3 normal = normalData.xyz;

float nScale = 1.0;
float uPixel = 1.0 / 512.0;
float vPixel = 1.0 / 512.0;
float4 uv = float4(o.uv / 5.0, 0, 0);
float height_pu = tex2Dlod(_Displacement, uv + float4(uPixel, 0, 0, 0)).y;
float height_mu = tex2Dlod(_Displacement, uv - float4(uPixel, 0, 0, 0)).y;
float height_pv = tex2Dlod(_Displacement, uv + float4(0, vPixel, 0, 0)).y;
float height_mv = tex2Dlod(_Displacement, uv - float4(0, vPixel, 0, 0)).y;
float du = height_mu - height_pu;
float dv = height_mv - height_pv;
float3 N = normalize(float3(du, dv, 1.0/nScale));
normal = N;
normal = float3(N.x, N.z, N.y);

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
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				float displacement = tex2D(_Displacement, i.uv / 5.0).x;
                i.color.x = displacement * .6;
				UNITY_APPLY_FOG(i.fogCoord, col);
				//col.xyz = float3(displacement, displacement, displacement);
				col.xyz = float3(i.uv, 0.0);
                //col.xyz = i.color.xyz * float3(0,0,.7);
                //col.xyz = lerp(float3(1, 1, 1) * .9, float3(0,0,.75), i.color.x);
                //col.xyz = lerp(float3(0,0,.75), float3(1, 1, 1) * .9, i.color.x);
                //col.xyz = lerp(float3(0, 1, .7) * .9, float3(0,.5,.75), i.color.x);
                float d = displacement ;
                
                col.xyz = i.worldNormal;



                float3 light = float3(1, 1, 1);
                float3 lightDir = normalize(float3(1, 1, 0));
                float3 worldPos = i.worldPos;
                //float3 lightColor = float3()
                //lighting
                //col.xyz = _BaseColor;

                //diffuse
                float NLDot = saturate(dot(lightDir, i.worldNormal.xyz));
                float intensity = saturate(.5 + NLDot);
                float3 diffuse = saturate(intensity * light * _BaseColor);


                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                
                intensity = Phong(i.worldNormal.xyz, viewDir, lightDir);
                float3 specular = saturate(intensity * light);
                specular = 0;
                
                col.xyz = diffuse + specular;

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
