using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WaveParticle : MonoBehaviour
{
    struct WaveParticleData
    {
        public bool haveNeighbor;
        public ParticleEmitter<WaveParticleData>.CustomEmitInfo neighbor;
        public float amplitude;
    }

    ParticleEmitter<WaveParticleData> Emitter;

    RenderTexture particleTexture;
    ComputeShader particleShader;
    int Kernel;

    public Material mat;
    public float speed = 10.0f;

    public float range = 1000;
    public Vector3 camPosition = Vector3.zero;
    public Vector3 camRot = Vector3.zero;
    RenderTexture tmpRT;

    Material gaussianFilter;

    // Start is called before the first frame update
    void Start()
    {
        ParticleEmitter<WaveParticleData>.ParticleEmitterParam param = new ParticleEmitter<WaveParticleData>.ParticleEmitterParam();
        param.maxParticle = 100000;
        param.ratio = 0f;
        param.lifeSpan = 20.0f;
        param.CustomEmitter = MyEmitFunc;
        param.CustomUpdater = MyUpdateFunc;
        Emitter = new ParticleEmitter<WaveParticleData>(param);


        tmpRT = WaterSysHelper.CreateRenderTexture(512, 512, RenderTextureFormat.ARGBFloat, FilterMode.Point, 1);
        particleTexture = WaterSysHelper.CreateRenderTexture(512, 512, RenderTextureFormat.ARGBFloat, FilterMode.Point, 1);
        particleShader = WaterSysHelper.LoadComputeShader("WaveParticleCS");
        Kernel = particleShader.FindKernel("CSMain");

        Shader guassianShader = WaterSysHelper.LoadShader("GaussianFilter");
        gaussianFilter = new Material(guassianShader);
    }

    // Update is called once per frame
    void Update()
    {
        Simulation();
        Render();
    }

    void Simulation()
    {
        Emitter.Update(Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {

            ParticleEmitter<WaveParticleData>.CustomEmitInfo info = new ParticleEmitter<WaveParticleData>.CustomEmitInfo();
            WaveParticleData waveData = new WaveParticleData();
            Vector3 center = transform.position;
            info.lifeSpan = 0;
            info.position = center;
            float interval = ((float)9) / 10 - 1;
            float theta = 2 * Mathf.PI * interval;

            info.velocity = new Vector3(Mathf.Sin(theta), 0, Mathf.Cos(theta)) * speed;
            for (int i = 0; i < 10; i++)
            {
                interval = ((float)i) / 10 - 1;
                theta = 2 * Mathf.PI * interval;
                waveData.haveNeighbor = true;
                waveData.neighbor = info;
                info.position = center;
                info.velocity = new Vector3(Mathf.Sin(theta), 0, Mathf.Cos(theta)) * speed;
                Emitter.Emit(info, waveData);
            }
        }
    }

    private void Render()
    {
        CommandBuffer _commandBuffer = new CommandBuffer();

        _commandBuffer.SetComputeTextureParam(particleShader, Kernel, "Result", particleTexture);
        _commandBuffer.DispatchCompute(particleShader, Kernel, particleTexture.width / 8, particleTexture.height / 8, 1);

        var particles = Emitter.GetParticles();
        Matrix4x4 matrix = new Matrix4x4();

        _commandBuffer.SetRenderTarget(tmpRT);
        _commandBuffer.ClearRenderTarget(true, true, Color.black);
        _commandBuffer.SetProjectionMatrix(Matrix4x4.Ortho(-range, range, -range, range, 0, 10000));
        _commandBuffer.SetViewMatrix(Matrix4x4.TRS(camPosition, Quaternion.Euler(camRot), Vector3.one));
        for (int i = 0; i < particles.count; ++i)
        {
            matrix = Matrix4x4.TRS(particles.particles[i].position, Quaternion.identity, Vector3.one);
            _commandBuffer.DrawProcedural(matrix, mat, 0, MeshTopology.Points, 4);

        }


        //_commandBuffer.Blit(particleTexture,)
        gaussianFilter.SetVector("iResolution", new Vector4(512, 512, 0, 0));
        _commandBuffer.Blit(tmpRT, particleTexture, gaussianFilter);
        Graphics.ExecuteCommandBuffer(_commandBuffer);

        _commandBuffer.Dispose();
    }

    ParticleEmitter<WaveParticleData>.CustomEmitInfo MyEmitFunc(ParticleEmitter<WaveParticleData>.ParticleEmitterParam param)
    {
        ParticleEmitter<WaveParticleData>.CustomEmitInfo info = new ParticleEmitter<WaveParticleData>.CustomEmitInfo();
        info.position = transform.position;
        info.velocity = new Vector3(0, 0, 1);
        return info;
    }

    WaveParticleData MyUpdateFunc(ParticleEmitter<WaveParticleData>.Particle particle, WaveParticleData waveData)
    {
        WaveParticleData data = waveData;
        if (!data.haveNeighbor) return data;

        data.neighbor.position += data.neighbor.velocity * Time.deltaTime;
        if ((data.neighbor.position - particle.position).magnitude > 3.0f )
        {
            ParticleEmitter<WaveParticleData>.CustomEmitInfo info = new ParticleEmitter<WaveParticleData>.CustomEmitInfo();
            WaveParticleData newWaveData = new WaveParticleData();
            newWaveData.neighbor = data.neighbor;
            newWaveData.haveNeighbor = true;
            float speed = (particle.velocity.magnitude + data.neighbor.velocity.magnitude) / 2;
            info.position = (particle.position + data.neighbor.position) / 2;
            info.velocity = ((particle.velocity + data.neighbor.velocity) / 2).normalized * speed;
            info.lifeSpan = particle.life;
            data.neighbor = info;
            Emitter.Emit(info, newWaveData);
        }

        return data;
    }

    public RenderTexture GetResultTexture()
    {
        return particleTexture;
    }

    private void OnDrawGizmos()
    {
        if (Emitter == null) return;
        var c = Gizmos.color;
        Gizmos.color = Color.green;
        var data = Emitter.GetParticles();
        for (int i = 0; i < data.count; ++i)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(data.particles[i].position, 1.0f);
        }
        Gizmos.color = c;
    }

    private void OnGUI()
    {
        //GUI.DrawTexture(new Rect(10, 10, 500, 500), particleTexture, ScaleMode.StretchToFill, false);
    }

}
