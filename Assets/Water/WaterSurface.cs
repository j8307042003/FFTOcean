using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSurface : MonoBehaviour {

    

    // Use this for initialization
    void Start () {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer)
        {
            renderer.material = WaterSys.waterSurface;
        }

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
