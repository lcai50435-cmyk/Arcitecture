using System;
using UnityEngine;

public static class BeaverQuoteLibrary
{
    private const string SpeakerPrefix = "河狸：";

    private static readonly string[] AmbientExplorationQuotes =
    {
        "击败怪物说不定有意外惊喜哦。",
        "收集齐全结构才能获得修补材料。",
        "每一份结构可能带来意想不到的加成。",
        "取舍是探索中最重要的功课。",
        "不同构件搭配会产生额外效果。",
        "合影也是夺回记忆的一种方式。",
        "和修复前的古建做最后留念。",
        "不要忘了，你可以随时按 P 和建筑合拍。",
        "建筑录恢复力量才可以查看完整建筑知识。",
        "记住，五分钟是你能停留的最长时间。",
        "合适的组合比稀有度更重要。",
        "没找到全部也别灰心，下次再来。",
        "探索本身就是一种修复。",
        "哪怕只找到一个构件也有价值。",
        "背包有限，选对的不选多的。"
    };

    private static readonly string[] AmbientStructureCombinationQuotes =
    {
        "斗拱配上飞椽，出檐更深远。",
        "有了柱础和地栿，柱子才稳当。",
        "正脊两端安鸱吻，才算完整。",
        "影壁配漏窗，隔而不堵有灵气。",
        "梁枋交圈，房子才成一个整体。",
        "翼角垂脊戗脊，缺一不可。",
        "榫卯咬紧，胜过千钉万胶。",
        "瓦当滴水瓦，一阴一阳配合。",
        "三层斗拱叠起来能托起天。"
    };

    private static readonly string[] AmbientEncouragementQuotes =
    {
        "每一块碎片都是一段记忆。",
        "修复的不只是房子，是文明的根。",
        "别急，古人用榫卯从不用蛮力。",
        "你看，古人的智慧就藏在这里。",
        "收集回来交给我，剩下的我来。",
        "一本建筑录，半部中华史。",
        "我们一起把失落的知识找回来。",
        "古人搭房子，也搭生活的秩序。",
        "我们修的是房子，也是文明。",
        "相信你能修好这个世界。",
        "夺回本就属于我们的建筑知识。"
    };

    private static readonly StructureFact[] StructureFacts =
    {
        new StructureFact(new[] { "斗拱", "出跳", "承重" }, "斗拱层层出跳，能以柔克刚承重。"),
        new StructureFact(new[] { "榫卯", "榫", "卯" }, "榫卯不用钉子，木头咬住木头。"),
        new StructureFact(new[] { "雀替" }, "雀替帮梁柱分担压力。"),
        new StructureFact(new[] { "翼角", "飞檐" }, "翼角总是像鸟儿展翅起飞。"),
        new StructureFact(new[] { "正脊" }, "正脊横在屋顶最高处镇守。"),
        new StructureFact(new[] { "垂脊" }, "垂脊从正脊斜下，如瀑布流淌。"),
        new StructureFact(new[] { "柱础", "石墩" }, "柱础是石墩，隔潮又稳根基。"),
        new StructureFact(new[] { "影壁" }, "影壁挡住视线，藏风又聚气。"),
        new StructureFact(new[] { "藻井" }, "藻井层层收拢像倒扣华盖。"),
        new StructureFact(new[] { "瓦当" }, "瓦当是屋檐的圆帽子，挡雨。"),
        new StructureFact(new[] { "脊兽" }, "脊兽排排坐在屋脊上守护。"),
        new StructureFact(new[] { "望板" }, "望板铺在椽上，托住瓦片。"),
        new StructureFact(new[] { "飞椽" }, "飞椽翘起让屋檐更轻盈。"),
        new StructureFact(new[] { "鸱吻" }, "鸱吻张口吞脊，护宅驱火。"),
        new StructureFact(new[] { "漏窗" }, "漏窗借景，墙上有画。"),
        new StructureFact(new[] { "门槛" }, "门槛虽矮，能挡风雨入屋。"),
        new StructureFact(new[] { "台基" }, "台基抬高房子，防潮又气派。"),
        new StructureFact(new[] { "额枋" }, "额枋联络柱子，让房子不散架。"),
        new StructureFact(new[] { "地栿" }, "地栿贴地，锁住柱脚不偏移。"),
        new StructureFact(new[] { "角梁" }, "角梁撑起翼角，弧度全靠它。"),
        new StructureFact(new[] { "山墙" }, "山墙封住左右，抗风又防火。"),
        new StructureFact(new[] { "檩条", "脊檩" }, "檩条架在梁上，托起整片瓦。"),
        new StructureFact(new[] { "椽子" }, "椽子密排，是屋顶的骨架。"),
        new StructureFact(new[] { "叉手" }, "叉手斜撑，稳住最顶上的脊檩。")
    };

    public static string GetAmbientQuote(string sceneName, bool hasAccessibleBuildingKnowledge)
    {
        string quote = SelectAmbientQuote(sceneName, hasAccessibleBuildingKnowledge);
        return string.IsNullOrWhiteSpace(quote) ? string.Empty : SpeakerPrefix + quote;
    }

    public static bool TryAnswerStructureQuestion(string query, out string answer)
    {
        answer = null;
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        string normalized = query.Trim();
        for (int i = 0; i < StructureFacts.Length; i++)
        {
            StructureFact fact = StructureFacts[i];
            for (int keywordIndex = 0; keywordIndex < fact.Keywords.Length; keywordIndex++)
            {
                if (normalized.IndexOf(fact.Keywords[keywordIndex], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    answer = fact.Answer;
                    return true;
                }
            }
        }

        return false;
    }

    private static string SelectAmbientQuote(string sceneName, bool hasAccessibleBuildingKnowledge)
    {
        bool gameplayScene = GameplayStageCatalog.IsGameplayScene(sceneName);
        int total = AmbientEncouragementQuotes.Length;
        if (gameplayScene)
        {
            total += AmbientExplorationQuotes.Length;
        }

        if (hasAccessibleBuildingKnowledge)
        {
            total += AmbientStructureCombinationQuotes.Length;
        }

        int index = UnityEngine.Random.Range(0, total);
        if (gameplayScene)
        {
            if (index < AmbientExplorationQuotes.Length)
            {
                return AmbientExplorationQuotes[index];
            }

            index -= AmbientExplorationQuotes.Length;
        }

        if (hasAccessibleBuildingKnowledge)
        {
            if (index < AmbientStructureCombinationQuotes.Length)
            {
                return AmbientStructureCombinationQuotes[index];
            }

            index -= AmbientStructureCombinationQuotes.Length;
        }

        return AmbientEncouragementQuotes[index];
    }

    private readonly struct StructureFact
    {
        public StructureFact(string[] keywords, string answer)
        {
            Keywords = keywords;
            Answer = answer;
        }

        public readonly string[] Keywords;
        public readonly string Answer;
    }
}
