using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class RuntimeButtonClickSfxRouterTests
{
    private GameObject buttonObject;

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
        {
            Object.DestroyImmediate(buttonObject);
        }
    }

    [Test]
    public void ShouldPlayClickForButtonReturnsTrueOnlyForActiveInteractableButtons()
    {
        buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        Button button = buttonObject.GetComponent<Button>();

        Assert.IsTrue(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button));

        button.interactable = false;
        Assert.IsFalse(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button));

        button.interactable = true;
        buttonObject.SetActive(false);
        Assert.IsFalse(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button));

        Assert.IsFalse(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(null));
    }
}
