// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Ocean/Gerstner_Wave"
{
	Properties
	{
		_LowColor("BaseLowColor", COLOR) = (1.0,1.0,1.0,1.0)
		_HighColor( "BaseHightColor", COLOR ) = (1.0,1.0,1.0,1.0)
		_Soft( "Soft", Float ) = 0.1
		_UseWave( "_", Float ) = 0.5
		_Diffuse("Diffuse", Color) = (1, 1, 1, 1)
		_Specular("Specular", Color) = (1, 1, 1, 1)
		_Gloss("Gloss", Range(8.0, 256)) = 20
		_Tessellation("Tessellation", Float) = 2
		_MinDistance("Tessellation Min Distance", Range(0, 100)) = 1.0
		_MaxDistance("Tessellation Max Distance", Range(0, 100)) = 1.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode" = "ForwardBase" }
		GrabPass{ "GrabPassTexture" }
		LOD 100

		Pass
		{
			CGPROGRAM
			
			//#pragma hull hs
			//#pragma domain ds
			#pragma target 4.6
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "Tessellation.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float3 normal : NORMAL;
				fixed4 tangent : TANGENT;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				float4 tangent : TEXCOORD4;
				fixed3 normal : TEXCOORD5;
				float4 objectVertex : TEXCOORD6;
			};

			float4 _MainTex_ST;
			float4 _HighColor;
			float4 _LowColor;
			float WaveCount;

			float _Soft;

			float4 _DirectionArray[10];
			float _SteepnessArray[10];
			float _AmplitudeArray[10];
			float _WaveLengthArray[10];
			float _SpeedArray[10];
			float _WaveKindArray[10];

			float _UseWave;

			fixed4 _Diffuse;
			fixed4 _Specular;
			float _Gloss;
			float _Tessellation;
			float _MinDistance;
			float _MaxDistance;
			sampler2D GrabPassTexture;

			float hash(float2 p) {
				float h = dot(p, float2(127.1, 311.7));
				return frac(sin(h)*43758.5453123);
			}
			float noise(in float2 p) {
				float2 i = floor(p);
				float2 f = frac(p);
				float2 u = f*f*(3.0 - 2.0*f);
				return -1.0 + 2.0*lerp(lerp(hash(i + float2(0.0, 0.0)),
					hash(i + float2(1.0, 0.0)), u.x),
					lerp(hash(i + float2(0.0, 1.0)),
						hash(i + float2(1.0, 1.0)), u.x), u.y);
			}

			float3 Gerstner( float2 coord, float2 direction, float steepness, float amplitude, float waveLength, float speed, float time, out float3 binormal, out float3 tagent, out float3 normal) {
				float steepnessAmplitude = steepness * amplitude;
				float2 directDotPos = dot( direction, coord );

				float triParam = dot(waveLength * direction, coord ) + speed  * time ;
				float cosValue = cos( triParam );
				float sinValue = sin( triParam );

				float xyValue = steepnessAmplitude * cosValue ;

				float WA = waveLength * amplitude;

				float WA_Sin = WA*sinValue;
				float WA_Cos = WA*cosValue;

				// binormal
				binormal = float3(steepness * pow(direction.x, 2) * WA_Sin, direction.x * WA_Cos, steepness * direction.x * direction.y * WA_Sin);
				
				tagent = float3(steepness * direction.x * direction.y * WA_Sin, direction.y * WA_Cos, steepness * pow(direction.y, 2) * WA_Sin);
		
				normal = float3(direction.x * WA_Cos, steepness*WA_Sin, direction.y * WA_Cos);

				return float3( direction.r * xyValue,  amplitude * sinValue, direction.g * xyValue);
			}
			
			v2f vert (appdata v)
			{
				
				v2f o;
				
				
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 pos;

				
				float3 SumBinormal, SumTagent, SumNormal;
				for (int i = 0; i < WaveCount; i++) {
					float3 binormal, tagent, normal;
					int kind = _WaveKindArray[i];
					float2 vpos = worldPos.rb;
					float2 dir = _DirectionArray[i].rg;
					if (kind == 1.0) {
						vpos -= _DirectionArray[i].zw;
						dir = normalize(vpos);
					}
					pos += Gerstner(vpos, -dir, _SteepnessArray[i], _AmplitudeArray[i], _WaveLengthArray[i], _SpeedArray[i], _Time.g, binormal, tagent, normal);
					SumBinormal += binormal;
					SumTagent += tagent;
					SumNormal += normal;
				}

				float3 binormal = float3(1 - SumBinormal.x, SumBinormal.y, -SumBinormal.z);
				float3 tagent = float3(-SumTagent.x, SumTagent.y, 1 - SumTagent.z);
				float3 normal = float3(-SumNormal.x, 1 - SumNormal.y, -SumNormal.z);

				o.worldNormal = mul(v.normal, (float3x3)unity_WorldToObject);
				o.worldNormal = mul(normal, (float3x3)unity_WorldToObject);
				pos += float3(worldPos.r, 0.0, worldPos.b);

				if ( _UseWave > 0.01)
					v.vertex.rgb = mul(unity_WorldToObject, float4(pos, worldPos.a)).rgb;
			
				float noiseValue = noise(v.texcoord);
				v.vertex.rgb += float3(noiseValue, 0.0, noiseValue);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = lerp(_HighColor, _LowColor, pos.g * _Soft);
				o.tangent = v.tangent;
				o.normal = v.normal;
				o.objectVertex = v.vertex;
				o.color = float4(0.0, 0.0 ,0.0, 0.0);

				o.screenPos = ComputeScreenPos(o.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			

//#ifdef UNITY_CAN_COMPILE_TESSELLATION
			struct InternalTessInterp_appdata {
				float4 vertex : INTERNALTESSPOS;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};

			InternalTessInterp_appdata tessvert(appdata v) {
				InternalTessInterp_appdata o;
				o.vertex = v.vertex;
				o.tangent = v.tangent;
				o.normal = v.normal;
				o.texcoord = v.texcoord;
				return o;
			}

			float4 Tessellation(v2f v, v2f v1, v2f v2) {
				return UnityDistanceBasedTess(v.objectVertex, v1.objectVertex, v2.objectVertex, _MinDistance, _MaxDistance, _Tessellation);
			}

			UnityTessellationFactors hsconst(InputPatch<v2f, 3> v) {
				UnityTessellationFactors o;
				float4 tf;
				tf = Tessellation(v[0], v[1], v[2]);
				//tf = float4(4.0f, 4.0f, 4.0f, 4.0f);
				o.edge[0] = tf.x;
				o.edge[1] = tf.y;
				o.edge[2] = tf.z;
				o.inside = tf.w;
				return o;
			}

			[UNITY_domain("tri")]
			[UNITY_partitioning("fractional_odd")]
			[UNITY_outputtopology("triangle_cw")]
			[UNITY_patchconstantfunc("hsconst")]
			[UNITY_outputcontrolpoints(3)]
			v2f hs(InputPatch<v2f, 3> v, uint id : SV_OutputControlPointID) {
				return v[id];
			}

			[UNITY_domain("tri")]
			v2f ds(UnityTessellationFactors tessFactors, const OutputPatch<v2f, 3> vi, float3 bary : SV_DomainLocation) {
				appdata v;

				v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
				v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
				v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
				v.texcoord = vi[0].uv*bary.x + vi[1].uv*bary.y + vi[2].uv*bary.z;

				v2f o = vert(v);
				return o;
			}
//#endif


			fixed4 frag (v2f i) : SV_Target
			{
				//return fixed4(1.0f,1.0f,1.0f,1.0f);

				// sample the texture
				fixed4 col = i.color;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);


				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);

				// Get the view direction in world space
				fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
				// Get the half direction in world space
				fixed3 halfDir = normalize(worldLightDir + viewDir);
				// Compute specular term

				fixed3 grabpass_color = tex2D(GrabPassTexture,
					i.screenPos.xy / i.screenPos.w).rgb;


				float3 reflectDir = normalize(reflect(-viewDir, worldNormal));
				//float3 reflectDir = normalize(reflect(-worldLightDir, worldNormal));

				float3 cubeColor = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectDir).rgb;

				float3 H = normalize(reflectDir + (-viewDir));
				float power = 32.0;
				float specVal = pow(saturate(dot(H, worldNormal)), power);
				float base = 1 - dot(-viewDir, H);
				float exponential = pow(base, 5.0);
				float F0 = 0.6;
				float fresnel = exponential + F0 * (1.0 - exponential);
				specVal *= fresnel;
				grabpass_color *= fresnel;

				//fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);


				return col + float4(/*specular + */saturate(cubeColor *  (1 -dot(worldNormal, viewDir))), 0.0) *( 1- fresnel) + float4(grabpass_color, 0.0) ;
			}
			ENDCG
		}
	}
}
