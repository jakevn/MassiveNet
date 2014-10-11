// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using UnityEngine;

public class CatAppearance : MonoBehaviour {

    private readonly List<MeshRenderer> furRenderer = new List<MeshRenderer>();
    private Material currentFurMat;

	void Awake () {
	    var renderers = GetComponentsInChildren<MeshRenderer>();
	    foreach (var r in renderers) {
	        if (!r.material.name.Contains("Fur")) continue;
            furRenderer.Add(r);
	        if (currentFurMat == null) currentFurMat = r.material;
	    }
	}

    public void ChangeFur(Material newFur) {
        currentFurMat = newFur;
        foreach (var r in furRenderer) r.material = newFur;
    }
}
