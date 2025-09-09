using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float playerCamSensitivity;
    [SerializeField] float camLockMax, camLockMin;

    float rotX;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * playerCamSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * playerCamSensitivity * Time.deltaTime;

        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, camLockMin, camLockMax);
        transform.localRotation = Quaternion.Euler(rotX, 0, 0);
        transform.parent.Rotate(Vector3.up * mouseX);
    }
}
