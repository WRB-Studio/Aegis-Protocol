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
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#else
        return false;
#endif
    }

    public static bool TryGetPointerDown(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPosition = Input.GetTouch(0).position;
            return true;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
#endif

        screenPosition = default;
        return false;
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
