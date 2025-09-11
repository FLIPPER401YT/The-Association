using UnityEngine;
using UnityEngine.Events;

public class ContractBoard : MonoBehaviour
{

    bool _boardUp = false;
    public UnityEvent onBoard, offBoard;

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
            onBoard.Invoke();
        }
        //else
        //{
        //    GameManager.instance.player.SetActive(true);
        //    GameManager.instance.contractBoardCam.SetActive(false);
        //    offBoard.Invoke();
        //}
        _boardUp = !_boardUp;
    }
}
