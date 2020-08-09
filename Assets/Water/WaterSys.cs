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

    public static Shader LoadShader(string shaderName)
    {
        Shader s = Resources.Load<Shader>(shaderName);
        Debug.Assert(s != null, "Load Water Shader Error! Shader Not Found! Name : " + shaderName);
        return s;
    }

    public static RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format = RenderTextureFormat.ARGBFloat, FilterMode filterMode = FilterMode.Point)
    {
        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        rt.hideFlags = HideFlags.DontSave;
        rt.filterMode = filterMode;
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
    public float unitLength = 1.0f; //meters

    public RenderTexture[] dataArray;
    public RenderTexture[] displacementDataArray;
    public RenderTexture[] normalDataArray;
    public RenderTexture[] displacementMapDataArray;
    public RenderTexture[] foamJacobianDataArray;
    public RenderTexture[] waveFoamJxy;
    public RenderTexture[] waveFoam;
    public RenderTexture[] waveFoamUpdate;

    public WaterRenderData()
    {
        dataArray = new RenderTexture[LodNum];
        displacementDataArray = new RenderTexture[LodNum];
        normalDataArray = new RenderTexture[LodNum];
        displacementMapDataArray = new RenderTexture[LodNum];
        foamJacobianDataArray = new RenderTexture[LodNum];
        waveFoamJxy = new RenderTexture[LodNum];
        waveFoam = new RenderTexture[LodNum];
        waveFoamUpdate = new RenderTexture[LodNum];
        for (int i = 0; i < LodNum; ++i)
        {
            dataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
            displacementDataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
            normalDataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize, filterMode: FilterMode.Bilinear);
            displacementMapDataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
            foamJacobianDataArray[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
            waveFoamJxy[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize);
            waveFoam[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize, filterMode:FilterMode.Bilinear);
            waveFoamUpdate[i] = WaterSysHelper.CreateRenderTexture(TextureSize, TextureSize, filterMode:FilterMode.Trilinear);
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
    public static string WaveTextureComputerShaderName { get { return "WaveTexture"; } }
    public static string WaveTextureKernel { get { return "WaveTexture"; } }
    public static string WaveTextureNormalKernel { get { return "WaveNormalKernel"; } }
    public static string WaveFFT_HorizontalKernel { get { return "FFT_horizontal"; } }
    public static string WaveFFT2DKernel { get { return "FFT2D"; } }
    public static string WaveFFT2D_HorizontalKernel { get { return "FFT2D_horizontal"; } }

    public static string WaveFoamUpdateShaderName { get { return "FoamUpdate"; } }


}


public class WaterSys : MonoBehaviour {

    public static WaterSys instance;
    public static Material waterSurface;

    public Material mat;

    public bool bShowDisplacement = true;
    public bool bShowNormal = false;

    [Range(0, 10)]
    public float timeScale = 1.0f;
    [Range(0, 1)]
    public float foamScale = 1.0f;

    [Range(0, 10)]
    public float foamExistTime = 1.0f;


    public RenderTexture displacement;
    public WaterRenderData renderData;
    public float unit_length = 1000.0f;
    ComputeShader waveCompute;
    int waveKernel;
    ComputeShader waveTexture;
    int waveTextureKernel;
    int waveNormalKernel;
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

    Shader FoamUpdateShader;
    Material foamBlitMat;

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
        renderData.unitLength = 1000.0f;
        butterFlyTex = WaterSysHelper.CreateRenderTexture((int)Mathf.Log(renderData.dataArray[0].width, 2), renderData.dataArray[0].width, RenderTextureFormat.ARGBInt);
        waveCompute = WaterSysHelper.LoadComputeShader(WaterSysType.WaveComputeShaderName);
        waveKernel = waveCompute.FindKernel(WaterSysType.WaveComputeKernel);
        waveTexture = WaterSysHelper.LoadComputeShader(WaterSysType.WaveTextureComputerShaderName);
        waveTextureKernel = waveTexture.FindKernel(WaterSysType.WaveTextureKernel);
        waveNormalKernel = waveTexture.FindKernel(WaterSysType.WaveTextureNormalKernel);
        

        butterFlyCS = WaterSysHelper.LoadComputeShader(WaterSysType.ButterFlyComputerShaderName);
        butterflyKernel = butterFlyCS.FindKernel(WaterSysType.ButterFlyComuteKernel);


        FFTCompute = WaterSysHelper.LoadComputeShader(WaterSysType.WaveFFTComputerShaderName);
        FFTKernel = FFTCompute.FindKernel(WaterSysType.WaveFFTKernel);
        FFTHorizontalKernel = FFTCompute.FindKernel(WaterSysType.WaveFFT_HorizontalKernel);
        FFT2DKernel = FFTCompute.FindKernel(WaterSysType.WaveFFT2DKernel);
        FFT2DHorizontalKernel = FFTCompute.FindKernel(WaterSysType.WaveFFT2D_HorizontalKernel);

        pingpongTex = WaterSysHelper.CreateRenderTexture(512, 512);

        FoamUpdateShader = WaterSysHelper.LoadShader(WaterSysType.WaveFoamUpdateShaderName);
        foamBlitMat = new Material(FoamUpdateShader);

        displacement = renderData.displacementMapDataArray[0];
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
    



    void Tick()
    {
        if (!tmp)
        {
            tmp = new Texture2D(9, 512, TextureFormat.RGBAHalf, false);
            tmp.Apply();
        }

        renderData.unitLength = unit_length;

        _commandBuffer = new CommandBuffer();

        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveTexture"), renderData.GetRenderTexArray()[0]);
        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveDisplacement"), renderData.displacementDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveNormal"), renderData.normalDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveFoamJocobian"), renderData.foamJacobianDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveCompute, waveKernel, Shader.PropertyToID("WaveFoamJxy"), renderData.waveFoamJxy[0]);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("time"), Time.time * timeScale);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("unitLen"), renderData.unitLength);

        _commandBuffer.SetComputeVectorParam(waveCompute, Shader.PropertyToID("direction"), direction);
        _commandBuffer.SetComputeVectorParam(waveCompute, Shader.PropertyToID("wind"), wind);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("steepness"), steepness);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("amplitude"), amplitude);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("waveLength"), waveLength);
        _commandBuffer.SetComputeFloatParam(waveCompute, Shader.PropertyToID("speed"), speed);



        _commandBuffer.DispatchCompute(waveCompute, waveKernel, WaterRenderData.TextureSize / 8, WaterRenderData.TextureSize / 8, 1);

        _commandBuffer.SetComputeTextureParam(butterFlyCS, butterflyKernel, Shader.PropertyToID("Result"), butterFlyTex);
        _commandBuffer.DispatchCompute(butterFlyCS, butterflyKernel, butterFlyTex.width, butterFlyTex.height / 8, 1);

        
        FFT(renderData.GetRenderTexArray()[0]);
        FFT2D(renderData.displacementDataArray[0]);
        FFT2D(renderData.normalDataArray[0]);
        FFT2D(renderData.foamJacobianDataArray[0]);
        FFT(renderData.waveFoamJxy[0]);
        
        _commandBuffer.SetComputeTextureParam(waveTexture, waveTextureKernel, Shader.PropertyToID("WaveHeightField"), renderData.GetRenderTexArray()[0]);
        _commandBuffer.SetComputeTextureParam(waveTexture, waveTextureKernel, Shader.PropertyToID("WaveHorizontal"), renderData.displacementDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveTexture, waveTextureKernel, Shader.PropertyToID("WaveDisplacement"), renderData.displacementMapDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveTexture, waveTextureKernel, Shader.PropertyToID("WaveFoamJocobian"), renderData.foamJacobianDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveTexture, waveTextureKernel, Shader.PropertyToID("WaveFoamJxy"), renderData.waveFoamJxy[0]);
        _commandBuffer.SetComputeTextureParam(waveTexture, waveTextureKernel, Shader.PropertyToID("WaveFoam"), renderData.waveFoam[0]);
        _commandBuffer.DispatchCompute(waveTexture, waveTextureKernel, renderData.displacementMapDataArray[0].width / 16, renderData.displacementMapDataArray[0].height / 16, 1);

        
        _commandBuffer.SetComputeTextureParam(waveTexture, waveNormalKernel, Shader.PropertyToID("WaveDisplacement"), renderData.displacementMapDataArray[0]);
        _commandBuffer.SetComputeTextureParam(waveTexture, waveNormalKernel, Shader.PropertyToID("WaveNormal"), renderData.normalDataArray[0]);
        _commandBuffer.SetComputeFloatParam(waveTexture, Shader.PropertyToID("unitLen"), renderData.unitLength);
        _commandBuffer.SetComputeVectorParam(waveTexture, Shader.PropertyToID("targetPos"), new Vector4(Mathf.Sin(Time.time), Mathf.Cos(Time.time), 0, 0) * (100) + new Vector4(256, 256));
        _commandBuffer.SetComputeFloatParam(waveTexture, Shader.PropertyToID("amplitude"), amplitude);
        _commandBuffer.DispatchCompute(waveTexture, waveNormalKernel, renderData.displacementMapDataArray[0].width / 16, renderData.displacementMapDataArray[0].height / 16, 1);

        foamBlitMat.SetFloat(Shader.PropertyToID("foamExistTime"), foamExistTime);
        foamBlitMat.SetFloat(Shader.PropertyToID("deltaTime"), Time.deltaTime);
        foamBlitMat.SetFloat(Shader.PropertyToID("foamScale"), foamScale);
        foamBlitMat.SetTexture(Shader.PropertyToID("_FoamData"), renderData.waveFoam[0]);
        foamBlitMat.SetTexture(Shader.PropertyToID("_Foam"), renderData.waveFoamUpdate[0]);
        _commandBuffer.Blit(renderData.waveFoam[0], pingpongTex, foamBlitMat);
        _commandBuffer.Blit(pingpongTex, renderData.waveFoamUpdate[0]);

        
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
        //waterSurface.SetTexture("_Displacement", renderData.displacementDataArray[0]);
        waterSurface.SetTexture("_Displacement", renderData.displacementMapDataArray[0]);
        waterSurface.SetTexture("_Normal", renderData.normalDataArray[0]);
        waterSurface.SetTexture("_Foam", renderData.waveFoamUpdate[0]);
        //waterSurface.SetTexture("_Foam", renderData.waveFoam[0]);
        waterSurface.SetFloat("unitLen", renderData.unitLength);
        waterSurface.SetFloat("foamScale", foamScale);

    }


    void FFT(RenderTexture renderTex)
    {
        for (int i = 0; i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFTKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFTKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderTex);
        }

        for (int i = 0; i < it; i++)
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
        for (int i = 0; i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFT2DKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderTex);
        }

        for (int i = 0; i < it; i++)
        {
            _commandBuffer.SetComputeIntParam(FFTCompute, Shader.PropertyToID("stage"), i);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DHorizontalKernel, Shader.PropertyToID("Result"), pingpongTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DHorizontalKernel, Shader.PropertyToID("pingpong"), renderTex);
            _commandBuffer.SetComputeTextureParam(FFTCompute, FFT2DHorizontalKernel, Shader.PropertyToID("ButterflyTex"), butterFlyTex);
            _commandBuffer.DispatchCompute(FFTCompute, FFT2DHorizontalKernel, renderTex.width / 16, renderTex.height / 16, 1);
            _commandBuffer.Blit(pingpongTex, renderTex);
        }
    }

    
    private void OnGUI()
    {
        //if (bShowDisplacement) GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.displacementMapDataArray[0], ScaleMode.StretchToFill, false);
        if (bShowDisplacement) GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.GetRenderTexArray()[0], ScaleMode.StretchToFill, false);
        //GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.GetRenderTexArray()[0]);
        //GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.displacementDataArray[0], ScaleMode.StretchToFill, false);
        //if(bShowNormal)GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.normalDataArray[0], ScaleMode.StretchToFill, false);
        if(bShowNormal)GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.waveFoamUpdate[0], ScaleMode.StretchToFill, false);
        //GUI.DrawTexture(new Rect(10, 10, 500, 500), renderData.displacementMapDataArray[0], ScaleMode.StretchToFill, false);

        //GUI.DrawTexture(new Rect(10, 10, 500, 500), butterFlyTex, ScaleMode.StretchToFill, false);
    }
    




}
