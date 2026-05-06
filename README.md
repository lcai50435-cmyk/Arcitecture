# Arcitecture

## WebGL GitHub Pages

- Play URL: <https://lcai50435-cmyk.github.io/Arcitecture/>
- CI/CD workflow: <https://github.com/lcai50435-cmyk/Arcitecture/actions/workflows/webgl-release.yml>
- Deploy path: GitHub Actions builds Unity WebGL into `Builds/WebGL`, verifies `Builds/WebGL/index.html`, then publishes that directory as the GitHub Pages root.
- Download artifact: each run creates `arcitecture-webgl-${version}.zip`, with the `WebGL/` directory inside the zip.

## Release Flow

1. In `Settings > Pages`, set Source to `GitHub Actions`.
2. Add Unity CI secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.
3. Push to `master`, `main`, `release/webgl`, `release/webgl-*`, or push a `v*.*.*` tag. You can also run `WebGL Release` manually from the Actions page.

If the URL returns 404, no successful Pages deployment has published `index.html` yet. Check the latest `WebGL Release` run and confirm the `Deploy GitHub Pages` job finished successfully.
