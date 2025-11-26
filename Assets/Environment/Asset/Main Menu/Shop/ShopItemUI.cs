using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button buyButton;

    private ShopItemData data;

    public void Setup(ShopItemData itemData)
    {
        data = itemData;
        if (iconImage) iconImage.sprite = data.icon;
        if (nameText) nameText.text = data.displayName;

        if (buyButton != null)
        {
            var buttonText = buyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                if (data.currencyType == ShopCurrencyType.Coins || data.currencyType == ShopCurrencyType.Tokens)
                    buttonText.text = data.price.ToString();
                else if (data.currencyType == ShopCurrencyType.RealMoney)
                    buttonText.text = "$" + data.price; // just placeholder
                else
                    buttonText.text = "FREE";
            }

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                ShopManager.Instance.TryPurchase(data);
            });
        }
    }
}
