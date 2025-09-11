using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{

    public string message;
    public UnityEvent onInteraction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void Interact()
    {
        onInteraction.Invoke();
    }

}
