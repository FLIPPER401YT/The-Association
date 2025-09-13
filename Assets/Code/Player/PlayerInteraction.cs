using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{

    public float playerReachDist = 2f;
    Interactable currInteractable;

    // Update is called once per frame
    void Update()
    {
        checkInteraction();
        if(Input.GetKeyDown(KeyCode.E) && currInteractable != null)
        {
            currInteractable.Interact();
        }
    }

    void checkInteraction()
    {
        RaycastHit hit;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.position);

        if(Physics.Raycast(ray, out hit, playerReachDist))
        {
            if(hit.collider.tag == "Interactable")
            {
                Interactable newInteractable = hit.collider.GetComponent<Interactable>();
                
                if (!newInteractable) return;

                if (newInteractable.enabled)
                {
                    SetNewCurrentInteractable(newInteractable);
                }
                else
                {
                    DisableCurrentInteractable();
                }
            }
            else
            {
                DisableCurrentInteractable();
            }
        }
        else
        {
            DisableCurrentInteractable();
        }

        
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        currInteractable = newInteractable;
        GameManager.instance.enableInteractableText(currInteractable.message);
    }

    void DisableCurrentInteractable()
    {
        GameManager.instance.disableInteractableText();
        if(currInteractable)
        { 
            currInteractable = null; 
        }
    }
}
