using System.Collections;
using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance;

    // 分辨率列表
    private string[] resolutionOptions = new string[]
    {
        "1280 × 720",    // 默认
        "1280 × 1050",
        "1920 × 1080",   // 全高清
        "1600 × 900",    // 常用
        "2560 × 1440"    // 2K
    };

    void Awake()
    {
        // 单例模式
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 获取分辨率选项
    public string[] GetResolutionOptions()
    {
        return resolutionOptions;
    }

    // 设置分辨率（核心方法）
    public void SetResolution(int index)
    {
        if (index < 0 || index >= resolutionOptions.Length)
            return;

        // 解析分辨率字符串
        string[] parts = resolutionOptions[index].Split('×');
        int w = int.Parse(parts[0].Trim());
        int h = int.Parse(parts[1].Trim());

        Debug.Log($"[ResolutionManager] 设置分辨率: {w}×{h}");

        // 启动协程强制刷新
        StartCoroutine(ForceResolution(w, h));
    }

    // 协程：强制刷新分辨率
    IEnumerator ForceResolution(int w, int h)
    {
        // 关键步骤1：先切全屏（强制生效）
        Screen.SetResolution(w, h, true);
        yield return new WaitForSeconds(0.3f);

        // 关键步骤2：切回窗口模式（如果需要窗口）
        Screen.SetResolution(w, h, false);
        yield return new WaitForSeconds(0.1f);

        Debug.Log($"[ResolutionManager] 实际分辨率: {Screen.width}×{Screen.height}");
    }

    // 获取当前分辨率索引
    public int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutionOptions.Length; i++)
        {
            if (IsCurrentResolution(resolutionOptions[i]))
                return i;
        }
        return 0;
    }

    bool IsCurrentResolution(string resString)
    {
        string[] parts = resString.Split('×');
        if (parts.Length == 2)
        {
            int w = int.Parse(parts[0].Trim());
            int h = int.Parse(parts[1].Trim());
            return Screen.width == w && Screen.height == h;
        }
        return false;
    }
}