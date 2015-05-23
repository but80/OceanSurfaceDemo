using UnityEngine;

public class CameraController : MonoBehaviour {

	private Vector3 prevPosition;
	
	void Start() {
		transform.LookAt(transform.parent.position);
	}
	
	void Update() {
		float wheel = Input.GetAxis("Mouse ScrollWheel");
		if (wheel != 0f) transform.localPosition *= Mathf.Max(.1f, 1f-wheel);
		
		var pos = Input.mousePosition;
		if (Input.GetMouseButton(1)) {
			var diff = pos - prevPosition;
			if (0f < diff.magnitude) {
				var parent = transform.parent;
				parent.RotateAround(parent.position, Vector3.up, diff.x);
				parent.RotateAround(parent.position, parent.right, -diff.y);
			}
		}
		prevPosition = pos;
	}

}
