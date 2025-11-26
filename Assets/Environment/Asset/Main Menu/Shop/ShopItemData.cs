using UnityEngine;

public enum ShopCurrencyType { Coins, Tokens, RealMoney, Free }

[System.Serializable]
public class ShopItemData
{
    public string id;              // Unique identifier
    public string displayName;     // "100 Coins", "Retry Token", etc.
    public Sprite icon;            // Item icon
    public int price;              // Cost in coins or tokens
    public ShopCurrencyType currencyType;  // How it’s bought
}
