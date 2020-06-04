Shader "Unlit/height"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MainColor( "Color", Color ) = ( 0.0, 0.0, 1.0, 0.0 )
		_Offset( "Offset", Vector ) = ( 0.0, 0.0, 0.0, 0.0 )
		_HeightTex("Texture", 2D) = "white" {}
		_Skybox("Skybox", CUBE) = "" {}
		_Scale( "Float", Float ) = 30.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members height)
//#pragma exclude_renderers d3d11
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};

			sampler2D _MainTex;
			sampler2D _HeightTex;
			samplerCUBE  _Skybox;
			float4 _MainColor;
			float4 _MainTex_ST;
			float _Scale;
			float4 _Offset;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//_height = tex2Dlod(_HeightTex, float4(o.uv.xy, 0, 0 )).x;
				float height = (tex2Dlod(_HeightTex, float4(o.uv.xy+_Offset.xy, 0, 0)).x) * _Scale ;
				
				//o.vertex.y += _height * _Scale;
				

				o.vertex.y += height ;
				//o.normal = float3(transpose(inverse(UNITY_MATRIX_IT_MV))) * v.normal;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				fixed height = tex2D(_HeightTex, i.uv+ _Offset.xy) /5 ;
				//col = fixed4( height, height, 1.0, 1.0);
				fixed4 col = _MainColor;
				
				//fixed color = (dot(UNITY_MATRIX_IT_MV[2].xyz, i.normal) < 0.5) ? 1.0 : 0.0;

				//col += fixed4(color, color, 0.0, 0.0);
				col += fixed4(height, height, 0.0, 0.0);
				col += float4(texCUBE(_Skybox, float3(0.0, 1.0, 0.0)).rgb, 0.0);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
