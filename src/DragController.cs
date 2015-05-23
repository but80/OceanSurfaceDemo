using UnityEngine;
using System.Collections;

public class DragController : MonoBehaviour {

	public Camera mainCamera;
	public OceanSurfaceRenderer oceanSurfaceRenderer;

	Draggable grabbedObject;
	Vector3 lastPosition;
	Vector3 dragPosDiffOnScreen;

	void Update() {
		bool dragStart = false;
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
			Debug.Log (ray);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane)) {
				var draggable = hit.collider.gameObject.GetComponent<Draggable>();
				if (draggable != null) {
					grabbedObject = draggable;
					lastPosition = grabbedObject.transform.position;
					dragPosDiffOnScreen = Input.mousePosition - mainCamera.WorldToScreenPoint(lastPosition);
					dragStart = true;
					Debug.Log(grabbedObject.name);
				}
			}
		}
		if (grabbedObject) {
			Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition - dragPosDiffOnScreen);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane, 1<<LayerMask.NameToLayer("TransparentFX"))) {
				if (!dragStart) {
					var prev = grabbedObject.transform.position;
					var curr = grabbedObject.transform.position += hit.point - lastPosition;
					var coll = grabbedObject.GetComponent<CapsuleCollider>();
					var radius = grabbedObject.transform.TransformVector(Vector3.right * coll.radius).x;
					oceanSurfaceRenderer.BeginForce();
					oceanSurfaceRenderer.AddCircleMovement(prev, curr, grabbedObject.oceanSurfaceWaveStrength, radius, 1<<grabbedObject.oceanSurfaceMaskIndex);
					oceanSurfaceRenderer.EndForce();
				}
				lastPosition = hit.point;
			}
		}
		if (Input.GetMouseButtonUp(0)) {
			grabbedObject = null;
		}
	}
}
