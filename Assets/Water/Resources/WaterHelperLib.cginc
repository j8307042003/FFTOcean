


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
	float r1 = clamp(abs(nrand(n)), 0.00001, .999999);
	float r2 = abs(nrand(n + n.x * n.y ));
	float u = 2.0 * M_PI * r2;
	float v = sqrt(-2.0 * log(r1));

    //return float2(r1, r2);
	return float2(v * cos(u), v * sin(u));
}


float PhilipSpectrum(float amplitude, float2 wavevector, float2 wind, float gravity) 
{
	float2 waveDir = normalize(wavevector);
	float2 windDir = normalize(wind);

	float dot_wave_wind = dot(waveDir, windDir);
	float waveLength = length(wavevector);
	float windLength = length(wind);

    //float L = ((windLength * windLength) * (windLength * windLength)) / gravity;
	float L = (windLength * windLength) / gravity;

	return (amplitude * exp(-1 / ( (waveLength * L) * (waveLength * L) ) ) * (dot_wave_wind * dot_wave_wind)) / (waveLength * waveLength * waveLength * waveLength);
}


float JONSWAPSpectrum(float amplitude, float2 wavevector, float2 wind, float gravity)
{	
	const float waveLength = length(wavevector);
	const float fetch = 500000;
	float U10 = length(wind);
	float alpha = 0.076 * pow(U10 * U10 / (fetch * gravity), 0.22);
	//float Omega = 2 * 3.1415926 / waveLength;
	float Omega = 1 / waveLength;
	float wp = 22 * pow(gravity * gravity / (U10 * fetch), 1.0 / 3.0);
	float r = 3.3;
	float t = Omega <= wp ? 0.07 : 0.09;

	float PSpectrum = alpha * (gravity * gravity) / (pow(Omega, 5)) * exp(-(5.0 / 4.0) * pow(wp / Omega, 4));
	float R = pow(3.3, 
					exp( -pow(Omega - wp, 2) / (2 * t * t * wp * wp))
				 );
	return amplitude * PSpectrum * r;
}


float2 complex_mul(float2 c1, float2 c2) 
{
	return float2(c1.x * c2.x - c1.y * c2.y, c1.x * c2.y + c1.y * c2.x);
}