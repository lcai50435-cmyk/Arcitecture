using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class RuntimeButtonClickSfxEmitter : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        PlayIfInteractable();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayIfInteractable();
    }

    private void PlayIfInteractable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (!RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button))
        {
            return;
        }

        RuntimeButtonClickSfxRouter.PlayClick();
    }
}
