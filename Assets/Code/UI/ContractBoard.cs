using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ContractBoard : MonoBehaviour
{

    bool _boardUp = false;
    public Button wendigoStartHunt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _boardUp = false;
        if (LevelManager.Instance != null && LevelManager.Instance.currentSave != null)
        {
            Debug.Log(LevelManager.Instance);
            Debug.Log(LevelManager.Instance.currentSave);
            Debug.Log(LevelManager.Instance.currentSave.defeatedBosses);
            wendigoStartHunt.interactable = LevelManager.Instance.currentSave.defeatedBosses.Contains("Bigfoot")
                && LevelManager.Instance.currentSave.defeatedBosses.Contains("Mothman");
        }
        else
        {
            wendigoStartHunt.interactable = false;
        }
    }

    public void ToggleBoard()
    {
        if(_boardUp)
        {
            GameManager.instance.playerUI.SetActive(false);
            GameManager.instance.contractBoardListUI.SetActive(true);
            GameManager.instance.mouseVisibility();
            GameManager.instance.contractBoardCam.SetActive(true);
            GameManager.instance.player.SetActive(false);
            GameManager.instance.interactableTextObject.SetActive(false);
            GameManager.instance.contractBoardActiveMenu = GameManager.instance.contractBoardListUI;
        }
        _boardUp = !_boardUp;
    }
}
