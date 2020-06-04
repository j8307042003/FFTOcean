﻿Shader "Unlit/WaterDisplacement"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Scale("Scale", Float) = 0
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _Displacement;
			float4 _MainTex_ST;
			float _Scale;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				float4 displacement = tex2Dlod(_Displacement, float4(o.uv / 1.0, 0.0, 0.0));

				o.vertex.y += displacement.x * _Scale;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				float displacement = tex2D(_Displacement, i.uv).x;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				col.xyz = float3(displacement, displacement, displacement);
				//col.xyz = float3(i.uv, 0.0);
				return col;
			}
			ENDCG
		}
	}
}
