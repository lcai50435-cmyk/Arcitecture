import fs from "node:fs/promises";
import path from "node:path";
import {
  Presentation,
  PresentationFile,
  column,
  row,
  grid,
  layers,
  panel,
  text,
  image,
  shape,
  rule,
  fill,
  hug,
  fixed,
  wrap,
  grow,
  fr,
  auto,
} from "@oai/artifact-tool";

const W = 1920;
const H = 1080;
const ROOT = path.resolve("../..");
const PROJECT = path.resolve("../../..");
const OUT = path.resolve("output/output.pptx");
const PREVIEW_DIR = path.resolve("../previews");

const assets = {
  cover: path.join(ROOT, "assets/generated-cover.png"),
  loop: path.join(ROOT, "assets/generated-loop.png"),
  repair: path.join(ROOT, "assets/generated-repair.png"),
  features: path.join(ROOT, "assets/generated-features.png"),
  baseHub: path.join(PROJECT, "Assets/Resources/BaseHub/base_hub_map.png"),
  book: path.join(PROJECT, "Assets/File/Prop/UIProp/NewUI/UIBook.png"),
  tulou: path.join(PROJECT, "Assets/File/UIResources/FuJianTuLou.png"),
  bridge: path.join(PROJECT, "Assets/File/UIResources/ZhaoGouBridge.png"),
  shuixiang: path.join(PROJECT, "Assets/File/UIResources/ShuiXiang.png"),
  dougong: path.join(PROJECT, "Assets/File/Prop/Prop/DouGong.png"),
  sunmao: path.join(PROJECT, "Assets/File/Prop/Prop/MortiseandTenon.png"),
  liangjia: path.join(PROJECT, "Assets/File/Prop/Prop/BeamFramework.png"),
  hangtU: path.join(PROJECT, "Assets/File/Prop/Prop/HangTu.png"),
  tile: path.join(PROJECT, "Assets/File/Prop/Prop/RoofTile.png"),
  prism: path.join(PROJECT, "Assets/File/Prop/Prop/Prism.png"),
  elf: path.join(PROJECT, "Assets/File/Prop/Prop/Elf.png"),
  backpack: path.join(PROJECT, "Assets/File/UIResources/BackpackSlots.png"),
  camera: path.join(PROJECT, "Assets/File/Prop/UIProp/Album/Camera.png"),
};

const color = {
  ink: "#111A17",
  ink2: "#18231D",
  parchment: "#F3E4BF",
  parchment2: "#E5CF9B",
  gold: "#D7A43B",
  gold2: "#F0C979",
  jade: "#4F8D76",
  teal: "#2A5F58",
  red: "#A34A32",
  blue: "#4C8EA8",
  muted: "#B7AA8D",
  white: "#FFF8E8",
};

const titleStyle = {
  fontFace: "PingFang SC",
  fontSize: 66,
  bold: true,
  color: color.white,
};

const subtitleStyle = {
  fontFace: "PingFang SC",
  fontSize: 30,
  color: "#E6D7B5",
};

const bodyStyle = {
  fontFace: "PingFang SC",
  fontSize: 28,
  color: color.white,
};

const smallStyle = {
  fontFace: "PingFang SC",
  fontSize: 20,
  color: "#D8C99F",
};

function slide() {
  return deck.slides.add();
}

function root(children, opts = {}) {
  return column(
    {
      name: opts.name || "root",
      width: fill,
      height: fill,
      padding: opts.padding || { x: 92, y: 72 },
      gap: opts.gap ?? 30,
    },
    children,
  );
}

function sectionKicker(value) {
  return text(value, {
    name: "section-kicker",
    width: fill,
    height: hug,
    style: { ...smallStyle, bold: true, color: color.gold2 },
  });
}

function headline(value, width = fill, size = 66) {
  return text(value, {
    name: "slide-title",
    width,
    height: hug,
    style: { ...titleStyle, fontSize: size },
  });
}

function subline(value, width = wrap(1120), size = 30) {
  return text(value, {
    name: "slide-subtitle",
    width,
    height: hug,
    style: { ...subtitleStyle, fontSize: size },
  });
}

function bullet(label, body, accent = color.gold) {
  return row(
    { name: `bullet-${label}`, width: fill, height: hug, gap: 18, align: "start" },
    [
      shape({
        name: "bullet-dot",
        geometry: "ellipse",
        width: fixed(16),
        height: fixed(16),
        fill: accent,
      }),
      column({ width: fill, height: hug, gap: 8 }, [
        text(label, {
          name: "bullet-label",
          width: fill,
          height: hug,
          style: { ...bodyStyle, fontSize: 30, bold: true, color: color.white },
        }),
        text(body, {
          name: "bullet-body",
          width: fill,
          height: hug,
          style: { ...bodyStyle, fontSize: 23, color: "#D9CEAD" },
        }),
      ]),
    ],
  );
}

function chip(value, accent = color.jade) {
  return panel(
    {
      name: `chip-${value}`,
      width: hug,
      height: hug,
      padding: { x: 22, y: 12 },
      fill: accent,
      borderRadius: 18,
    },
    text(value, {
      name: "chip-text",
      width: hug,
      height: hug,
      style: { ...smallStyle, fontSize: 22, bold: true, color: color.white },
    }),
  );
}

function darkBg(children, opts = {}) {
  return layers(
    { width: fill, height: fill },
    [
      shape({ name: "bg", width: fill, height: fill, fill: opts.fill || color.ink }),
      shape({
        name: "top-band",
        width: fill,
        height: fixed(178),
        fill: opts.band || "#1C2B23",
      }),
      root(children, opts),
    ],
  );
}

function imageStage(assetPath, fit = "contain") {
  return panel(
    {
      name: "image-stage",
      width: fill,
      height: fill,
      padding: 18,
      fill: "#E8D8B3",
      borderRadius: 24,
    },
    image({
      name: "stage-image",
      path: assetPath,
      width: fill,
      height: fill,
      fit,
      alt: "project visual",
    }),
  );
}

function labelPill(value, accent = color.gold) {
  return panel(
    {
      name: `label-${value}`,
      width: hug,
      height: hug,
      padding: { x: 18, y: 10 },
      fill: accent,
      borderRadius: 14,
    },
    text(value, {
      name: "label-text",
      width: hug,
      height: hug,
      style: { ...smallStyle, fontSize: 21, bold: true, color: color.ink },
    }),
  );
}

function compose(s, node) {
  s.compose(node, { frame: { left: 0, top: 0, width: W, height: H }, baseUnit: 8 });
}

async function saveBlob(blob, filePath) {
  if (typeof blob.save === "function") {
    await blob.save(filePath);
    return;
  }
  const buffer = Buffer.from(await blob.arrayBuffer());
  await fs.writeFile(filePath, buffer);
}

const deck = Presentation.create({ slideSize: { width: W, height: H } });

// 1. Cover
{
  const s = slide();
  compose(
    s,
    layers({ width: fill, height: fill }, [
      image({ name: "cover-art", path: assets.cover, width: fill, height: fill, fit: "cover", alt: "建筑录封面主视觉" }),
      shape({ name: "cover-veil", width: fill, height: fill, fill: "rgba(9, 17, 14, 0.58)" }),
      column(
        {
          name: "cover-copy",
          width: wrap(1060),
          height: fill,
          padding: { x: 94, y: 92 },
          gap: 30,
          justify: "center",
        },
        [
          text("Arcitecture", {
            width: fill,
            height: hug,
            style: { ...titleStyle, fontSize: 104, color: "#FFF2C5" },
          }),
          text("中国建筑录的探索修复之旅", {
            width: fill,
            height: hug,
            style: { ...titleStyle, fontSize: 54, color: color.white },
          }),
          rule({ width: fixed(360), stroke: color.gold2, weight: 5 }),
          text("用搜、打、撤的轻 Rogue 循环，让建筑知识从“被阅读”变成“被夺回”。", {
            width: wrap(900),
            height: hug,
            style: { ...subtitleStyle, fontSize: 32, color: "#F0DEAF" },
          }),
        ],
      ),
    ]),
  );
}

// 2. Design idea
{
  const s = slide();
  compose(
    s,
    darkBg([
      sectionKicker("01 设计理念"),
      headline("把古建筑保护主题，转译成玩家亲手参与的修复任务", wrap(1260), 58),
      grid(
        {
          width: fill,
          height: grow(1),
          columns: [fr(1.05), fr(0.95)],
          columnGap: 60,
          rows: [fr(1)],
          padding: { y: 26 },
        },
        [
          column({ width: fill, height: fill, gap: 30, justify: "center" }, [
            bullet("主题", "保护与理解中国古建筑智慧，不停留在百科式介绍。", color.gold),
            bullet("身份", "玩家是误入《中国建筑录》的学生，天然贴近课程与校园语境。", color.jade),
            bullet("转译", "探索、收集、修复和解锁共同构成“知识恢复”的行动链。", color.red),
          ]),
          panel(
            { width: fill, height: fill, padding: 30, fill: "#23362C", borderRadius: 26 },
            column({ width: fill, height: fill, gap: 24, justify: "center" }, [
              image({ path: assets.book, width: fill, height: fixed(340), fit: "contain", alt: "建筑录图鉴素材" }),
              row({ width: fill, height: hug, gap: 16, justify: "center" }, [
                chip("文明记忆", color.teal),
                chip("建筑修复", color.gold),
                chip("知识夺回", color.red),
                chip("参与式学习", color.jade),
              ]),
            ]),
          ),
        ],
      ),
    ]),
  );
}

// 3. Learning path
{
  const s = slide();
  compose(
    s,
    darkBg([
      sectionKicker("02 寓教于乐"),
      headline("先让玩家感到有用，再让玩家愿意理解", wrap(1240), 60),
      subline("知识不是被塞进文本框，而是逐步进入战斗、图鉴和问答。", wrap(980), 28),
      row({ width: fill, height: grow(1), gap: 34, align: "center" }, [
        stage("1", "探索中捡到结构", "先感知结构带来的战斗变化", assets.prism, color.gold),
        text("→", { width: fixed(56), height: hug, style: { fontSize: 60, bold: true, color: color.gold2 } }),
        stage("2", "图鉴中解锁建筑", "再理解建筑背景与文化含义", assets.tulou, color.jade),
        text("→", { width: fixed(56), height: hug, style: { fontSize: 60, bold: true, color: color.gold2 } }),
        stage("3", "通关后询问河狸", "最后用简短问答补充知识", assets.elf, color.blue),
      ]),
      text("学习链路：先爽感，再好奇，后理解。", {
        width: fill,
        height: hug,
        style: { ...titleStyle, fontSize: 42, color: "#FFE6A6" },
      }),
    ]),
  );
}

function stage(num, title, body, assetPath, accent) {
  return panel(
    {
      width: grow(1),
      height: fixed(520),
      padding: { x: 28, y: 30 },
      fill: "#223229",
      borderRadius: 24,
    },
    column({ width: fill, height: fill, gap: 22, align: "center" }, [
      row({ width: fill, height: hug, gap: 16, align: "center" }, [
        labelPill(num, accent),
        text(title, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 28, bold: true } }),
      ]),
      image({ path: assetPath, width: fill, height: fixed(260), fit: "contain", alt: title }),
      text(body, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 23, color: "#D9CEAD" } }),
    ]),
  );
}

// 4. Core loop
{
  const s = slide();
  compose(
    s,
    layers({ width: fill, height: fill }, [
      image({ path: assets.loop, width: fill, height: fill, fit: "cover", alt: "搜打撤循环概念图" }),
      shape({ width: fill, height: fill, fill: "rgba(8, 15, 13, 0.42)" }),
      root(
        [
          sectionKicker("03 核心玩法"),
          headline("搜、打、撤：把知识恢复做成一轮完整冒险", wrap(1250), 58),
          subline("基地出发，限时探索，战斗收集，撤回提交，修复建筑，开放下一关。", wrap(1160), 28),
          row({ width: fill, height: grow(1), gap: 24, align: "end" }, [
            loopStep("搜", "进入探索场景\n收集智慧结晶", color.gold),
            text("→", { width: fixed(44), height: hug, style: { fontSize: 46, bold: true, color: color.white } }),
            loopStep("打", "墨笔应对灾害\n结构改变攻击", color.red),
            text("→", { width: fixed(44), height: hug, style: { fontSize: 46, bold: true, color: color.white } }),
            loopStep("撤", "风险升高前\n回到基地", color.blue),
            text("→", { width: fixed(44), height: hug, style: { fontSize: 46, bold: true, color: color.white } }),
            loopStep("修", "提交收集物\n修复建筑录", color.jade),
          ]),
        ],
        { padding: { x: 86, y: 70 }, gap: 26 },
      ),
    ]),
  );
}

function loopStep(label, body, accent) {
  return panel(
    { width: grow(1), height: fixed(230), padding: 24, fill: "rgba(19, 31, 26, 0.88)", borderRadius: 24 },
    column({ width: fill, height: fill, gap: 16, justify: "center" }, [
      text(label, { width: fill, height: hug, style: { ...titleStyle, fontSize: 58, color: accent } }),
      text(body, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 25, color: "#F7E8C2" } }),
    ]),
  );
}

// 5. Rogue-lite strategy
{
  const s = slide();
  compose(
    s,
    darkBg([
      sectionKicker("04 轻 Rogue 策略"),
      headline("结构不是百科词条，而是玩家手里的构筑选择", wrap(1280), 58),
      grid(
        {
          width: fill,
          height: grow(1),
          columns: [fr(1.25), fr(0.75)],
          columnGap: 54,
          rows: [fr(1)],
          padding: { y: 16 },
        },
        [
          grid(
            {
              width: fill,
              height: fill,
              columns: [fr(1), fr(1), fr(1)],
              rows: [fr(1), fr(1)],
              columnGap: 22,
              rowGap: 22,
            },
            [
              structure("榫卯", "扇形发射", assets.sunmao, color.gold),
              structure("斗拱", "追加波次", assets.dougong, color.red),
              structure("梁架", "提升攻速", assets.liangjia, color.jade),
              structure("夯土", "射程与速度", assets.hangtU, color.blue),
              structure("瓦片", "体积与消耗", assets.tile, color.gold),
              structure("专用结构", "长期成长 / 解锁建筑", assets.prism, color.teal),
            ],
          ),
          column({ width: fill, height: fill, gap: 28, justify: "center" }, [
            bullet("临时变化", "通用结构进入背包后立即改变墨笔攻击，离开背包后失效。", color.gold),
            bullet("选择压力", "背包 6 格限制让玩家必须决定保留哪种结构组合。", color.red),
            bullet("永久目标", "专用结构服务建筑解锁与长期成长，形成重复探索动力。", color.jade),
          ]),
        ],
      ),
    ]),
  );
}

function structure(title, body, assetPath, accent) {
  return panel(
    { width: fill, height: fill, padding: 22, fill: "#24362D", borderRadius: 22 },
    column({ width: fill, height: fill, gap: 12, align: "center", justify: "center" }, [
      image({ path: assetPath, width: fill, height: fixed(150), fit: "contain", alt: title }),
      text(title, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 29, bold: true, color: accent } }),
      text(body, { width: fill, height: hug, style: { ...smallStyle, fontSize: 21, color: "#E3D6B5" } }),
    ]),
  );
}

// 6. Repair loop
{
  const s = slide();
  compose(
    s,
    layers({ width: fill, height: fill }, [
      image({ path: assets.repair, width: fill, height: fill, fit: "cover", alt: "建筑修补情绪图" }),
      shape({ width: fill, height: fill, fill: "rgba(9, 16, 13, 0.38)" }),
      root(
        [
          sectionKicker("05 建筑修补"),
          headline("从通关结果，变成“这是我修好的建筑”", wrap(1040), 60),
          subline("修复材料把基地、关卡、建筑知识与情感连接串成闭环。", wrap(920), 28),
          row({ width: wrap(1280), height: hug, gap: 18 }, [
            repairStep("收集结构", color.gold),
            text("→", { width: fixed(34), height: hug, style: { fontSize: 36, color: color.white, bold: true } }),
            repairStep("上交建筑录", color.jade),
            text("→", { width: fixed(34), height: hug, style: { fontSize: 36, color: color.white, bold: true } }),
            repairStep("领取修补材料", color.blue),
            text("→", { width: fixed(34), height: hug, style: { fontSize: 36, color: color.white, bold: true } }),
            repairStep("修复破损建筑", color.red),
          ]),
        ],
        { padding: { x: 92, y: 74 }, gap: 28 },
      ),
    ]),
  );
}

function repairStep(value, accent) {
  return panel(
    { width: fixed(260), height: hug, padding: { x: 18, y: 14 }, fill: "rgba(24, 35, 29, 0.9)", borderRadius: 16 },
    text(value, { width: fill, height: hug, style: { ...smallStyle, fontSize: 24, bold: true, color: accent } }),
  );
}

// 7. Player hooks
{
  const s = slide();
  compose(
    s,
    layers({ width: fill, height: fill }, [
      image({ path: assets.features, width: fill, height: fill, fit: "cover", alt: "特色四宫格概念图" }),
      shape({ width: fill, height: fill, fill: "rgba(8, 14, 12, 0.28)" }),
      root(
        [
          sectionKicker("06 特色体验"),
          headline("玩家留下来，不只因为能学到，也因为过程足够好玩", wrap(1320), 56),
          row({ width: fill, height: grow(1), gap: 24, align: "end" }, [
            hook("截图合拍", "和建筑合影，留在相册里。", color.gold),
            hook("策略选择", "临时构筑 + 永久成长。", color.jade),
            hook("河狸问答", "通关后追问建筑知识。", color.blue),
            hook("割草爽感", "低压力，高反馈，先玩进去。", color.red),
          ]),
        ],
        { padding: { x: 86, y: 68 }, gap: 22 },
      ),
    ]),
  );
}

function hook(title, body, accent) {
  return panel(
    { width: grow(1), height: fixed(220), padding: 24, fill: "rgba(19, 30, 25, 0.90)", borderRadius: 22 },
    column({ width: fill, height: fill, gap: 12, justify: "center" }, [
      text(title, { width: fill, height: hug, style: { ...titleStyle, fontSize: 36, color: accent } }),
      text(body, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 23, color: "#F3E5BE" } }),
    ]),
  );
}

// 8. Technology
{
  const s = slide();
  compose(
    s,
    darkBg([
      sectionKicker("07 技术实现"),
      headline("技术不单独炫技，而是服务这条体验闭环", wrap(1220), 58),
      grid(
        {
          width: fill,
          height: grow(1),
          columns: [fr(1), fr(1), fr(1)],
          rows: [fr(1), fr(1)],
          columnGap: 26,
          rowGap: 26,
          padding: { y: 20 },
        },
        [
          tech("关卡解锁", "建筑修复状态决定后续关卡开放", "GameplayStageCatalog", color.gold),
          tech("永久进度", "修复、图鉴、属性成长写入存档", "RuntimeProgressState", color.jade),
          tech("墨水词条", "背包结构实时影响武器形态", "InkModifierRuntimeConfig", color.red),
          tech("河狸助手", "本地知识库、关键词问答、简短科普", "BeaverAssistantRuntime", color.blue),
          tech("相册系统", "截图保存、相册查看、情感留存", "PhotoAlbumRepository", color.gold),
          tech("失败流", "死亡与超时让撤离决策成立", "GameplayFailureController", color.teal),
        ],
      ),
    ]),
  );
}

function tech(title, body, code, accent) {
  return panel(
    { width: fill, height: fill, padding: 28, fill: "#24362D", borderRadius: 22 },
    column({ width: fill, height: fill, gap: 16, justify: "center" }, [
      text(title, { width: fill, height: hug, style: { ...titleStyle, fontSize: 34, color: accent } }),
      text(body, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 24, color: "#F0E1BB" } }),
      rule({ width: fixed(160), stroke: accent, weight: 3 }),
      text(code, { width: fill, height: hug, style: { ...smallStyle, fontSize: 18, color: "#BAAC87" } }),
    ]),
  );
}

// 9. Demo path
{
  const s = slide();
  compose(
    s,
    darkBg([
      sectionKicker("08 项目完成度与演示路径"),
      headline("现场演示按真实主循环走，评委最容易看懂", wrap(1200), 58),
      grid(
        {
          width: fill,
          height: grow(1),
          columns: [fr(0.95), fr(1.05)],
          rows: [fr(1)],
          columnGap: 56,
          padding: { y: 12 },
        },
        [
          imageStage(assets.baseHub, "contain"),
          column({ width: fill, height: fill, gap: 22, justify: "center" }, [
            demo("01", "从基地进入关卡", color.gold),
            demo("02", "收集结构并展示武器变化", color.jade),
            demo("03", "撤回基地提交建筑录", color.blue),
            demo("04", "领取修复材料并修复建筑", color.red),
            demo("05", "查看图鉴、相册和河狸问答", color.gold),
          ]),
        ],
      ),
    ]),
  );
}

function demo(num, body, accent) {
  return row({ width: fill, height: hug, gap: 20, align: "center" }, [
    labelPill(num, accent),
    text(body, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 30, color: color.white } }),
  ]);
}

// 10. Closing
{
  const s = slide();
  compose(
    s,
    darkBg(
      [
        sectionKicker("09 总结"),
        headline("我们希望玩家记住的，不只是建筑名字", wrap(1200), 62),
        subline("而是亲手把它修复回来的过程。", wrap(900), 42),
        grid(
          {
            width: fill,
            height: grow(1),
            columns: [fr(1), fr(1), fr(1)],
            columnGap: 34,
            padding: { y: 36 },
          },
          [
            ending("游戏价值", "搜打撤循环清晰，战斗反馈轻松，构筑变化明确。", color.gold),
            ending("教育价值", "建筑知识被嵌入收集、修复、问答与相册记忆中。", color.jade),
            ending("情感价值", "玩家不是旁观者，而是建筑录的修复者。", color.red),
          ],
        ),
        text("Arcitecture · 中国建筑录", {
          width: fill,
          height: hug,
          style: { ...smallStyle, fontSize: 22, color: "#BEAF8B" },
        }),
      ],
      { band: "#2C231A" },
    ),
  );
}

function ending(title, body, accent) {
  return panel(
    { width: fill, height: fixed(330), padding: 30, fill: "#24362D", borderRadius: 24 },
    column({ width: fill, height: fill, gap: 20, justify: "center" }, [
      text(title, { width: fill, height: hug, style: { ...titleStyle, fontSize: 40, color: accent } }),
      text(body, { width: fill, height: hug, style: { ...bodyStyle, fontSize: 25, color: "#E8D9B6" } }),
    ]),
  );
}

await fs.mkdir(path.dirname(OUT), { recursive: true });
await fs.mkdir(PREVIEW_DIR, { recursive: true });

const pptx = await PresentationFile.exportPptx(deck);
await saveBlob(pptx, OUT);

for (let i = 0; i < deck.slides.length; i += 1) {
  const png = await deck.slides.get(i).export({ format: "png" });
  await saveBlob(png, path.join(PREVIEW_DIR, `slide-${String(i + 1).padStart(2, "0")}.png`));
}

console.log(JSON.stringify({ slides: deck.slides.length, pptx: OUT, previews: PREVIEW_DIR }, null, 2));
