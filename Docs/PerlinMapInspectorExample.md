# PerlinMap Inspector 填表示例

## 目标

这份示例对应当前代码里的第一轮稳定配置，目标不是一次性把所有过渡图填满，而是先让地图稳定生成，并且草土边缘先跑通。

## 先点哪些按钮

在 `PerlinMapWithSingleBuilding` 组件的 Inspector 底部，会看到一组 Editor Helper 按钮。

推荐顺序：

1. 如果这是迁移后的旧场景，先点击 `一键修复迁移后的引用`
2. 如果基础资源仍为空，点击 `重导入 Block 资源`
3. 再次点击 `一键修复迁移后的引用`
4. 点击 `初始化推荐过渡骨架`
5. 点击 `填充 Phase 1 稳定草土示例`

这样会自动完成：

- `terrainTilemap` 的自动回填
- `terrainTiles.grass / soil / water` 的基础资源填充
- `soilGrassTransitions` 的稳定骨架创建
- `Trilateral / Quadrilateral` 的稳定草土过渡示例填充
- `useRandomSeed = false` 与 `seed = 12345` 的稳定化设置

如果 `重导入 Block 资源` 后仍然无法填入基础资源，再继续点击 `重导入全部 MapResources`，然后重复第 1 步。

## Phase 1 示例结果

### 基础地形资源

| Inspector 字段 | 示例资源 |
| --- | --- |
| `terrainTiles.grass.sprite` | `Assets/File/MapResources/Block/GrassBlock.png` |
| `terrainTiles.soil.sprite` | `Assets/File/MapResources/Block/SoilBlock.png` |
| `terrainTiles.water.sprite` | `Assets/File/MapResources/Block/WaterBlock.png` |

### `soilGrassTransitions`

第一轮只先填稳定资源：

| description | mask | diagonalMask | 示例资源 |
| --- | --- | --- | --- |
| `Phase 1 - 左侧缺口` | `7` | `-1` | `TrilateralGrassBlock_4.png` |
| `Phase 1 - 底部缺口` | `11` | `-1` | `TrilateralGrassBlock_2.png` |
| `Phase 1 - 右侧缺口` | `13` | `-1` | `TrilateralGrassBlock_1.png` |
| `Phase 1 - 顶部缺口` | `14` | `-1` | `TrilateralGrassBlock_3.png` |
| `Phase 1 - 四边包围` | `15` | `-1` | `QuadrilateralGrassBlock_1.png` |

### `waterEdgeTransitions`

第一轮只建议先保留骨架，不建议立刻全量自动填图。

推荐先生成这 4 条骨架：

| description | mask | diagonalMask | 是否立刻填图 |
| --- | --- | --- | --- |
| `手工确认 - 顶部临水直岸` | `1` | `-1` | 否 |
| `手工确认 - 右侧临水直岸` | `2` | `-1` | 否 |
| `手工确认 - 底部临水直岸` | `4` | `-1` | 否 |
| `手工确认 - 左侧临水直岸` | `8` | `-1` | 否 |

如果你要先试候选图，优先从下面这 4 张里挑：

- `GrassSoilWithWater_2.png`
- `GrassSoilWithWater_4.png`
- `GrassSoilWithWater_6.png`
- `GrassSoilWithWater_8.png`

但这一步仍然建议进 Play Mode 逐张试，不建议盲填。

### `mixedTransitions`

第一轮同样只建议保留骨架：

| description | mask | diagonalMask |
| --- | --- | --- |
| `手工确认 - 顶右转角` | `3` | `-1` |
| `手工确认 - 右下转角` | `6` | `-1` |
| `手工确认 - 左下转角` | `12` | `-1` |
| `手工确认 - 左上转角` | `9` | `-1` |

候选图优先从下面 4 张开始观察：

- `GrassSoilWithWater_1.png`
- `GrassSoilWithWater_3.png`
- `GrassSoilWithWater_5.png`
- `GrassSoilWithWater_7.png`

## 还需要你手动填的字段

- `buildingTilemap`
- `buildingTileAsset`
- 建筑相关阈值参数
- 水边与混合过渡图

如果当前场景还没有单独的建筑层，`buildingTilemap` 可以在本轮先留空。只要 `buildingTileAsset` 也为空，系统会跳过建筑生成，不会阻塞地形验证。

## 推荐的第一轮参数

这组参数更适合先验证生成链路是否稳定：

| 字段 | 建议值 |
| --- | --- |
| `width` | `80` |
| `height` | `80` |
| `useRandomSeed` | `false` |
| `seed` | `12345` |
| `heightScale` | `18` |
| `moistureScale` | `12` |
| `octaves` | `4` |
| `persistence` | `0.5` |
| `lacunarity` | `2` |
| `waterThreshold` | `0.32` |
| `soilThreshold` | `0.52` |
| `edgeFalloffWidth` | `6` |
| `smoothingPasses` | `1` |
| `minWaterClusterSize` | `12` |
| `minSoilClusterSize` | `10` |
| `buildingMinClusterSize` | `18` |
| `buildingPadding` | `1` |
| `minDistanceFromBorder` | `3` |

## 配置完成后的预期

- 地图可以稳定生成草 / 土 / 水三层
- 草土之间会先出现三边和四边的稳定过渡
- 没配置到的过渡形状会回退到基础瓦片，不会阻塞运行
- 水边和混合边缘还可能比较粗糙，这属于第一轮预期内现象

## 配套文档

- `Docs/PerlinMapMigrationRecovery.md`
- `Docs/PerlinMapTransitionAssetNotes.md`
- `Docs/PerlinMapSetupTODO.md`
