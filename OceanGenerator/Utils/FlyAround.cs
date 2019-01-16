using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyAround : MonoBehaviour {

	private float yaw, pitch;
	public float mouseSpeed = 2f;
	public float playerSpeed = 3f;

	public Transform BallPrefab;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKey(KeyCode.Mouse0) && Application.platform != RuntimePlatform.Android) {
			yaw += 2f * Input.GetAxis("Mouse X");
        	pitch -= 2f * Input.GetAxis("Mouse Y");

        	transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
		}

		bool movingForward = false;

		if(Input.touchCount > 0) {
			yaw += .2f * Input.GetTouch(0).deltaPosition.x;
        	pitch -= .2f * Input.GetTouch(0).deltaPosition.y;

        	transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

			if(Input.GetTouch(0).tapCount == 2 && Input.GetTouch(0).phase == TouchPhase.Stationary) {
				movingForward = true;
			} 
		}

		
		if(Input.GetKey(KeyCode.W) || movingForward) {
			transform.position += transform.forward * playerSpeed * Time.deltaTime;
		}
		if(Input.GetKey(KeyCode.S)) {
			transform.position -= transform.forward * playerSpeed * Time.deltaTime;
		}
		if(Input.GetKey(KeyCode.A)) {
			transform.position += transform.TransformDirection(Vector3.left) * playerSpeed * Time.deltaTime;
		}
		if(Input.GetKey(KeyCode.D)) {
			transform.position += transform.TransformDirection(Vector3.right) * playerSpeed * Time.deltaTime;
		}

		if(Input.GetKeyDown(KeyCode.Mouse1)) {
			Transform t = Instantiate(BallPrefab);
			t.position = transform.position;
			t.GetComponent<Rigidbody>().AddForce(transform.forward * 1000);
		}
	}
}
