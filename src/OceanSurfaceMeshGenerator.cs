using UnityEngine;
using System.Collections;
using UnityEditor;

public class OceanSurfaceMeshGenerator : MonoBehaviour {

	public Mesh templateMesh;
	public int divisionX = 150;
	public int divisionZ = 100;
	public float sizeX = 150f;
	public float sizeZ = 100f;
	public string saveAsAnAssetInPath = ""; // Example: "Assets/OceanSurface/OceanSurfaceMesh.asset"
	
	void Start() {
		int countX = divisionX + 1;
		int countZ = divisionZ + 1;
		var vertices = new Vector3[countX * countZ];
		var uv = new Vector2[countX * countZ];
		int k = 0;
		for (int i=0; i<=divisionZ; i++) {
			for (int j=0; j<=divisionX; j++) {
				float u = (float)j / divisionX;
				float v = (float)i / divisionZ;
				vertices[k].Set((u - .5f) * sizeX, 0f, (v - .5f) * sizeZ);
				uv[k++].Set(u, v);
			}
		}

		var triangles = new int[6 * divisionX * divisionZ];
		int l=0, kTL=0, kTR=1, kBL=countX, kBR=countX+1;
		for (int i=0; i<divisionZ; i++) {
			for (int j=0; j<divisionX; j++) {
				triangles[l++] = kTL;
				triangles[l++] = kBL++;
				triangles[l++] = kBR;
				triangles[l++] = kTR++;
				triangles[l++] = kBR++;
				triangles[l++] = kTL++;
			}
			kTL++; kTR++; kBL++; kBR++;
		}

		var mesh = Instantiate(templateMesh);
		mesh.name = "OceanSurfaceMesh";
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		var filter = GetComponent<MeshFilter>();
		filter.sharedMesh = mesh;

		if (saveAsAnAssetInPath != "") {
			AssetDatabase.CreateAsset(mesh, saveAsAnAssetInPath);
			AssetDatabase.SaveAssets();
		}
	}

}
