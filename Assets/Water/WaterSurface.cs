using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSurface : MonoBehaviour {

    Texture2D tmp;
    public float s = 1.0f;
    // Use this for initialization
    void Start () {
        tmp = new Texture2D(512, 512, TextureFormat.RGBAFloat, false);
        tmp.filterMode = FilterMode.Point;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer)
        {
            renderer.material = WaterSys.waterSurface;
        }

	}
	
	// Update is called once per frame
	void Update () {
		
	}


    Vector3 GetNormal(Texture2D tex, Vector2 uv)
    {
        float nScale = 1.0f;
        float uPixel = 1.0f / 512.0f;
        float vPixel = 1.0f / 512.0f;
        Vector2Int uv_i = new Vector2Int((int)(uv.x * 512), (int)(uv.y * 512));
        float height_pu = tex.GetPixel(uv_i.x + 1, uv_i.y).b;
        float height_mu = tex.GetPixel(uv_i.x - 1, uv_i.y).b;
        float height_pv = tex.GetPixel(uv_i.x, uv_i.y + 1).b;
        float height_mv = tex.GetPixel(uv_i.x, uv_i.y - 1).b;
        float du = height_mu - height_pu;
        float dv = height_mv - height_pv;
        Vector3 N = (new Vector3(du, dv, 1.0f / nScale)).normalized;
        return new Vector3(N.x, N.z, N.y);
    }

    private void OnDrawGizmos()
    {
        /*
        if (WaterSys.instance == null) return;
        var rt = RenderTexture.active;
        RenderTexture.active = WaterSys.instance.renderData.displacementMapDataArray[0];
        tmp.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        RenderTexture.active = rt;
        Gizmos.DrawRay(new Ray(transform.position, new Vector3(0, 1, 0)));


        MeshFilter meshfilter = GetComponent<MeshFilter>();
        Mesh m = meshfilter.mesh;

        return;
        var origin = transform.position;
        for (int i = 0; i < m.vertices.Length; ++i)
        {
            Vector2 uv = m.uv[i] / 5.0f;
            var d = tmp.GetPixel((int)(uv.x * 512.0), (int)(uv.y * 512.0));
            Vector3 location = origin + m.vertices[i] + new Vector3(0, (float)(d.b * s), 0);
            //Vector3 location = origin + m.vertices[i] + new Vector3(0, (float)(d.b * 1e-08), 0);

            Vector3 n = GetNormal(tmp, uv);
            Gizmos.color = Color.green;
            //Gizmos.DrawRay(new Ray(location, new Vector3(0, 1,0)));
            Gizmos.DrawRay(new Ray(location, n));
        }
        */

    }
}
