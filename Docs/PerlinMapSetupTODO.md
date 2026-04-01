# PerlinMap 用户操作 TODO

## Phase 0：先修迁移后的资源引用

- [ ] 打开 `Assets/Scenes/SampleScene.unity`
- [ ] 选中 `NoiseManager`
- [ ] 确认 Inspector 中 `terrainTilemap` 已指向地形层 Tilemap
- [ ] 如果 `terrainTilemap` 仍为空，先点击 `一键修复迁移后的引用`
- [ ] 如果按钮执行后基础资源仍为空，点击 `重导入 Block 资源`
- [ ] 如果 `Block` 重导入后仍异常，再点击 `重导入全部 MapResources`
- [ ] 再次点击 `一键修复迁移后的引用`
- [ ] 确认 `terrainTiles.grass / soil / water` 都已填上资源
- [ ] 确认 `useRandomSeed = false`
- [ ] 确认 `seed = 12345`
- [ ] 立即保存场景

## Phase 0 验收

- [ ] 控制台不再出现 `基础地形资源缺失: grass, soil, water`
- [ ] 关闭并重新打开 `SampleScene` 后，`terrainTiles.grass / soil / water` 不会重新变空
- [ ] 如果只缺 `buildingTileAsset`，系统最多只输出建筑 warning，不会阻塞地形生成

## Phase 1：让地图先稳定跑起来

- [ ] 在 Inspector 底部点击 `初始化推荐过渡骨架`
- [ ] 在 Inspector 底部点击 `填充 Phase 1 稳定草土示例`
- [ ] 使用 `Docs/PerlinMapInspectorExample.md` 里的建议参数跑第一轮
- [ ] 进入 Play Mode 检查地图是否能稳定生成
- [ ] 按一次 `R`，确认重生时旧 tile 会被清理

## Phase 1 验收

- [ ] 能生成草 / 土 / 水三层
- [ ] 外圈存在明显边缘收束，不会直接锯齿切断
- [ ] 不再出现大量单格水点或单格土点
- [ ] 草土交界至少能看到基础的三边 / 四边过渡
- [ ] `R` 重生时旧 tile 会被清掉

## Phase 2：手动补水边

- [ ] 先只处理 `waterEdgeTransitions`
- [ ] 优先尝试 `GrassSoilWithWater_2.png`
- [ ] 优先尝试 `GrassSoilWithWater_4.png`
- [ ] 优先尝试 `GrassSoilWithWater_6.png`
- [ ] 优先尝试 `GrassSoilWithWater_8.png`
- [ ] 每次只填 1 张后进 Play Mode 看方向是否正确
- [ ] 方向不对就换图，不要一次性全量拖完

## Phase 3：补混合边缘

- [ ] 再处理 `mixedTransitions`
- [ ] 观察水、土、草三者交界最刺眼的位置
- [ ] 从 `GrassSoilWithWater_1/3/5/7.png` 里挑候选图
- [ ] 先用 `diagonalMask = -1`
- [ ] 只在确认拓扑正确后再细分 `diagonalMask`

## 发现问题时优先检查

- [ ] `terrainTilemap` 是否真的绑到了地形层
- [ ] `terrainTiles.grass / soil / water` 是否都有资源
- [ ] Unity Console 是否存在编译错误，导致自定义 Inspector 没有生效
- [ ] `Assets/File/MapResources/Block` 下的 `.png` 和 `.meta` 是否成对存在
- [ ] 过渡 entry 的 `mask` 是否填对
- [ ] `diagonalMask` 是否误填成了错误值
- [ ] 水边候选图是否放反了方向

## 配套文档

- `Docs/PerlinMapInspectorExample.md`
- `Docs/PerlinMapMigrationRecovery.md`
- `Docs/PerlinMapTransitionAssetNotes.md`
