using UnityEngine;
using System.Runtime.InteropServices;

struct OceanParticle {
	public Vector3 position;
	public float velocity;
	public float force;
	public int stopped;
	public OceanParticle(Vector3 position) {
		this.position = position;
		this.velocity = 0f;
		this.force = 0f;
		this.stopped = 0;
	}
}

struct IndexRect {
	public int j0;
	public int i0;
	public int j1;
	public int i1;
	public IndexRect(int j0, int i0, int j1, int i1) {
		this.j0 = j0;
		this.i0 = i0;
		this.j1 = j1;
		this.i1 = i1;
	}
}

//[ExecuteInEditMode]
public class OceanSurfaceRenderer : MonoBehaviour {

	const int THREAD_COUNT = 64;

	public ComputeShader computeShader;

	int divisionX;
	int divisionZ;
	float sizeX;
	float sizeZ;

	float tension = .1f; //5f
	float friction = .01f;//.05f;
	float fadeRadius = 1e10f;
	float fadeCurve = 4f;
	float density = 5f; //1f
	int iterationCount = 5;
	float minDistanceCoef = 1f;

	ComputeBuffer particlesBuffer;
	OceanParticle[] particlesData;
	int computeForceKID;
	int computePositionKID;

	int countX {
		get { return divisionX + 1; }
	}

	int countZ {
		get { return divisionZ + 1; }
	}

	float mass {
		get { return density * sizeX * sizeZ / (countX * countZ); }
	}
	
	void InitParticleData() {
		Debug.Log("SizeOf OceanParticle: " + Marshal.SizeOf(typeof(OceanParticle)));
		particlesBuffer = new ComputeBuffer(countX*countZ, Marshal.SizeOf(typeof(OceanParticle)));
		particlesData = new OceanParticle[particlesBuffer.count];
		int k = 0;
		for (int i=0; i<=divisionZ; i++) {
			float ip = (float)i/divisionZ;
			float z = Mathf.Lerp(-sizeZ*.5f, sizeZ*.5f, ip);
			for (int j=0; j<=divisionX; j++) {
				float jp = (float)j/divisionX;
				float x = Mathf.Lerp(-sizeX*.5f, sizeX*.5f, jp);
				particlesData[k++] = new OceanParticle(new Vector3(x, 0f, z));
			}
		}
		particlesBuffer.SetData(particlesData);
	}
	
	public void AddImpulse(Vector3 center, float strength, float radius) {
		particlesBuffer.GetData(particlesData);
		for (int k=0; k<particlesData.Length; k++) {
			float x = particlesData[k].position.x - center.x;
			float z = particlesData[k].position.z - center.z;
			float h = strength * Mathf.Exp(-(x*x+z*z) / (2f*radius*radius));
			particlesData[k].force += h;
		}
		particlesBuffer.SetData(particlesData);
	}

	public void BeginForce() {
		particlesBuffer.GetData(particlesData);
	}

	public void EndForce() {
		particlesBuffer.SetData(particlesData);
	}

	IndexRect localRectToIndex(float x0, float z0, float x1, float z1, float margin=0f) {
		return new IndexRect(
			Mathf.Max(        0, (int)Mathf.Floor(divisionX * Mathf.InverseLerp(-sizeX*.5f, sizeX*.5f, x0-margin))),
			Mathf.Max(        0, (int)Mathf.Floor(divisionZ * Mathf.InverseLerp(-sizeZ*.5f, sizeZ*.5f, z0-margin))),
			Mathf.Min(divisionX, (int)Mathf.Ceil (divisionX * Mathf.InverseLerp(-sizeX*.5f, sizeX*.5f, x1+margin))),
			Mathf.Min(divisionZ, (int)Mathf.Ceil (divisionZ * Mathf.InverseLerp(-sizeZ*.5f, sizeZ*.5f, z1+margin)))
		);
	}

	// @todo stoppedに入った点が持っていたエネルギーを外側に逃がす
	public void AddCircleMovement(Vector3 fromLocal, Vector3 toLocal, float strength, float radius, int objectMask) {
		Vector2 p1 = new Vector2(toLocal.x, toLocal.z);
		Vector2 p0 = new Vector2(fromLocal.x, fromLocal.z);
		Vector2 v = p1 - p0;
		float d = v.magnitude;
		if (d < 1e-6) return;

		// 移動先の円内のオフセット総計
		float totalOffset = 0f;
		IndexRect rect1 = localRectToIndex(p1.x, p1.y, p1.x, p1.y, radius);
		for (int i=rect1.i0; i<=rect1.i1; i++) {
			int k = rect1.j0 + i * countX;
			for (int j=rect1.j0; j<=rect1.j1; j++, k++) {
				Vector3 p3 = particlesData[k].position;
				Vector2 p = new Vector2(p3.x, p3.z);
				float lFrom1 = (p - p1).magnitude;
				if (lFrom1 < radius) {
					totalOffset += particlesData[k].force;
					particlesData[k].position.Set(p3.x, 0f, p3.z);
					particlesData[k].velocity = 0f;
					particlesData[k].force = 0f;
					particlesData[k].stopped |= objectMask;
				}
			}
		}

		//
		float strengthInner = strength;
		float minDistance = new Vector2(sizeX / divisionX, sizeZ / divisionZ).magnitude * minDistanceCoef;

		if (d < minDistance) {
			strengthInner *= d / minDistance;
			d = minDistance;
			v = v.normalized * d;
			p0 = p1 - v;
		}

		Vector2 vForward = v.normalized / d;
		Vector2 vSide = (new Vector2(-v.y, v.x)).normalized / radius;
		totalOffset /= strengthInner;
		
		// 押し出された水の量を計算
		float piR_d = Mathf.PI * radius + d;
		float s = piR_d * piR_d + 2f * Mathf.PI * (2 * radius * d + totalOffset);
		s = (Mathf.Sqrt(s) - piR_d) / Mathf.PI;
		float strengthOuter = strength;
		if (s < minDistance) {
			strengthOuter *= s / minDistance;
			s = minDistance;
		}
		float radiusOuter = radius + s;
		Vector2 vSideOuter = vSide.normalized / radiusOuter;
		
		//
		float z0 = Mathf.Min(p0.y, p1.y);
		float z1 = Mathf.Max(p0.y, p1.y);
		float x0 = Mathf.Min(p0.x, p1.x);
		float x1 = Mathf.Max(p0.x, p1.x);
		IndexRect rect = localRectToIndex(x0, z0, x1, z1, radiusOuter);
		for (int i=rect.i0; i<=rect.i1; i++) {
			int k = rect.j0 + i * countX;
			for (int j=rect.j0; j<=rect.j1; j++, k++) {
				Vector3 p3 = particlesData[k].position;
				Vector2 p = new Vector2(p3.x, p3.z);
				Vector2 pFrom0 = p - p0;
				Vector2 pFrom1 = p - p1;
				float lFrom0 = pFrom0.magnitude;
				float lFrom1 = pFrom1.magnitude;
				if (lFrom1 < radius) continue;
				// 移動先の円外
				int stopped = particlesData[k].stopped &= ~objectMask;
				if (stopped != 0) continue;
				float forward = Vector2.Dot(pFrom0, vForward);
				float side = Mathf.Abs(Vector2.Dot(pFrom0, vSide));
				bool forwardIn = 0 < forward && forward < 1;
				if (lFrom0 < radius || forwardIn && side < 1) {
					// 移動元の円より内側 or 軌跡の内側
					particlesData[k].force -= strengthInner;
				}
				else {
					float sideOuter = Mathf.Abs(Vector2.Dot(pFrom0, vSideOuter));
					if (lFrom1 < radius + s || forwardIn && sideOuter < forward) {
						// 移動先から押し出し半径の内側 or 軌跡の側面
						particlesData[k].force += strengthOuter;
					}
				}
			}
		}
	}

	void OnDisable() {
		particlesBuffer.Release();
	}

	void Start() {
		var tile = GetComponentInChildren<OceanSurfaceMeshGenerator>();
		divisionX = tile.divisionX;
		divisionZ = tile.divisionZ;
		sizeX = tile.sizeX;
		sizeZ = tile.sizeZ;
		//
		InitParticleData();
		computeForceKID = computeShader.FindKernel("ComputeForce");
		computePositionKID = computeShader.FindKernel("ComputePosition");
	}

	void FixedUpdate() {
		computeShader.SetBuffer(computeForceKID, "Particles", particlesBuffer);
		computeShader.SetBuffer(computePositionKID, "Particles", particlesBuffer);
		computeShader.SetFloats("Tension", new float[2]{ tension, tension });
		computeShader.SetFloat("Friction", friction);
		computeShader.SetFloat("FadeRadius", fadeRadius);
		computeShader.SetFloat("FadeCurve", fadeCurve);
		computeShader.SetFloat("Mass", mass);
		computeShader.SetInts("Division", new int[2]{ divisionX, divisionZ });
		computeShader.SetInt("XCount", countX);
		computeShader.SetInt("ZCount", countZ);
		computeShader.SetFloats("Pitch", new float[2]{ sizeX/divisionX, sizeZ/divisionZ });
		computeShader.SetFloat("DeltaTime", Time.deltaTime);
		computeShader.SetInt("Frame", Time.frameCount & 1);
		int n = particlesBuffer.count / THREAD_COUNT + 1;
		for (int i=0; i<iterationCount; i++) {
			computeShader.Dispatch(computeForceKID, n, 1, 1);
			computeShader.Dispatch(computePositionKID, n, 1, 1);
		}
	}

	void OnRenderObject() {
		var renderers = GetComponentsInChildren<MeshRenderer>();
		foreach (var renderer in renderers) {
			var material = renderer.materials[0];
			material.SetBuffer("Particles", particlesBuffer);
			material.SetInt("XDivision", divisionX);
			material.SetInt("ZDivision", divisionZ);
			material.SetInt("XCount", countX);
			material.SetInt("ZCount", countZ);
			material.SetFloat("XSize", sizeX);
			material.SetFloat("ZSize", sizeZ);
			material.SetPass(0);
		}
		Graphics.DrawProcedural(MeshTopology.Points, particlesBuffer.count);
	}

}
