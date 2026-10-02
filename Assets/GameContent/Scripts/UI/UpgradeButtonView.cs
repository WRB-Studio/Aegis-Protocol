using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonView : MonoBehaviour
{
    public StationModule.eModuleType moduleType;
    public UpgradeAttribute.eUpgradeName upgradeName;
    public Button button;
    public Image symbol;
    public Image marker;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI priceText;
    public Outline selectionGlow;
}
