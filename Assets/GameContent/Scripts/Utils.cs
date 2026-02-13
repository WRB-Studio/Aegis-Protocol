using UnityEngine;
using UnityEngine.EventSystems;

public class Utils : MonoBehaviour
{
    public static string FormatNumber(int amount)
    {
        if (amount >= 1_000_000)
            return (amount / 1_000_000f).ToString("0.#") + "M";
        else if (amount >= 1000)
            return (amount / 1000f).ToString("0.#") + "k";
        else
            return amount.ToString();
    }

    public static bool IsPointerOverUI()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }
        return false;
#else
        return EventSystem.current.IsPointerOverGameObject();
#endif
    }

    public static bool IsOutOfView(Vector3 worldPosition)
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPosition);
        return viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1;
    }

    public static Sprite GetSymbolByName(UpgradeAttribute.eUpgradeName upgradeName)
    {
        Sprite[] allSymbols = Resources.LoadAll<Sprite>("Images/UpgradeSymbols");

        foreach (var sprite in allSymbols)
        {
            if (sprite.name.ToLower() == upgradeName.ToString().ToLower())
                return sprite;
        }

        Debug.LogWarning("Symbol not found: " + upgradeName.ToString().ToLower());
        return null;
    }

}
