using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private Transform currencyTabParent;
    [SerializeField] private Transform skinsTabParent;
    [SerializeField] private Transform powerupsTabParent;
    [SerializeField] private Transform specialsTabParent;

    [Header("Item Data")]
    public List<ShopItemData> currencyItems = new List<ShopItemData>();
    public List<ShopItemData> skinItems = new List<ShopItemData>();
    public List<ShopItemData> powerupItems = new List<ShopItemData>();
    public List<ShopItemData> specialItems = new List<ShopItemData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        PopulateTab(currencyItems, currencyTabParent);
        PopulateTab(skinItems, skinsTabParent);
        PopulateTab(powerupItems, powerupsTabParent);
        PopulateTab(specialItems, specialsTabParent);
    }

    private void PopulateTab(List<ShopItemData> items, Transform parent)
    {
        foreach (var data in items)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, parent);
            ShopItemUI ui = itemObj.GetComponent<ShopItemUI>();
            if (ui != null) ui.Setup(data);
        }
    }

    public void TryPurchase(ShopItemData item)
    {
        switch (item.currencyType)
        {
            case ShopCurrencyType.Coins:
                if (MasterInfo.totalCoins >= item.price)
                {
                    MasterInfo.totalCoins -= item.price;
                    PlayerPrefs.SetInt("TotalCoins", MasterInfo.totalCoins);
                    PlayerPrefs.Save();
                    Debug.Log("✅ Bought " + item.displayName);
                }
                else
                {
                    Debug.Log("❌ Not enough coins for " + item.displayName);
                    // Later: DialogManager.ShowInfo("Not enough coins!");
                }
                break;

            case ShopCurrencyType.Tokens:
                if (MasterInfo.totalCoins >= item.price)
                {
                    MasterInfo.totalCoins -= item.price;
                    MasterInfo.retryTokens += 1;
                    PlayerPrefs.SetInt("TotalCoins", MasterInfo.totalCoins);
                    PlayerPrefs.SetInt("RetryTokens", MasterInfo.retryTokens);
                    PlayerPrefs.Save();
                    Debug.Log("✅ Bought a retry token!");
                }
                else
                {
                    Debug.Log("❌ Not enough coins for token.");
                }
                break;

            case ShopCurrencyType.RealMoney:
                Debug.Log("🛒 IAP purchase simulated: " + item.displayName);
                break;

            case ShopCurrencyType.Free:
                Debug.Log("🎁 Freebie claimed: " + item.displayName);
                break;
        }
    }
}
