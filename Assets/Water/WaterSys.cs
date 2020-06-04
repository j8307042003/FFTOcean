using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class WaterSysHelper
{
    public static ComputeShader LoadComputeShader(string shaderName)
    {
        ComputeShader s = Resources.Load<ComputeShader>(shaderName);
        Debug.Assert(s != null, "Load Water Compute Shader Error! Shader Not Found! Name : " + shaderName);
        return s;
    }

    public static RenderTexture CreateRenderTexture(int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        rt.hideFlags = HideFlags.DontSave;
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.useMipMap = false;
        rt.Create();


        return rt;
    }
}


public class WaterRenderData
{
    const int LodNum = 8;
    public const int TextureSize = 512;

    public RenderTexture[] dataArray;

    public WaterRenderData()
    {
        dataArray = new RenderTexture[LodNum];
        for (int i = 0; i < LodNum; ++i)
        {
            dataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
        }
    }

    public RenderTexture[] GetRenderTexArray() { return dataArray; }
}


public class WaterSysType
{
    public static string WaveComputeShaderName { get { return "WaveCompute"; } }
    public static string WaveComputeKernel { get { return "Tick"; } }
}

public class WaterSys : MonoBehaviour {

    public static WaterSys instance;
    public static Material waterSurface;

    public Material mat;

    public bool bShowDebug = true;


    WaterRenderData renderData;
    ComputeShader waveCompute;
    int waveKernel;
    CommandBuffer _commandBuffer;


    private void Awake()
    {
        Initialize();
        instance = this;
        waterSurface = mat;
    }

    private void OnDestroy()
    {
        
    }

    void Initialize()
    {

        renderData = new WaterRenderData();
        waveCompute = WaterSysHelper.LoadComputeShader(WaterSysType.WaveComputeShaderName);
        waveKernel = waveCompute.FindKernel(WaterSysType.WaveComputeKernel);
    }


    private void Update()
    {
        Tick();
    }


    public Vector2 direction;
    public Vector2 wind;
    public float steepness;
    public float amplitude;
    public float waveLength;
    public float speed;

    void Tick()
    {
        _commandBuffer = new CommandBuffer();

        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveTexture"), renderData.GetRenderTexArray()[0]);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("time"), Time.time);

        _commandBuffer.SetComputeVectorParam(waveCompute, Shader.PropertyToID("direction"), direction);
        _commandBuffer.SetComputeVectorParam(waveCompute, Shader.PropertyToID("wind"), wind);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("steepness"), steepness);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("amplitude"), amplitude);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("waveLength"), waveLength);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("speed"), speed);



        _commandBuffer.DispatchCompute(waveCompute, waveKernel, WaterRenderData.TextureSize / 8, WaterRenderData.TextureSize / 8, 1);

        UnityEngine.Profiling.Profiler.BeginSample("My Command Buffer");
        Graphics.ExecuteCommandBuffer(_commandBuffer);
        UnityEngine.Profiling.Profiler.EndSample();

        waterSurface.SetTexture("_Displacement", renderData.GetRenderTexArray()[0]);

    }

    private void OnGUI()
    {
        if (!bShowDebug) return;
        GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.GetRenderTexArray()[0]);
    }

}
