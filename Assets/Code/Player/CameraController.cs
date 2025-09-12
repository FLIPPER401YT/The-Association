using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float playerCamSensitivity;
    [SerializeField] float camLockMax, camLockMin;

    public bool canLook = true;

    float rotX;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (canLook)
        {
            float mouseX = Input.GetAxis("Mouse X") * playerCamSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * playerCamSensitivity * Time.deltaTime;

            rotX -= mouseY;
            rotX = Mathf.Clamp(rotX, camLockMin, camLockMax);
            transform.localRotation = Quaternion.Euler(rotX, 0, 0);
            transform.parent.Rotate(Vector3.up * mouseX);
            Debug.Log(Vector3.up * mouseX);
            Debug.Log(transform.parent.gameObject);
        }
    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        transform.parent.localRotation = Quaternion.Euler(0, 0, 0);
    }
}
