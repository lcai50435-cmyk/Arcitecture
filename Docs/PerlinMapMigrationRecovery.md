# PerlinMap 迁移恢复说明

## 适用场景

这份说明用于处理项目从其他磁盘迁移后，`PerlinMapWithSingleBuilding` 在 `SampleScene` 中出现资源引用丢失的问题，典型报错如下：

```text
❌ 基础地形资源缺失: grass, soil, water
```

当前仓库已确认：

- `Assets/File/MapResources/Block/GrassBlock.png`
- `Assets/File/MapResources/Block/SoilBlock.png`
- `Assets/File/MapResources/Block/WaterBlock.png`

这些资源文件真实存在，且 `.meta` 仍是 `Sprite` 导入配置。大多数情况下，问题是场景序列化字段为空，或者 Unity 本地导入缓存没有刷新。

## 推荐恢复顺序

### 1. 打开场景并修复引用

1. 打开 `Assets/Scenes/SampleScene.unity`
2. 选中 `NoiseManager`
3. 在 `PerlinMapWithSingleBuilding` Inspector 底部点击 `一键修复迁移后的引用`
4. 确认以下结果：
   - `terrainTilemap` 已自动指向地形 Tilemap
   - `terrainTiles.grass / soil / water` 已自动填充
   - `useRandomSeed` 被切到 `false`
   - `seed` 被设为 `12345`

### 2. 如果资源仍为空，重导入资源目录

优先级顺序固定如下：

1. 点击 `重导入 Block 资源`
2. 再次点击 `一键修复迁移后的引用`
3. 如果仍失败，再点击 `重导入全部 MapResources`
4. 再次点击 `一键修复迁移后的引用`

不建议在这一阶段移动资源路径。当前 Editor Helper 已按 `Assets/File/MapResources/...` 的固定路径查找资源，继续挪目录只会扩大问题范围。

### 3. 保存场景

修复完成后，立刻保存 `SampleScene`。这一步的目标是让 Unity 把新字段结构真正写回场景文件，而不是只停留在内存态。

## 验证标准

### 进入 Play Mode 前

- `terrainTilemap` 不为空
- `terrainTiles.grass / soil / water` 不为空
- `useRandomSeed = false`
- `seed = 12345`

### 进入 Play Mode 后

- 不再出现 `基础地形资源缺失: grass, soil, water`
- 地形 Tilemap 能刷出基础草 / 土 / 水
- 按 `R` 后旧 tile 会被清理并重新生成

### 重开场景后

- 关闭 `SampleScene`
- 重新打开 `SampleScene`
- 再次检查 `NoiseManager` 上的基础资源引用仍然存在

如果这一步通过，说明这次修复已经被序列化保存，而不是一次性的 Inspector 内存态修补。

## 常见失败点

### Inspector 按钮没有生效

优先检查 Unity Console 是否有编译错误。只要 `Assets/Editor/PerlinMapWithSingleBuildingEditor.cs` 没有正常编译，自定义 Inspector 就不会工作。

### 资源路径对不上

检查以下固定路径是否仍然存在：

- `Assets/File/MapResources/Block/GrassBlock.png`
- `Assets/File/MapResources/Block/SoilBlock.png`
- `Assets/File/MapResources/Block/WaterBlock.png`

### `.meta` 丢失或重建

如果跨磁盘迁移时漏掉了 `.meta`，Unity 会把资源当成新文件重新分配 GUID。这种情况下，场景里的旧 GUID 会全部失效，必须重新绑定并重新保存场景。

### 只有建筑相关字段为空

当前阶段允许 `buildingTileAsset` 暂时为空。这样会跳过建筑生成，但不会阻塞地形生成链路。基础地形恢复后，再补建筑资源和单独的 `buildingTilemap` 即可。
