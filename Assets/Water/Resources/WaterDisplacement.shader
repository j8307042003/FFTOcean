Shader "Unlit/WaterDisplacement"
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
			};

			sampler2D _MainTex;
            sampler2D _Height;
			sampler2D _Displacement;
			float4 _MainTex_ST;
			float _Scale;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				float4 height = tex2Dlod(_Height, float4(o.uv / 1.0, 0.0, 0.0));
                float4 displacement = tex2Dlod(_Displacement, float4(o.uv / 1.0, 0.0, 0.0));

                o.vertex.y += height.x * _Scale;
                o.vertex.xz += float2(displacement.x, displacement.z) * _Scale * 0.4;
                //o.vertex.x += displacement.x * _Scale;
				//o.vertex.xz += float2(3, 3) * _Scale;
                o.color.xyz = height.x * _Scale;
				return o;
			}
            
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				float displacement = tex2D(_Displacement, i.uv).x;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				//col.xyz = float3(displacement, displacement, displacement);
				col.xyz = float3(i.uv, 0.0);
                //col.xyz = i.color.xyz * float3(0,0,.7);
                //col.xyz = lerp(float3(1, 1, 1) * 1, float3(0,0,.7), i.color.x);
                float d = displacement ;
                
                //d *= 1000;
                //col.xyz = float3(d, d,d);
				return col;
			}
			ENDCG
		}
	}
}
