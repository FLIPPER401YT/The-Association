using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [SerializeField] int dashMax;
    [SerializeField] float dashDistance;
    [SerializeField] float dashReloadTime;
    [SerializeField] float dashTimeLength;
    [SerializeField] float fovChangeSpeed;
    [SerializeField] float fovChangeAmount;
    [SerializeField] Rigidbody rb;
    [SerializeField] PlayerMovement movement;
    [SerializeField] AudioClip dashSound;

    public bool dashing = false;

    int dashes = 0;
    float dashTimer = 0;
    float dashingTimer = 0;
    float camFovOriginal;

    void Start()
    {
        camFovOriginal = Camera.main.fieldOfView;
    }

    public void Dash()
    {
        if (!dashing && Input.GetButtonDown("Dash") && dashes < dashMax)
        {
            Debug.Log("Dash");
            GameManager.instance.playerScript.audioSource.PlayOneShot(dashSound);
            dashing = true;
            dashingTimer = 0;
            //rb.AddForce((movement.moveDir != Vector3.zero ? movement.moveDir.normalized : transform.forward) * dashDistance * Time.deltaTime, ForceMode.Impulse);
            dashes++;
        }
    }

    void FixedUpdate()
    {
        if (dashing)
        {
            dashingTimer += Time.fixedDeltaTime;
            Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, camFovOriginal + fovChangeAmount, fovChangeSpeed * Time.deltaTime);
            rb.AddForce((movement.moveDir != Vector3.zero ? movement.moveDir.normalized : transform.forward) * dashDistance * Time.fixedDeltaTime, ForceMode.Impulse);
            if (dashingTimer >= dashTimeLength)
            {
                dashing = false;
                dashingTimer = 0;
            }
        }
        else
        {
            Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, camFovOriginal, fovChangeSpeed * Time.deltaTime);
        }
    }

    void Update()
    {
        
        if (dashes > 0)
        {
            
            updatePlayerDashUI();

            dashTimer += Time.deltaTime;
            if (dashTimer >= dashReloadTime)
            {
                dashes--;
                dashTimer = 0;
            }
        }
        else dashTimer = 0;
    }

    public void updatePlayerDashUI()
    {
        if(dashes == 1)
        {
            GameManager.instance.playerDash.fillAmount = ((dashMax - dashes) / 3f) + ((dashTimer / dashReloadTime) / 3);
        }
        else if(dashes == 2)
        {
            GameManager.instance.playerDash.fillAmount = ((dashMax - dashes) / 3f) + ((dashTimer / dashReloadTime) / 3);
        }
        else if (dashes == 3)
        {
            GameManager.instance.playerDash.fillAmount = ((dashMax - dashes) / 3f) + ((dashTimer / dashReloadTime) / 3);
        }

    }
}