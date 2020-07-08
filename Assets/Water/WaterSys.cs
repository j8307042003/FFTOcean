using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using System.Text;
public class WaterSysHelper
{
    public static ComputeShader LoadComputeShader(string shaderName)
    {
        ComputeShader s = Resources.Load<ComputeShader>(shaderName);
        Debug.Assert(s != null, "Load Water Compute Shader Error! Shader Not Found! Name : " + shaderName);
        return s;
    }

    public static RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format = RenderTextureFormat.ARGBFloat)
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
    public RenderTexture[] displacementDataArray;

    public WaterRenderData()
    {
        dataArray = new RenderTexture[LodNum];
        displacementDataArray = new RenderTexture[LodNum];
        for (int i = 0; i < LodNum; ++i)
        {
            dataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
            displacementDataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
        }
    }

    public RenderTexture[] GetRenderTexArray() { return dataArray; }
    
}


public class WaterSysType
{
    public static string WaveComputeShaderName { get { return "WaveCompute"; } }
    public static string WaveComputeKernel { get { return "Tick"; } }
    public static string ButterFlyComputerShaderName { get { return "ButterFlyTexture"; } }
    public static string ButterFlyComuteKernel { get { return "Tick"; } }
    public static string WaveFFTComputerShaderName {  get { return "WaveFFT"; } }
    public static string WaveFFTKernel { get { return "FFT"; } }
    public static string WaveFFT_HorizontalKernel { get { return "FFT_horizontal"; } }
    public static string WaveFFT2DKernel { get { return "FFT2D"; } }
    public static string WaveFFT2D_HorizontalKernel { get { return "FFT2D_horizontal"; } }

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


    RenderTexture butterFlyTex;
    ComputeShader butterFlyCS;
    int butterflyKernel;

    ComputeShader FFTCompute;
    int FFTKernel;
    int FFTHorizontalKernel;
    int FFT2DKernel;
    int FFT2DHorizontalKernel;

    RenderTexture pingpongTex;

    [Range(0, 9)]
    public int it = 0;

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
        butterFlyTex = WaterSysHelper.CreateRenderTexture((int)Mathf.Log(renderData.dataArray[0].width, 2), renderData.dataArray[0].width, RenderTextureFormat.ARGBInt);
        waveCompute = WaterSysHelper.LoadComputeShader(WaterSysType.WaveComputeShaderName);
        waveKernel = waveCompute.FindKernel(WaterSysType.WaveComputeKernel);

        butterFlyCS = WaterSysHelper.LoadComputeShader(WaterSysType.ButterFlyComputerShaderName);
        butterflyKernel = butterFlyCS.FindKernel(WaterSysType.ButterFlyComuteKernel);


        FFTCompute = WaterSysHelper.LoadComputeShader(WaterSysType.WaveFFTComputerShaderName);
        FFTKernel = FFTCompute.FindKernel(WaterSysType.WaveFFTKernel);
        FFTHorizontalKernel = FFTCompute.FindKernel(WaterSysType.WaveFFT_HorizontalKernel);
        FFT2DKernel = FFTCompute.FindKernel(WaterSysType.WaveFFT2DKernel);
        FFT2DHorizontalKernel = FFTCompute.FindKernel(WaterSysType.WaveFFT2D_HorizontalKernel);

        pingpongTex = WaterSysHelper.CreateRenderTexture(512, 512);
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

    Texture2D tmp;

    int reverse(int i, int log_2_N)
    {
        int res = 0;
        for (int j = 0; j < log_2_N; j++)
        {
            res = (res << 1) + (i & 1);
            i >>= 1;
        }
        return res;
    }
    float t = 0;



    void Tick()
    {
        if (!tmp)
        {
            tmp = new Texture2D(9, 512, TextureFormat.RGBAHalf, false);
            tmp.Apply();
        }

        _commandBuffer = new CommandBuffer();

        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveTexture"), renderData.GetRenderTexArray()[0]);
        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveDisplacement"), renderData.displacementDataArray[0]);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("time"), Time.time);

        _commandBuffer.SetComputeVectorParam(waveCompute, Shader.PropertyToID("direction"), direction);
        _commandBuffer.SetComputeVectorParam(waveCompute, Shader.PropertyToID("wind"), wind);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("steepness"), steepness);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("amplitude"), amplitude);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("waveLength"), waveLength);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("speed"), speed);



        _commandBuffer.DispatchCompute(waveCompute, waveKernel, WaterRenderData.TextureSize / 8, WaterRenderData.TextureSize / 8, 1);

        _commandBuffer.SetComputeTextureParam(butterFlyCS, butterflyKernel, Shader.PropertyToID("Result"), butterFlyTex);
        _commandBuffer.DispatchCompute(butterFlyCS, butterflyKernel, butterFlyTex.width, butterFlyTex.height / 8, 1);

        t += Time.deltaTime;
        if (t > 1) {
            t = 0;
            //it++;
            //it %= 10;
        }

        //Debug.LogError(it);
        //for (int i = 0; i < butterFlyTex.width; i++)
        FFT(renderData.GetRenderTexArray()[0]);
        FFT2D(renderData.displacementDataArray[0]);

        UnityEngine.Profiling.Profiler.BeginSample("My Command Buffer");
        Graphics.ExecuteCommandBuffer(_commandBuffer);
        UnityEngine.Profiling.Profiler.EndSample();



        /*
        //RenderTexture.active = butterFlyTex;
        //tmp.ReadPixels(new Rect(0, 0, 9, 512), 0, 0);
        
        RenderTexture.active = renderData.GetRenderTexArray()[0];
        tmp.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        RenderTexture.active = null;

        Color c = tmp.GetPixel(0, 0);
        Color[] c0 = new Color[512];
        int[] reverseIdx = new int[512];

        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < 512; i++)
        {
            c0[i] = tmp.GetPixel(1, i);
            reverseIdx[i] = reverse(i, 9);
            if (float.IsNaN(c0[i].r))
                Debug.LogError("Nan at y " + i);
            sb.AppendLine(c0[i].r + "  " + c0[i].g);
        }

        Debug.Log(sb.ToString());
        */
        waterSurface.SetTexture("_Height", renderData.GetRenderTexArray()[0]);
        waterSurface.SetTexture("_Displacement", renderData.displacementDataArray[0]);

    }


    void FFT(RenderTexture renderTex)
    {
        for (int i = Mathf.Min(0, it - 1); i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFTKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderTex);
        }

        for (int i = Mathf.Min(0, it - 1); i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTHorizontalKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTHorizontalKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTHorizontalKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFTHorizontalKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderTex);
        }
    }

    void FFT2D(RenderTexture renderTex)
    {
        for (int i = Mathf.Min(0, it - 1); i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFT2DKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderData.displacementDataArray[0]);
        }

        for (int i = Mathf.Min(0, it - 1); i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DHorizontalKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DHorizontalKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DHorizontalKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFT2DHorizontalKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderTex);
        }
    }

    /*
    private void OnGUI()
    {
        if (!bShowDebug) return;
        GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.GetRenderTexArray()[0]);
        //GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.displacementDataArray[0], ScaleMode.StretchToFill, false);
        //GUI.DrawTexture(new Rect(10, 10, 500, 500), butterFlyTex, ScaleMode.StretchToFill, false);
    }
    */

}
