using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private int mainMenuIndex = 0;
    [SerializeField] private int gameSceneIndex = 1;

    public static SceneLoader Instance;
    private bool isBusy;
    private static readonly int FadeOut = Animator.StringToHash("FadeOut");
    private static readonly int FadeIn = Animator.StringToHash("FadeIn");

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 强制初始透明
        Image fadeImage = GetComponentInChildren<Image>();
        if (fadeImage)
        {
            Color c = Color.black;
            c.a = 0;
            fadeImage.color = c;
        }

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool(FadeOut, false);
            animator.SetBool(FadeIn, false);
        }
    }

    public void ToGame() => SwitchScene(gameSceneIndex);
    public void ToMenu() => SwitchScene(mainMenuIndex);

    void SwitchScene(int index)
    {
        if (isBusy || index == SceneManager.GetActiveScene().buildIndex) return;
        StartCoroutine(DoTransition(index));
    }

    System.Collections.IEnumerator DoTransition(int index)
    {
        isBusy = true;

        // 淡出（变黑）
        if (animator)
        {
            animator.SetBool(FadeIn, false);
            animator.SetBool(FadeOut, true);
        }
        yield return new WaitForSeconds(fadeDuration);

        // 加载场景
        yield return SceneManager.LoadSceneAsync(index);
        yield return null;

        // 淡入（变亮）
        if (animator)
        {
            animator.SetBool(FadeOut, false);
            animator.SetBool(FadeIn, true);
        }
        yield return new WaitForSeconds(fadeDuration);

        if (animator) animator.SetBool(FadeIn, false);
        isBusy = false;
    }
}