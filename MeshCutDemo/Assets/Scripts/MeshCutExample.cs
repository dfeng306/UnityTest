using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutExample : MonoBehaviour {

	// Update is called once per frame
	bool isWorking=false;
	void Update () {
		if (Input.GetKeyDown (KeyCode.F)) {
			MeshCut.working=isWorking=!isWorking;
		}
	}
}
