# Arcitecture WebGL Release

## 发布链路

- 发布分支：`release/webgl`
- 正式版本 tag：`vX.Y.Z`
- Unity CI 版本：`2022.3.62f1`
- 构建入口：`ArcitectureWebGLBuildCommand.Build`
- WebGL 输出目录：`Builds/WebGL`
- GitHub Pages 发布源：GitHub Actions

## GitHub Secrets

仓库需要配置以下 Secrets：

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

这些值用于 GameCI 在 GitHub Actions runner 中激活 Unity Personal license。公开仓库使用标准 runner 时，这条 WebGL + Pages 发布链路可以走免费路径。

## 发布步骤

1. 将准备发布的代码合入 `release/webgl`。
2. 推送 `release/webgl` 后，workflow 会自动构建 WebGL 并部署 GitHub Pages 预览。
3. 在 GitHub Actions 的 `Deploy GitHub Pages` job 输出中查看预览地址。
4. 在发布提交上打 tag，例如 `v0.1.0`。
5. 推送 tag 后，workflow 会自动构建 WebGL、更新 GitHub Pages、创建 GitHub Release。

## Web 预览

`release/webgl` 分支每次推送都会更新 GitHub Pages 预览。项目页默认地址通常是：

`https://lcai50435-cmyk.github.io/Arcitecture/`

以 GitHub Actions 中 `Deploy GitHub Pages` job 输出的 `page_url` 为准。

## WebGL 运行时边界

首个网页试玩版优先保证主流程可玩。桌面版里直接依赖本地项目路径的资源读取，在 WebGL 中会走运行时兜底，后续如果需要完全还原图标和截图相册体验，应补资源目录资产或浏览器专用存储实现。
