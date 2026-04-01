# PerlinMap 过渡资源整理

## 目的

这份文档用于给 `PerlinMapWithSingleBuilding` 的过渡瓦片配置提供参考，避免直接按文件名猜方向。

当前脚本中的四向 bitmask 约定如下：

- `N = 1`：顶部
- `E = 2`：右侧
- `S = 4`：底部
- `W = 8`：左侧

对角 `diagonalMask` 约定如下：

- `NE = 1`
- `SE = 2`
- `SW = 4`
- `NW = 8`

这里的“顶部 / 右侧 / 底部 / 左侧”是以菱形地块的屏幕朝向为准，不是世界坐标轴。

## 总体结论

- `TrilateralGrassBlock` 和 `QuadrilateralGrassBlock` 的拓扑最稳定，适合优先录入。
- `GrassSoilWithWater` 可以明显分成“直岸”与“转角 / 湾口”两组，适合第二优先级录入。
- `UnilateralGrassBlock` 和 `BilateralGrassBlock` 的文件名与真实视觉形状不完全一致，不能直接按“1~4 是四个方向，5~8 是补充”理解。
- 第一轮配置建议先把“稳定资源”填进去，让系统先跑通；有歧义的资源先作为同 mask 的视觉变体，不要急着做精细对角映射。

## 推荐优先级

### 1. 先录入的稳定资源

#### `TrilateralGrassBlock`

这组基本可以直接按“缺一边”的拓扑理解：

| 文件 | 视觉结论 | 推荐 mask |
| --- | --- | --- |
| `TrilateralGrassBlock_1.png` | 右侧缺口 | `N + S + W = 13` |
| `TrilateralGrassBlock_2.png` | 底部缺口 | `N + E + W = 11` |
| `TrilateralGrassBlock_3.png` | 顶部缺口 | `E + S + W = 14` |
| `TrilateralGrassBlock_4.png` | 左侧缺口 | `N + E + S = 7` |

建议：

- 先把这 4 张分别录入 `soilGrassTransitions`。
- `diagonalMask` 先统一填 `-1`，不做对角细分。

#### `QuadrilateralGrassBlock`

| 文件 | 视觉结论 | 推荐 mask |
| --- | --- | --- |
| `QuadrilateralGrassBlock_1.png` | 四边全包 | `N + E + S + W = 15` |

建议：

- 直接录入 `soilGrassTransitions`，`mask = 15`，`diagonalMask = -1`。

### 2. 第二轮录入的稳定资源

#### `GrassSoilWithWater`

这组更适合按“直岸”和“转角”理解，而不是直接按文件名推导四向关系。

相对稳定的直岸资源：

- `GrassSoilWithWater_2.png`
- `GrassSoilWithWater_4.png`
- `GrassSoilWithWater_6.png`
- `GrassSoilWithWater_8.png`

这些图都表现为“大面积水体 + 一条明显岸线”，适合作为第一批 `waterEdgeTransitions` 或 `mixedTransitions` 的直岸变体。

相对适合补角 / 湾口的资源：

- `GrassSoilWithWater_1.png`
- `GrassSoilWithWater_3.png`
- `GrassSoilWithWater_5.png`
- `GrassSoilWithWater_7.png`

这些图更像内凹角、湾口或三者交汇的细节，不建议第一轮就强行绑定到精确 mask。更稳妥的做法是：

- 先把 `2/4/6/8` 用于基础直岸。
- 等水边方向完全确认后，再把 `1/3/5/7` 作为对角增强资源补进去。

## 当前资源存在的问题

### `UnilateralGrassBlock`

这组存在明显问题：

- 多张图不是纯粹的“单边过渡”，而是带有明显角部延展。
- `1~8` 不是简单的 4 方向旋转关系。
- 直接用“文件编号 -> mask”硬映射，极容易把边缘刷错。

建议：

- 暂时不要把这组当成精确的单边拓扑资源。
- 如果要先利用它们，建议只挑视觉最明确的几张作为同一个 `mask` 下的随机变体。
- 第一轮宁愿少配，也不要把错误方向的图塞进去。

### `BilateralGrassBlock`

这组也有相同问题：

- 有些图更像“两边 + 角部补偿”，不是严格的双边开口。
- `1~8` 之间存在明显“同一大方向下的宽窄变化”和“角部细节差异”。
- 更适合当作同拓扑下的多变体，而不是 8 张各管一个精确方向。

建议：

- 不要把整组一次性填满所有 `mask`。
- 先只挑视觉最稳定、方向最明确的图录入。
- 其余资源放到后续做 `diagonalMask` 精修时再补。

## 推荐录入顺序

### Phase 1：先把系统跑稳

1. `soilGrassTransitions`
   - `mask = 7` -> `TrilateralGrassBlock_4.png`
   - `mask = 11` -> `TrilateralGrassBlock_2.png`
   - `mask = 13` -> `TrilateralGrassBlock_1.png`
   - `mask = 14` -> `TrilateralGrassBlock_3.png`
   - `mask = 15` -> `QuadrilateralGrassBlock_1.png`
2. `waterEdgeTransitions`
   - 先录 `GrassSoilWithWater_2/4/6/8.png`
   - `diagonalMask = -1`
3. `mixedTransitions`
   - 先空着也可以
   - 或只录 `GrassSoilWithWater_1/3/5/7.png` 作为后续实验资源

### Phase 2：再补单边 / 双边

1. 先在运行中观察最常出现、最刺眼的边缘形状。
2. 从 `UnilateralGrassBlock` / `BilateralGrassBlock` 里挑视觉最接近的图。
3. 优先作为“同一 mask 下的额外 variant”加入，而不是新增复杂 `diagonalMask`。
4. 等方向全部确认后，再做对角区分。

## 配置建议

- 第一轮配置时，尽量让每个 `mask` 至少有 1 张“方向无争议”的图。
- 对方向还拿不准的资源，优先用 `diagonalMask = -1` 做 wildcard。
- 如果某个 `mask` 还没有可靠资源，宁愿退回基础地块，也不要用错误方向的过渡图。
- 先把“不会刷反”的资源配出来，地图视觉会立刻稳定很多。

## 现阶段最值得注意的坑

- 这批资源的目录名是参考，不是最终真相。
- `Unilateral / Bilateral` 不能直接按字面意思全量套进 `1, 2, 4, 8` 或 `3, 5, 6, 9, 10, 12`。
- 混合水边资源更像“岸线素材包”，不是严格按四邻接生成的标准 autotile 套装。
- 如果后面发现某些 mask 永远找不到完美图，建议接受“基础地块回退”，不要为了覆盖率牺牲方向正确性。
