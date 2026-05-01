using UnityEngine;
using UnityEngine.UI;

internal static class BuildingDetailRuntimeResolver
{
    private const string FujianDetailCanvasName = "DetailInformationFuJianCanvas";
    private const string ShuiXiangDetailCanvasName = "DetailInformationShuiXiangCanvas";

    public static CatalogueBuildingUnlockState ResolveUnlockState(
        Component source,
        CatalogueBuildingUnlockState current)
    {
        if (current != null || source == null)
        {
            return current;
        }

        UnlockedBuildingImageButton imageButton = source.GetComponent<UnlockedBuildingImageButton>();
        if (imageButton != null && imageButton.buildingUnlockState != null)
        {
            return imageButton.buildingUnlockState;
        }

        BuildingDetailOpenButton openButton = source.GetComponent<BuildingDetailOpenButton>();
        if (openButton != null && openButton.buildingUnlockState != null)
        {
            return openButton.buildingUnlockState;
        }

        return source.GetComponentInParent<CatalogueBuildingUnlockState>(true);
    }

    public static BuildingDetailData ResolveDetailData(
        Component source,
        CatalogueBuildingUnlockState unlockState,
        BuildingDetailData current)
    {
        if (current != null || source == null)
        {
            return current;
        }

        UnlockedBuildingImageButton imageButton = source.GetComponent<UnlockedBuildingImageButton>();
        if (imageButton != null && imageButton.buildingDetailData != null)
        {
            return imageButton.buildingDetailData;
        }

        BuildingDetailOpenButton openButton = source.GetComponent<BuildingDetailOpenButton>();
        if (openButton != null && openButton.buildingDetailData != null)
        {
            return openButton.buildingDetailData;
        }

        BuildingDetailData detailData = source.GetComponent<BuildingDetailData>();
        if (detailData != null)
        {
            return detailData;
        }

        if (unlockState != null)
        {
            detailData = unlockState.buildingDetailData ??
                         unlockState.GetComponent<BuildingDetailData>() ??
                         unlockState.GetComponentInChildren<BuildingDetailData>(true);
        }

        if (detailData != null)
        {
            return detailData;
        }

        return unlockState != null
            ? CreateDefinitionDetailData(source.gameObject, unlockState.BuildingId)
            : null;
    }

    public static DetailedInformationUI ResolveDetailUi(
        Component source,
        CatalogueBuildingUnlockState unlockState,
        DetailedInformationUI current,
        GameObject fallbackPanel)
    {
        CatalogueBuildingId? buildingId = unlockState != null
            ? unlockState.BuildingId
            : (CatalogueBuildingId?)null;
        DetailedInformationUI authoredDetailUi = buildingId.HasValue
            ? ResolveSceneAuthoredDetailUi(buildingId.Value)
            : null;

        if (authoredDetailUi != null)
        {
            return authoredDetailUi;
        }

        if (current != null)
        {
            EnsureDetailPanel(current);
            return current;
        }

        if (fallbackPanel != null)
        {
            DetailedInformationUI fallbackUi = fallbackPanel.GetComponent<DetailedInformationUI>();
            if (fallbackUi == null)
            {
                fallbackUi = fallbackPanel.GetComponentInChildren<DetailedInformationUI>(true);
            }

            if (fallbackUi != null)
            {
                EnsureDetailPanel(fallbackUi);
                return fallbackUi;
            }
        }

        if (source != null)
        {
            DetailedInformationUI localUi = source.GetComponent<DetailedInformationUI>() ??
                                            source.GetComponentInParent<DetailedInformationUI>(true);
            if (localUi != null)
            {
                EnsureDetailPanel(localUi);
                return localUi;
            }
        }

        DetailedInformationUI sceneUi = Object.FindObjectOfType<DetailedInformationUI>(true);
        if (sceneUi != null)
        {
            EnsureDetailPanel(sceneUi);
        }

        return sceneUi;
    }

    public static GameObject ResolveDetailPanel(DetailedInformationUI detailUi, GameObject current)
    {
        if (current != null)
        {
            return current;
        }

        return detailUi != null
            ? detailUi.detailedInformationPanel != null
                ? detailUi.detailedInformationPanel
                : detailUi.gameObject
            : null;
    }

    public static void HideOtherSceneAuthoredDetailCanvases(CatalogueBuildingUnlockState unlockState)
    {
        if (unlockState == null)
        {
            return;
        }

        string activeCanvasName = ResolveSceneAuthoredDetailCanvasName(unlockState.BuildingId);
        if (string.IsNullOrEmpty(activeCanvasName))
        {
            return;
        }

        HideSceneAuthoredDetailCanvas(FujianDetailCanvasName, activeCanvasName);
        HideSceneAuthoredDetailCanvas(ShuiXiangDetailCanvasName, activeCanvasName);
    }

    private static DetailedInformationUI ResolveSceneAuthoredDetailUi(CatalogueBuildingId buildingId)
    {
        string canvasName = ResolveSceneAuthoredDetailCanvasName(buildingId);
        if (string.IsNullOrEmpty(canvasName))
        {
            return null;
        }

        Transform detailCanvas = FindLoadedTransformByName(canvasName);
        if (detailCanvas == null)
        {
            return null;
        }

        DetailedInformationUI detailUi = detailCanvas.GetComponent<DetailedInformationUI>();
        if (detailUi == null)
        {
            detailUi = detailCanvas.gameObject.AddComponent<DetailedInformationUI>();
        }

        detailUi.detailedInformationPanel = detailCanvas.gameObject;
        BindSceneAuthoredDetailFields(detailUi, detailCanvas);
        return detailUi;
    }

    private static string ResolveSceneAuthoredDetailCanvasName(CatalogueBuildingId buildingId)
    {
        switch (buildingId)
        {
            case CatalogueBuildingId.Building1:
                return FujianDetailCanvasName;
            case CatalogueBuildingId.Building3:
                return ShuiXiangDetailCanvasName;
            default:
                return null;
        }
    }

    private static void HideSceneAuthoredDetailCanvas(string canvasName, string activeCanvasName)
    {
        if (string.Equals(canvasName, activeCanvasName, System.StringComparison.Ordinal))
        {
            return;
        }

        Transform detailCanvas = FindLoadedTransformByName(canvasName);
        if (detailCanvas != null)
        {
            detailCanvas.gameObject.SetActive(false);
        }
    }

    private static Transform FindLoadedTransformByName(string objectName)
    {
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                !string.Equals(candidate.name, objectName, System.StringComparison.Ordinal))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static BuildingDetailData CreateDefinitionDetailData(GameObject host, CatalogueBuildingId buildingId)
    {
        if (host == null)
        {
            return null;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingDetailData detailData = host.AddComponent<BuildingDetailData>();
        detailData.buildingName = !string.IsNullOrWhiteSpace(definition.detailTitle)
            ? definition.detailTitle
            : definition.displayName;
        detailData.introduction1 = definition.detailDescription;
        detailData.finalIntroduction = definition.detailDescription;
        return detailData;
    }

    private static void EnsureDetailPanel(DetailedInformationUI detailUi)
    {
        if (detailUi != null && detailUi.detailedInformationPanel == null)
        {
            detailUi.detailedInformationPanel = detailUi.gameObject;
        }
    }

    private static void BindSceneAuthoredDetailFields(DetailedInformationUI detailUi, Transform detailCanvas)
    {
        if (detailUi == null || detailCanvas == null)
        {
            return;
        }

        if (detailUi.page1NameText == null)
        {
            detailUi.page1NameText = FindText(detailCanvas, "Name", "Title");
        }

        if (detailUi.page1IntroductionText == null)
        {
            detailUi.page1IntroductionText = FindText(detailCanvas, "Introduction", "Content", "Body");
        }

        if (detailUi.page2FinallyIntroductionText == null)
        {
            detailUi.page2FinallyIntroductionText = FindText(detailCanvas, "Finally", "Final");
        }

        if (detailUi.closeButton1 == null)
        {
            detailUi.closeButton1 = FindButton(detailCanvas, "Close", "Setting", "关闭");
        }
    }

    private static Text FindText(Transform root, params string[] nameFragments)
    {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int fragmentIndex = 0; fragmentIndex < nameFragments.Length; fragmentIndex++)
        {
            string fragment = nameFragments[fragmentIndex];
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text != null &&
                    text.name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return text;
                }
            }
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private static Button FindButton(Transform root, params string[] nameFragments)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int fragmentIndex = 0; fragmentIndex < nameFragments.Length; fragmentIndex++)
        {
            string fragment = nameFragments[fragmentIndex];
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null &&
                    button.name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return button;
                }
            }
        }

        return null;
    }
}
