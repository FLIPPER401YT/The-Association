using Unity.VisualScripting;
using UnityEngine;

public class ContractBoard : MonoBehaviour
{

    bool _boardUp = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _boardUp = false;
    }

    public void ToggleBoard()
    {
        if(_boardUp)
        {
            GameManager.instance.contractBoardUI.SetActive(true);
            GameManager.instance.contractBoardMouseVisibility();
            GameManager.instance.contractBoardCam.SetActive(true);
            GameManager.instance.player.SetActive(false);
            GameManager.instance.interactableTextObject.SetActive(false);
        }
        _boardUp = !_boardUp;
    }
}
