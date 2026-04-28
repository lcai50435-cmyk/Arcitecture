using System;
using UnityEngine;

public enum MusicCueId
{
    None = 0,
    StartScreen = 1,
    SafeHouse = 2,
    Stage = 3,
    Fight = 4
}

public enum SfxCueId
{
    None = 0,
    PlayerAttack = 1,
    FireMonsterCast = 2,
    HandbookBookmark = 3,
    SlotSwitch = 4,
    ButtonClick = 5
}

[Serializable]
public struct MusicCueDefinition
{
    public MusicCueId cueId;
    public AudioClip clip;
    [Range(0f, 1f)] public float gain;
}

[Serializable]
public struct SfxCueDefinition
{
    public SfxCueId cueId;
    public AudioClip clip;
    public AudioClip[] variants;
    [Range(0f, 1f)] public float gain;
}

[CreateAssetMenu(fileName = "ArcitectureAudioCatalog", menuName = "Arcitecture/Audio Catalog")]
public sealed class ArcitectureAudioCatalog : ScriptableObject
{
    [SerializeField] private MusicCueDefinition[] musicCues = Array.Empty<MusicCueDefinition>();
    [SerializeField] private SfxCueDefinition[] sfxCues = Array.Empty<SfxCueDefinition>();

    public AudioClip GetMusicClip(MusicCueId cueId)
    {
        for (int i = 0; i < musicCues.Length; i++)
        {
            if (musicCues[i].cueId == cueId)
            {
                return musicCues[i].clip;
            }
        }

        return null;
    }

    public float GetMusicGain(MusicCueId cueId)
    {
        for (int i = 0; i < musicCues.Length; i++)
        {
            if (musicCues[i].cueId == cueId)
            {
                return Mathf.Clamp01(musicCues[i].gain);
            }
        }

        return 1f;
    }

    public AudioClip GetSfxClip(SfxCueId cueId)
    {
        for (int i = 0; i < sfxCues.Length; i++)
        {
            if (sfxCues[i].cueId == cueId)
            {
                return SelectSfxClip(sfxCues[i]);
            }
        }

        return null;
    }

    private static AudioClip SelectSfxClip(SfxCueDefinition definition)
    {
        AudioClip[] variants = definition.variants;
        if (variants != null && variants.Length > 0)
        {
            int validCount = 0;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount > 0)
            {
                int targetIndex = UnityEngine.Random.Range(0, validCount);
                for (int i = 0; i < variants.Length; i++)
                {
                    if (variants[i] == null)
                    {
                        continue;
                    }

                    if (targetIndex == 0)
                    {
                        return variants[i];
                    }

                    targetIndex--;
                }
            }
        }

        return definition.clip;
    }

    public float GetSfxGain(SfxCueId cueId)
    {
        for (int i = 0; i < sfxCues.Length; i++)
        {
            if (sfxCues[i].cueId == cueId)
            {
                return Mathf.Clamp01(sfxCues[i].gain);
            }
        }

        return 1f;
    }
}
