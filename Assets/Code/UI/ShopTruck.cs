using UnityEngine;

public class ShopTruck : MonoBehaviour
{
    bool _shopUp = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _shopUp = false;
    }

    public void ToggleShop()
    {
        if (_shopUp)
        {
            GameManager.instance.playerUI.SetActive(false);
            GameManager.instance.shopUI.SetActive(true);
            GameManager.instance.mouseVisibility();
            GameManager.instance.shopCam.SetActive(true);
            GameManager.instance.player.SetActive(false);
            GameManager.instance.interactableTextObject.SetActive(false);
        }
        _shopUp = !_shopUp;
    }

}
