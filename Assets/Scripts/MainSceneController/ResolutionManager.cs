using UnityEngine;

public sealed class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance { get; private set; }

    private string[] cachedResolutionOptions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string[] GetResolutionOptions()
    {
        if (cachedResolutionOptions != null && cachedResolutionOptions.Length == GameSettingsStore.ResolutionOptionCount)
        {
            return cachedResolutionOptions;
        }

        cachedResolutionOptions = new string[GameSettingsStore.ResolutionOptionCount];
        for (int i = 0; i < cachedResolutionOptions.Length; i++)
        {
            cachedResolutionOptions[i] = GameSettingsStore.GetResolutionLabel(i);
        }

        return cachedResolutionOptions;
    }

    public void SetResolution(int index)
    {
        GameSettingsDraft draft = GameSettingsStore.CreateDraftFromSaved();
        draft.resolutionIndex = Mathf.Clamp(index, 0, GameSettingsStore.ResolutionOptionCount - 1);
        GameSettingsStore.ApplyDraft(draft);
    }

    public int GetCurrentResolutionIndex()
    {
        return GameSettingsStore.GetResolutionIndex();
    }
}
