using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class noise : MonoBehaviour {
    public int pixWidth;
    public int pixHeight;
    public float xOrg;
    public float yOrg;
    public float scale = 1.0F;
    public Cubemap cubemap;
    private Texture2D noiseTex;
    private Color[] pix;
    private Renderer rend;
    float timer = 0.0f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        noiseTex = new Texture2D(pixWidth, pixHeight);
        pix = new Color[noiseTex.width * noiseTex.height];
        rend.material.SetTexture( "_HeightTex", noiseTex);
        //Camera.main.GetComponent<Skybox>().material.mainTexture;
        //rend.material.SetTexture( "_Skybox", Camera.main.GetComponent<Skybox>().material.mainTexture);
        Camera.main.RenderToCubemap(cubemap);
        rend.material.SetTexture("_Skybox", cubemap);

        CalcNoise();

    }
    void CalcNoise()
    {
        float y = 0.0F;
        while (y < noiseTex.height)
        {
            float x = 0.0F;
            while (x < noiseTex.width)
            {
                float xCoord = xOrg + x / noiseTex.width * scale;
                float yCoord = yOrg + y / noiseTex.height * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                pix[ (int)(y * noiseTex.width + x)] = new Color(sample, sample, sample);
                x++;
            }
            y++;
        }
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
    }
    void Update()
    {

        timer += Time.deltaTime/3;
        rend.material.SetVector("_Offset", new Vector4( timer, timer,0,0));

    }
}
