#define DEBUG
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCut : MonoBehaviour {


	static Mesh targetMesh=null;
	static Vector3 hitPos;
	static Vector3 cutVertivalDir;
	static Vector3 camPos;
	static Vector3 dir;
	static Vector3 planeNormal;
	static Transform hitTarget;

	public static bool working=false;

	static List<Vector3> tempVert1=new List<Vector3>();
	static List<Vector3> tempNormal1=new List<Vector3>();
	static List<int> triangles1=new List<int>();

	static List<Vector3> tempVert2=new List<Vector3>();
	static List<Vector3> tempNormal2=new List<Vector3>();
    static List<int> triangles2=new List<int>();
	static int[] triangles;

	static Dictionary<int,int> pointIndex1 = new Dictionary<int, int> ();
	static Dictionary<int,int> pointIndex2 = new Dictionary<int, int> ();

	static List<Vector3> localPos=new List<Vector3>();
	static List<Vector3> allPoint = new List<Vector3> ();
	static Vector3 fpos;
	static bool fbool = false;

	float scrollSpeed=25.0f;
	Ray _ray;
	static RaycastHit _hit;
	static bool colliding=false;
	GameObject temp=null;
	//使用GL画线所需材质
	public Material mat;
	float angle=0;
	void Start(){
	}

	void Update(){
		if (working) {
			_ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			if (Physics.Raycast (_ray, out _hit)) {
				colliding = true;
				camPos = Camera.main.transform.position;

				//切割平面由dir,cutVerticleDir两个向量唯一确定，planeNormal由dir，cutVerticleDir叉乘所得
				dir = (_hit.point - camPos).normalized;

				float ang = Vector3.Angle (dir, Vector3.up);
				if (ang == 90.0f)
					cutVertivalDir = Vector3.up;
				else
					cutVertivalDir = (Vector3.Dot (Vector3.up, dir) * (-dir) + Vector3.up).normalized;
				planeNormal = Vector3.Cross (dir, cutVertivalDir).normalized;

				if (temp != _hit.transform.gameObject)
					angle = 0f;
				temp = _hit.transform.gameObject;
				//使用鼠标滚轮旋转切割平面
				if (Input.GetAxis ("Mouse ScrollWheel") < 0) {
					angle += scrollSpeed * Time.deltaTime;
				} else if (Input.GetAxis ("Mouse ScrollWheel") > 0) {
					angle -= scrollSpeed * Time.deltaTime;
				}

				cutVertivalDir = cutVertivalDir * Mathf.Cos (angle) + planeNormal * Mathf.Sin (angle);
				planeNormal = Vector3.Cross (dir, cutVertivalDir).normalized;

				#if DEBUG
				Debug.DrawRay (_hit.point, cutVertivalDir, Color.green);
				Debug.DrawRay (_hit.point, -dir, Color.red);
				Debug.DrawRay (_hit.point, planeNormal, Color.yellow);
				#endif
				if (Input.GetKeyDown (KeyCode.Mouse0)) {
					CutMesh ();
				}

			} else
				colliding = false;
		}
	}

	void OnPostRender() {
		if (!working||!colliding)
			return;

		if (!mat) {
			Debug.LogError("Please Assign a material on the inspector");
			return;
		}

		GL.PushMatrix(); 
		mat.SetPass(0);
		GL.Color(Color.yellow);
		GL.Begin(GL.LINES);
		GL.Vertex(_hit.point+cutVertivalDir*2f-dir*1f);
		GL.Vertex(_hit.point-cutVertivalDir*2f-dir*1f);
		GL.Vertex(_hit.point-dir*1f);
		GL.Vertex(_hit.point-planeNormal*0.2f-dir*1f);
		GL.End();
		GL.PopMatrix();

	}


	 static void CutMesh(Mesh _targetMesh){
		
	}

	 static void CutMesh(){
		if (!GetMesh ())
			return;

//		Debug.DrawRay(hitPos,cutVertivalDir,Color.green,333f);
//		Debug.DrawRay (hitPos,-dir,Color.red,333f);
//		Debug.DrawRay (hitPos, planeNormal, Color.yellow, 333f);


		tempVert1.Clear ();
		tempNormal1.Clear ();
		triangles1.Clear ();

		tempVert2.Clear ();
		tempNormal2.Clear ();
		triangles2.Clear ();

		pointIndex1.Clear ();
		pointIndex2.Clear ();

		allPoint.Clear ();

		Cut ();
		//补全截面
		GenerateSection ();

		Mesh originMesh=new Mesh(),newMesh=new Mesh();

		originMesh.vertices = tempVert1.ToArray ();
		originMesh.normals = tempNormal1.ToArray ();
		originMesh.triangles = triangles1.ToArray ();
		hitTarget.GetComponent<MeshFilter> ().mesh = originMesh;


		newMesh.vertices = tempVert2.ToArray ();
		newMesh.normals = tempNormal2.ToArray ();
		newMesh.triangles = triangles2.ToArray ();
		GameObject newObj = new GameObject ();
		newObj.transform.position = hitTarget.position;
		newObj.transform.rotation = hitTarget.rotation;
		//BoxCollider collider = newObj.AddComponent<BoxCollider> ();
		//collider.center = newMesh.bounds.center;
		//collider.size = newMesh.bounds.size;
		newObj.AddComponent<MeshFilter> ().mesh = newMesh;
		newObj.AddComponent<MeshRenderer> ();
		Material material = hitTarget.GetComponent<MeshRenderer> ().material;
		newObj.GetComponent<MeshRenderer> ().material = material;
		newObj.AddComponent<Rigidbody> ();

		Destroy (newObj,5f);

	}

	 static void Cut(){
		triangles = targetMesh.triangles;
		//遍历网格的每个面
		for (int i = 0; i < triangles.Length; i += 3) {
			
			int index1 = triangles [i], index2 = triangles [i + 1], index3 = triangles [i + 2];

			float vert1 = Vector3.Dot (planeNormal, (hitTarget.TransformPoint(targetMesh.vertices [index1]) - hitPos));
			float vert2 = Vector3.Dot (planeNormal, (hitTarget.TransformPoint(targetMesh.vertices [index2]) - hitPos));
			float vert3 = Vector3.Dot (planeNormal, (hitTarget.TransformPoint(targetMesh.vertices [index3]) - hitPos));
		

			if (vert1 >= 0 && vert2 >= 0 && vert3 >= 0) {
				CopyVert (index1, index2, index3, ref tempVert1, ref tempNormal1, ref triangles1,ref pointIndex1);
			} else if (vert1 <= 0 && vert2 <= 0 && vert3 <= 0) {
				CopyVert (index1, index2, index3, ref tempVert2, ref tempNormal2, ref triangles2,ref pointIndex2);
			} else {
				localPos.Clear();
				fbool = false;

				if (!((vert1 >0 && vert2 >0) || (vert1 < 0 && vert2 < 0))) {
					GetIntersection (index1, index2, vert1, vert2);
				}
			
				if (!((vert2 >0 && vert3 > 0) || (vert2 < 0 && vert3 < 0))) {
					GetIntersection (index2, index3, vert2, vert3);
				}
					
				if (!((vert3 > 0 && vert1 > 0) || (vert3 < 0 && vert1 < 0))) {
					GetIntersection (index3, index1, vert3, vert1);
				}
				Debug.DrawLine (hitTarget.TransformPoint(localPos [0]),hitTarget.TransformPoint(localPos [1]),Color.red,2f);

				if (vert1 >= 0) {
					if (vert2 >= 0) {
						AddVert (index1, index2,ref tempVert1,ref tempNormal1,ref triangles1,ref pointIndex1);
						AddVert2 (index3,ref tempVert2,ref tempNormal2,ref triangles2,ref pointIndex2,false);
					} else {
						if(vert3>=0){
							AddVert (index3, index1,ref tempVert1,ref tempNormal1,ref triangles1,ref pointIndex1);
							AddVert2 (index2,ref tempVert2,ref tempNormal2,ref triangles2,ref pointIndex2,false);
						}else{
							AddVert2 (index1,ref tempVert1,ref tempNormal1,ref triangles1,ref pointIndex1,true);
							AddVert (index2,index3,ref tempVert2,ref tempNormal2,ref triangles2,ref pointIndex2,false);
						}
					}
				} else {
					if(vert2>=0){
						if(vert3>=0){
							AddVert (index2, index3,ref tempVert1,ref tempNormal1,ref triangles1,ref pointIndex1);
							AddVert2 (index1,ref tempVert2,ref tempNormal2,ref triangles2,ref pointIndex2,true);
						}else{
							AddVert2 (index2,ref tempVert1,ref tempNormal1,ref triangles1,ref pointIndex1,false);
							AddVert (index3, index1, ref tempVert2, ref tempNormal2, ref triangles2,ref pointIndex2);
						}
					}else{
						AddVert2 (index3,ref tempVert1,ref tempNormal1,ref triangles1,ref pointIndex1,false);
						AddVert (index1, index2, ref tempVert2, ref tempNormal2, ref triangles2,ref pointIndex2);
					}
				}



			}
		}
		
	}

	static void GenerateSection(){
		Debug.Log ("all point count:"+allPoint.Count);
		Vector3 center=0.5f*(allPoint[0]+allPoint[allPoint.Count/2]);
		Vector3 normal = hitTarget.InverseTransformDirection (planeNormal);

		tempVert1.Add (center);
		tempNormal1.Add (-normal);

		tempVert2.Add (center);
		tempNormal2.Add (normal);


		for (int i = 0; i < allPoint.Count; i+=2) {

			tempVert1.Add (allPoint[i]);
			tempVert1.Add (allPoint[i+1]);
			tempNormal1.Add (-normal);
			tempNormal1.Add (-normal);

			Vector3 vector_1 = allPoint [i] - center;
			Vector3 vector_2 = allPoint [i + 1] - center;
			Vector3 cross_vector = Vector3.Cross (vector_1, vector_2);

			if (Vector3.Dot(normal,cross_vector)<0) {
				triangles1.Add (tempVert1.LastIndexOf (center));
				triangles1.Add (tempVert1.Count-2);
				triangles1.Add (tempVert1.Count-1);
			} else {
				triangles1.Add (tempVert1.LastIndexOf (center));
				triangles1.Add (tempVert1.Count-1);
				triangles1.Add (tempVert1.Count-2);
			}

			tempVert2.Add (allPoint[i]);
			tempVert2.Add (allPoint[i+1]);
			tempNormal2.Add (normal);
			tempNormal2.Add (normal);

			if (Vector3.Dot(normal,cross_vector)>0) {
				triangles2.Add (tempVert2.LastIndexOf (center));
				triangles2.Add (tempVert2.Count-2);
				triangles2.Add (tempVert2.Count-1);
			} else {
				triangles2.Add (tempVert2.LastIndexOf (center));
				triangles2.Add (tempVert2.Count-1);
				triangles2.Add (tempVert2.Count-2);
			}
		}
	
	}



	static void CopyVert(int index1,int index2,int index3,ref List<Vector3> tempVert,ref List<Vector3> tempNormal,ref List<int> triangles,ref Dictionary<int,int> pointIndex){

		if (!pointIndex.ContainsKey (index1)) {
			tempVert.Add (targetMesh.vertices [index1]);
			tempNormal.Add (targetMesh.normals [index1]);

			pointIndex.Add (index1,tempVert.Count-1);
		}
		if (!pointIndex.ContainsKey (index2)) {
			tempVert.Add (targetMesh.vertices [index2]);
			tempNormal.Add (targetMesh.normals [index2]);

			pointIndex.Add (index2,tempVert.Count-1);

		}
		if (!pointIndex.ContainsKey (index3)) {
			tempVert.Add (targetMesh.vertices [index3]);
			tempNormal.Add (targetMesh.normals [index3]);

			pointIndex.Add (index3,tempVert.Count-1);

		}

	
		triangles.Add (pointIndex[index1]);
		triangles.Add (pointIndex[index2]);
		triangles.Add (pointIndex[index3]);

	}
		


	static void GetIntersection(int index1,int index2,float vert1,float  vert2){
		Vector3 p;
		Vector3 lineDir;

		if (vert1 > 0 || vert1 < 0) {
			p = targetMesh.vertices [index1];
			lineDir = targetMesh.vertices [index2] - targetMesh.vertices [index1];
		} else if (vert2 > 0 || vert2 < 0) {
			p = targetMesh.vertices [index2];
			lineDir = targetMesh.vertices [index1] - targetMesh.vertices [index2];
		} else {
			p = Vector3.zero;
			lineDir = targetMesh.vertices [index2] - targetMesh.vertices [index1];
		}

		if (vert1 > 0&&!fbool) {
			fpos = targetMesh.vertices [index1];
			fbool = true;
		} else if(vert2 > 0&&!fbool){
			fpos = targetMesh.vertices [index2];
			fbool = true;
		}

		Vector3 intersection;
		intersection=hitTarget.TransformPoint(p)+hitTarget.TransformDirection(lineDir).normalized*((Vector3.Dot(hitPos,planeNormal)-Vector3.Dot(hitTarget.TransformPoint(p),planeNormal))/Vector3.Dot(hitTarget.TransformDirection(lineDir).normalized,planeNormal));

		Vector3 localIntersection = hitTarget.InverseTransformPoint (intersection);
		localPos.Add (localIntersection);
		allPoint.Add (localIntersection);
	}


	static void AddVert2(int index,ref List<Vector3> tempVert,ref List<Vector3> tempNormal,ref List<int> triangles,ref Dictionary<int,int> pointIndex,bool b){

		if (!pointIndex.ContainsKey (index)) {
			tempVert.Add (targetMesh.vertices [index]);
			tempNormal.Add (targetMesh.normals[index]);
			pointIndex.Add (index, tempVert.Count - 1);

			}


		tempVert.Add (localPos[0]);
		tempVert.Add (localPos[1]);

		tempNormal.Add (targetMesh.normals [index]);
		tempNormal.Add (targetMesh.normals [index]);

		triangles.Add (pointIndex[index]);

		if (b) {
			triangles.Add (tempVert.Count - 2);
			triangles.Add (tempVert.Count - 1);
		} else {
			triangles.Add (tempVert.Count - 1);
			triangles.Add (tempVert.Count - 2);
		}
	}


	public static void AddVert(int index1,int index2,ref List<Vector3> tempVert,ref List<Vector3> tempNormal,ref List<int> triangles,ref Dictionary<int,int> pointIndex,bool b=true){
		if (!pointIndex.ContainsKey (index1)) {
			tempVert.Add (targetMesh.vertices [index1]);
			tempNormal.Add (targetMesh.normals[index1]);
			pointIndex.Add (index1,tempVert.Count-1);

		}
		if (!pointIndex.ContainsKey (index2)) {
			tempVert.Add (targetMesh.vertices [index2]);
			tempNormal.Add (targetMesh.normals[index2]);
			pointIndex.Add (index2,tempVert.Count-1);

		}
		tempVert.Add (localPos[0]);
		tempVert.Add (localPos[1]);

		tempNormal.Add (targetMesh.normals [index1]);
		tempNormal.Add (targetMesh.normals [index2]);



		triangles.Add (pointIndex[index1]);
		triangles.Add (pointIndex[index2]);
		triangles.Add (tempVert.Count-2);

		if (fpos == targetMesh.vertices [index1]) {
			triangles.Add (pointIndex [index2]);
			triangles.Add (tempVert.Count - 1);
			triangles.Add (tempVert.Count - 2);
      

		} else if (fpos == targetMesh.vertices [index2]) {
			triangles.Add (pointIndex [index1]);
			triangles.Add (tempVert.Count - 2);
			triangles.Add (tempVert.Count - 1);

		} else {
			if (!b) {
				triangles.Add (pointIndex [index2]);
				triangles.Add (tempVert.Count - 1);
				triangles.Add (tempVert.Count - 2);
			} else {
				triangles.Add (pointIndex [index1]);
				triangles.Add (tempVert.Count - 2);
				triangles.Add (tempVert.Count - 1);
			}
		}

	}

	static bool GetMesh(){
		if (!colliding)
			return false;
		targetMesh = _hit.transform.GetComponent<MeshFilter> ().mesh;

		hitPos = _hit.point;

		hitTarget = _hit.transform;
		return true;
	}
}
