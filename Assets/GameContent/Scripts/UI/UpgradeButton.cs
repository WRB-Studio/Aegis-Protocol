using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeButton : Button
{
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
            onClick.Invoke();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        // Pointer presses already trigger the action. Keyboard submit still uses Button.OnSubmit.
    }
}
