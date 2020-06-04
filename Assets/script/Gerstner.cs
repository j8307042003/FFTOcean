using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class Gerstner : MonoBehaviour {

    [System.Serializable]
    public class WaveParameter 
    {
        [SerializeField]
        public Vector2 direction;
        public int Kind;
        public Vector2 position;
        public float amplitude;
        public float steepness;
        public float waveLength;
        public float speed;
    }

    public int SeaMeshSize = 60;

    int waveCount = 0;
    Vector4[] directionArray;
    float[] amplitudeArray;
    float[] steepnessArray;
    float[] waveLengthArray;
    float[] speedArray;
    float[] waveKindArray;
    //Vector2[] positionArray;

    public List<WaveParameter> waves = new List<WaveParameter>();

    MeshFilter SeaMesh;
    Material mat;
    public Cubemap skyBox;

    int waveCountHash;
    int directArrayHash;
    int amplitudeArrayHash;
    int steepnessArrayHash;
    int waveLengthArrayHash;
    int speedArrayHash;
    int waveKindArrayHash;

    bool useWave = false;

    public void SetWaveParameter()
    {
        waveCount = waves.Count;

        const float twoPi = Mathf.PI * 2;

        if (waveCount == 0) return;
        directionArray = new Vector4[waveCount];
        amplitudeArray = new float[waveCount];
        steepnessArray = new float[waveCount];
        waveLengthArray = new float[waveCount];
        speedArray = new float[waveCount];
        waveKindArray = new float[waveCount];
        //positionArray = new Vector2[waveCount];


        for ( int i = 0; i < waveCount; ++i)
        {
            waves[i].direction.Normalize();
            directionArray[i] = new Vector4(waves[i].direction.x, waves[i].direction.y, waves[i].position.x, waves[i].position.y);
            waveKindArray[i] = waves[i].Kind;
            amplitudeArray[i] = waves[i].amplitude;
            steepnessArray[i] = waves[i].steepness;
            waveLengthArray[i] = twoPi / waves[i].waveLength;
            speedArray[i] = waves[i].speed * waveLengthArray[i];
        }
    }

    public void SetMesh()
    {
        if (SeaMesh == null ) SeaMesh = GetComponent<MeshFilter>();

        List<Vector3> verticeList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        List<int> triList = new List<int>();
        Mesh mesh = new Mesh();

        int width = SeaMeshSize;
        int height = SeaMeshSize;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                verticeList.Add(new Vector3(x - width / 2, 0f, y - height / 2));
                uvList.Add(new Vector2(x / (float)(width), y / (float)(height)));
                //Skip if a new square on the plane hasn't been formed
                if (x == 0 || y == 0)
                    continue;
                //Adds the index of the three vertices in order to make up each of the two tris
                triList.Add(width * x + y); //Top right
                triList.Add(width * x + y - 1); //Bottom right
                triList.Add(width * (x - 1) + y - 1); //Bottom left - First triangle
                triList.Add(width * (x - 1) + y - 1); //Bottom left 
                triList.Add(width * (x - 1) + y); //Top left
                triList.Add(width * x + y); //Top right - Second triangle
            }
        }

        mesh.vertices = verticeList.ToArray();
        mesh.uv = uvList.ToArray();
        mesh.triangles = triList.ToArray();
        mesh.RecalculateNormals();
        SeaMesh.sharedMesh = mesh;
    }


    // Use this for initialization
    void Start () {
        if (skyBox != null)
        {
            Shader.SetGlobalTexture("_SkyboxTex1", skyBox);
        }

        mat = GetComponent<MeshRenderer>().material;
        SeaMesh = GetComponent<MeshFilter>();
        waveCountHash = Shader.PropertyToID("WaveCount");
        directArrayHash = Shader.PropertyToID("_DirectionArray");
        amplitudeArrayHash = Shader.PropertyToID("_AmplitudeArray");
        steepnessArrayHash = Shader.PropertyToID("_SteepnessArray");
        waveLengthArrayHash = Shader.PropertyToID("_WaveLengthArray");
        speedArrayHash = Shader.PropertyToID("_SpeedArray");
        waveKindArrayHash = Shader.PropertyToID("_WaveKindArray");
        SetMesh();
        SetWaveParameter();
    }
	
	// Update is called once per frame
	void Update () {
        mat.SetFloat(waveCountHash, waveCount);
        mat.SetVectorArray(directArrayHash, directionArray);
        mat.SetFloatArray(steepnessArrayHash, steepnessArray);
        mat.SetFloatArray(amplitudeArrayHash, amplitudeArray);
        mat.SetFloatArray(waveLengthArrayHash, waveLengthArray);
        mat.SetFloatArray(speedArrayHash, speedArray);
        mat.SetFloatArray(waveKindArrayHash, waveKindArray);

        if (Input.GetKeyDown(KeyCode.B))
        {
            useWave = !useWave;
            mat.SetFloat("_UseWave", useWave? 0.1f : 0.0f);
        }

    }
}
