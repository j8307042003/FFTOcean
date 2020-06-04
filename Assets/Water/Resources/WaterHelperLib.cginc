


/*
float3 Gerstner(float pos, float t) 
{
	const float k = 2.0;
	const float o = 1.0;
	float x = acos(k * pos.y - t * 0.567143 + o);
	float y = pos.x + (1.0);

	return float3(0.0, 0.0, 0.0);
}
*/

float3 Gerstner(float2 coord, float2 direction, float steepness, float amplitude, float waveLength, float speed, float time, out float3 binormal, out float3 tagent, out float3 normal) {
	float steepnessAmplitude = steepness * amplitude;
	float2 directDotPos = dot(direction, coord);

	float triParam = dot(waveLength * direction, coord) + speed * time;
	float cosValue = cos(triParam);
	float sinValue = sin(triParam);

	float xyValue = steepnessAmplitude * cosValue;

	float WA = waveLength * amplitude;

	float WA_Sin = WA * sinValue;
	float WA_Cos = WA * cosValue;

	// binormal
	binormal = float3(steepness * pow(direction.x, 2) * WA_Sin, direction.x * WA_Cos, steepness * direction.x * direction.y * WA_Sin);

	tagent = float3(steepness * direction.x * direction.y * WA_Sin, direction.y * WA_Cos, steepness * pow(direction.y, 2) * WA_Sin);

	normal = float3(direction.x * WA_Cos, steepness*WA_Sin, direction.y * WA_Cos);

	return float3(direction.r * xyValue, amplitude * sinValue, direction.g * xyValue);
}


float WaveSpeed(float waveLength, float depth, float gravity) 
{
	//http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
	const float PI = 3.14159;
	return sqrt( ((gravity * waveLength) / 2 * PI) * tanh(2 * PI * (depth / waveLength)) );
}



//note: uniformly distributed, normalized rand, [0;1]
float nrand(float2 n)
{
	return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
}


#define M_PI 3.1415926

float2 gaussian_pair(float2 n, float time)
{
	float r1 = nrand(n);
	float r2 = nrand(n + n.x * n.y);
	float u = 2.0 * M_PI * r2;
	float v = sqrt(-2.0 * log(r2));

	return float2(v * cos(u), v * sin(u));
}


float PhilipSpectrum(float amplitude, float2 wavevector, float2 wind, float gravity) 
{
	float2 waveDir = normalize(wavevector);
	float2 windDir = normalize(wind);

	float dot_wave_wind = dot(waveDir, windDir);
	float waveLength = length(wavevector);
	float windLength = length(wind);

	float L = (windLength * windLength) / gravity;

	return (amplitude * exp(-1 / (waveLength * L)) * (dot_wave_wind * dot_wave_wind)) / (waveLength * waveLength * waveLength * waveLength);
}


float2 complex_mul(float2 c1, float2 c2) 
{
	return float2(c1.x * c2.x - c1.y * c2.y, c1.x * c2.y + c1.y * c2.x);
}