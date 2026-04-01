# PerlinMap 用户操作 TODO

## Phase 1：让地图先稳定跑起来

- [ ] 打开场景，找到挂有 `PerlinMapWithSingleBuilding` 的对象
- [ ] 绑定 `terrainTilemap`
- [ ] 绑定 `buildingTilemap`
- [ ] 绑定 `buildingTileAsset`
- [ ] 在 Inspector 底部点击 `填充基础地形示例资源`
- [ ] 在 Inspector 底部点击 `初始化推荐过渡骨架`
- [ ] 在 Inspector 底部点击 `填充 Phase 1 稳定草土示例`
- [ ] 将 `useRandomSeed` 先设为 `false`
- [ ] 将 `seed` 先设为 `12345`
- [ ] 使用 `Docs/PerlinMapInspectorExample.md` 里的建议参数跑第一轮
- [ ] 进入 Play Mode 检查地图是否能稳定生成

## Phase 1 验收

- [ ] 能生成草 / 土 / 水三层
- [ ] 外圈存在明显边缘收束，不会直接锯齿切断
- [ ] 不再出现大量单格水点或单格土点
- [ ] 建筑不会刷到水上
- [ ] 建筑不会贴地图边缘
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

## Phase 4：补单边 / 双边细节

- [ ] 最后再动 `UnilateralGrassBlock`
- [ ] 最后再动 `BilateralGrassBlock`
- [ ] 先把这两组资源当“同 mask 的额外变体”
- [ ] 不要直接按文件编号推方向
- [ ] 只修当前最明显的视觉问题，不追求一次性覆盖所有 mask

## 发现问题时优先检查

- [ ] `terrainTilemap` 是否真的绑到了地形层
- [ ] `buildingTilemap` 是否单独一层
- [ ] `terrainTiles.grass / soil / water` 是否都有资源
- [ ] 过渡 entry 的 `mask` 是否填对
- [ ] `diagonalMask` 是否误填成了错误值
- [ ] 水边候选图是否放反了方向
- [ ] 当前 seed 下是否刚好暴露了未配置的拓扑

## 建议的提交拆分

- [ ] 第 1 批：核心地图生成与渲染重构
- [ ] 第 2 批：Editor Helper
- [ ] 第 3 批：资源整理文档与用户 TODO

## 配套文档

- `Docs/PerlinMapTransitionAssetNotes.md`
- `Docs/PerlinMapInspectorExample.md`
