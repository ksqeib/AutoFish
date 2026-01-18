using System;
using System.Linq;
using System.Text;
using AutoFish.Data;
using Terraria;
using TShockAPI;

namespace AutoFish.AFMain;

/// <summary>
///     自动钓鱼插件的聊天命令处理。
/// </summary>
public class Commands
{
    /// <summary>
    ///     向玩家展示自动钓鱼指令帮助。
    /// </summary>
    private static void HelpCmd(TSPlayer player)
    {
        var helpMessage = new StringBuilder();
        helpMessage.Append("          [i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");

        // 个人指令（按权限和全局开关过滤）
        helpMessage.Append("\n/af -- 查看自动钓鱼菜单");
        helpMessage.Append("\n/af status -- 查看个人状态");

        if (AutoFish.Config.GlobalAutoFishFeatureEnabled && AutoFish.HasFeaturePermission(player, "autofish.fish"))
            helpMessage.Append("\n/af fish -- 开启丨关闭[c/4686D4:自动钓鱼]功能");

        if (AutoFish.Config.GlobalBuffFeatureEnabled && AutoFish.HasFeaturePermission(player, "autofish.buff"))
            helpMessage.Append("\n/af buff -- 开启丨关闭[c/F6B152:钓鱼BUFF]");

        if (AutoFish.Config.GlobalMultiHookFeatureEnabled && AutoFish.HasFeaturePermission(player, "autofish.multihook"))
        {
            helpMessage.Append("\n/af multi -- 开启丨关闭[c/87DF86:多钩功能]");
            helpMessage.Append("\n/af hook 数字 -- 设置个人钩子上限 (<= 全局上限)");
        }

        if (AutoFish.Config.GlobalSkipNonStackableLoot && AutoFish.HasFeaturePermission(player, "autofish.filter.unstackable"))
            helpMessage.Append("\n/af stack -- 开启丨关闭[c/F4C17F:过滤不可堆叠渔获]");

        if (AutoFish.Config.GlobalBlockMonsterCatch && AutoFish.HasFeaturePermission(player, "autofish.filter.monster"))
            helpMessage.Append("\n/af monster -- 开启丨关闭[c/F48FB1:不钓怪物]");

        if (AutoFish.Config.GlobalSkipFishingAnimation && AutoFish.HasFeaturePermission(player, "autofish.skipanimation"))
            helpMessage.Append("\n/af anim -- 开启丨关闭[c/8EC4F4:跳过上鱼动画]");

        if (AutoFish.Config.GlobalConsumptionModeEnabled)
            helpMessage.Append("\n/af list -- 列出消耗模式[c/F5F251:指定物品表]");

        if (AutoFish.Config.ExtraCatchItemIds.Count != 0)
            helpMessage.Append("\n/af loot -- 查看[c/F25055:额外渔获表]");

        // 管理员附加指令（末尾追加，避免遮挡玩家指令）
        if (player.HasPermission("autofish.admin"))
        {
            helpMessage.Append("\n[全局] /af gbuff -- 开启丨关闭全局钓鱼BUFF");
            helpMessage.Append("\n[全局] /af gmore -- 开启丨关闭多线模式");

            if (AutoFish.Config.GlobalMultiHookFeatureEnabled)
                helpMessage.Append("\n[全局] /af gduo 数字 -- 设置多线的钩子数量上限");

            helpMessage.Append("\n[全局] /af gmod -- 开启丨关闭消耗模式");

            if (AutoFish.Config.GlobalConsumptionModeEnabled)
            {
                helpMessage.Append("\n[全局] /af gset 数量 -- 设置消耗物品数量要求");
                helpMessage.Append("\n[全局] /af gtime 数字 -- 设置自动时长(分钟)");
                helpMessage.Append("\n[全局] /af gadd 物品名 -- 添加指定鱼饵");
                helpMessage.Append("\n[全局] /af gdel 物品名 -- 移除指定鱼饵");
            }

            helpMessage.Append("\n[全局] /af gaddloot 物品名 -- 添加额外渔获");
            helpMessage.Append("\n[全局] /af gdelloot 物品名 -- 移除额外渔获");
            helpMessage.Append("\n[全局] /af gstack -- 开启丨关闭过滤不可堆叠渔获");
            helpMessage.Append("\n[全局] /af gmonster -- 开启丨关闭不钓怪物");
            helpMessage.Append("\n[全局] /af gani -- 开启丨关闭跳过上鱼动画");
        }

        player.SendMessage(helpMessage.ToString(), 193, 223, 186);
    }

    /// <summary>
    ///     处理 /af 相关指令的入口。
    /// </summary>
    public static void Afs(CommandArgs args)
    {
        var player = args.Player;

        if (!AutoFish.Config.PluginEnabled) return;

        var playerData = AutoFish.PlayerData.GetOrCreatePlayerData(player.Name, AutoFish.CreateDefaultPlayerData);

        //消耗模式下的剩余时间记录
        var remainingMinutes = AutoFish.Config.RewardDurationMinutes - (DateTime.Now - playerData.LogTime).TotalMinutes;

        if (args.Parameters.Count == 0)
        {
            HelpCmd(args.Player);

            if (!playerData.AutoFishEnabled)
                args.Player.SendSuccessMessage("请输入该指令开启→: [c/92C5EC:/af fish]");

            //开启了消耗模式
            else if (playerData.ConsumptionEnabled)
                args.Player.SendMessage($"自动钓鱼[c/46C4D4:剩余时长]：[c/F3F292:{Math.Floor(remainingMinutes)}]分钟", 243, 181,
                    145);
            return;
        }

        if (HandlePlayerCommand(args, playerData, remainingMinutes))
            return;

        if (HandleAdminCommand(args))
            return;

        HelpCmd(args.Player);
    }

    /// <summary>
    ///     展示个人状态信息。
    /// </summary>
    private static void SendStatus(TSPlayer player, AFPlayerData.ItemData playerData, double remainingMinutes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[自动钓鱼个人状态]");
        sb.AppendLine($"功能：{(playerData.AutoFishEnabled ? "开启" : "关闭")}");
        sb.AppendLine($"BUFF：{(playerData.BuffEnabled ? "开启" : "关闭")}");
        sb.AppendLine($"多钩：{(playerData.MultiHookEnabled ? "开启" : "关闭")}, 钩子上限：{playerData.HookMaxNum}");
        sb.AppendLine($"过滤不可堆叠：{(playerData.SkipNonStackableLoot ? "开启" : "关闭")}");
        sb.AppendLine($"不钓怪物：{(playerData.BlockMonsterCatch ? "开启" : "关闭")}");
        sb.AppendLine($"跳过上鱼动画：{(playerData.SkipFishingAnimation ? "开启" : "关闭")}");

        if (AutoFish.Config.BaitItemIds.Any() || playerData.ConsumptionEnabled)
        {
            var minutesLeft = Math.Max(0, Math.Floor(remainingMinutes));
            var consumeLine = playerData.ConsumptionEnabled
                ? $"开启，剩余：{minutesLeft} 分钟"
                : "关闭";
            sb.AppendLine($"消耗模式：{consumeLine}");
        }

        player.SendInfoMessage(sb.ToString());
    }

    private static bool HandlePlayerCommand(CommandArgs args, AFPlayerData.ItemData playerData, double remainingMinutes)
    {
        var player = args.Player;
        var sub = args.Parameters[0].ToLower();

        if (args.Parameters.Count == 1)
        {
            switch (sub)
            {
                case "fish":
                    if (!AutoFish.HasFeaturePermission(player, "autofish.fish"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用自动钓鱼功能。");
                        return true;
                    }

                    var fishEnabled = playerData.AutoFishEnabled;
                    playerData.AutoFishEnabled = !fishEnabled;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{(fishEnabled ? "禁用" : "启用")}]自动钓鱼功能。");
                    return true;
                case "buff":
                    if (!AutoFish.HasFeaturePermission(player, "autofish.buff"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用自动钓鱼BUFF功能。");
                        return true;
                    }

                    var isEnabled = playerData.BuffEnabled;
                    playerData.BuffEnabled = !isEnabled;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{(isEnabled ? "禁用" : "启用")}]自动钓鱼BUFF");
                    return true;
                case "multi":
                    if (!AutoFish.HasFeaturePermission(player, "autofish.multihook"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用多钩功能。");
                        return true;
                    }

                    if (!AutoFish.Config.GlobalMultiHookFeatureEnabled)
                    {
                        args.Player.SendWarningMessage("多钩功能未在全局开启，无法切换。");
                        return true;
                    }

                    playerData.MultiHookEnabled = !playerData.MultiHookEnabled;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{(playerData.MultiHookEnabled ? "启用" : "禁用")}]多钩功能。");
                    return true;
                case "status":
                    SendStatus(args.Player, playerData, remainingMinutes);
                    return true;
                case "stack":
                    if (!AutoFish.Config.GlobalSkipNonStackableLoot)
                    {
                        args.Player.SendWarningMessage("过滤不可堆叠未在全局开启，无法切换。");
                        return true;
                    }

                    if (!AutoFish.HasFeaturePermission(player, "autofish.filter.unstackable"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用过滤不可堆叠功能。");
                        return true;
                    }

                    playerData.SkipNonStackableLoot = !playerData.SkipNonStackableLoot;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{(playerData.SkipNonStackableLoot ? "启用" : "禁用")}]过滤不可堆叠渔获。");
                    return true;
                case "monster":
                    if (!AutoFish.Config.GlobalBlockMonsterCatch)
                    {
                        args.Player.SendWarningMessage("不钓怪物未在全局开启，无法切换。");
                        return true;
                    }

                    if (!AutoFish.HasFeaturePermission(player, "autofish.filter.monster"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用不钓怪物功能。");
                        return true;
                    }

                    playerData.BlockMonsterCatch = !playerData.BlockMonsterCatch;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{(playerData.BlockMonsterCatch ? "启用" : "禁用")}]不钓怪物。");
                    return true;
                case "anim":
                    if (!AutoFish.Config.GlobalSkipFishingAnimation)
                    {
                        args.Player.SendWarningMessage("跳过上鱼动画未在全局开启，无法切换。");
                        return true;
                    }

                    if (!AutoFish.HasFeaturePermission(player, "autofish.skipanimation"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用跳过上鱼动画功能。");
                        return true;
                    }

                    playerData.SkipFishingAnimation = !playerData.SkipFishingAnimation;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{(playerData.SkipFishingAnimation ? "启用" : "禁用")}]跳过上鱼动画。");
                    return true;
                case "list" when AutoFish.Config.BaitItemIds.Any():
                    args.Player.SendInfoMessage("[指定消耗物品表]\n" + string.Join(", ",
                        AutoFish.Config.BaitItemIds.Select(x =>
                            TShock.Utils.GetItemById(x).Name + "([c/92C5EC:{0}])".SFormat(x))));
                    args.Player.SendSuccessMessage(
                        $"兑换规则为：每[c/F5F252:{AutoFish.Config.BaitConsumeCount}]个 => [c/92C5EC:{AutoFish.Config.RewardDurationMinutes}]分钟");
                    return true;
                case "loot" when AutoFish.Config.ExtraCatchItemIds.Any():
                    args.Player.SendInfoMessage("[额外渔获表]\n" + string.Join(", ",
                        AutoFish.Config.ExtraCatchItemIds.Select(x =>
                            TShock.Utils.GetItemById(x).Name + "([c/92C5EC:{0}])".SFormat(x))));
                    return true;
                default:
                    return false;
            }
        }

        if (args.Parameters.Count == 2)
        {
            switch (sub)
            {
                case "hook":
                    if (!AutoFish.HasFeaturePermission(args.Player, "autofish.multihook"))
                    {
                        args.Player.SendErrorMessage("你没有权限设置钩子上限。");
                        return true;
                    }

                    if (!AutoFish.Config.GlobalMultiHookFeatureEnabled)
                    {
                        args.Player.SendWarningMessage("多钩功能未在全局开启，无法设置钩子数量。");
                        return true;
                    }

                    if (!int.TryParse(args.Parameters[1], out var personalMax))
                    {
                        args.Player.SendErrorMessage("请输入数字，格式：/af hook 数字");
                        return true;
                    }

                    if (personalMax < 1)
                    {
                        args.Player.SendWarningMessage("钩子数量必须大于等于 1。");
                        return true;
                    }

                    if (personalMax > AutoFish.Config.GlobalMultiHookMaxNum)
                    {
                        args.Player.SendWarningMessage($"已限制为全局上限：{AutoFish.Config.GlobalMultiHookMaxNum}。");
                        personalMax = AutoFish.Config.GlobalMultiHookMaxNum;
                    }

                    playerData.HookMaxNum = personalMax;
                    args.Player.SendSuccessMessage($"已将个人钩子上限设置为：{personalMax} (全局上限 {AutoFish.Config.GlobalMultiHookMaxNum})");
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }

    private static bool HandleAdminCommand(CommandArgs args)
    {
        if (!args.Player.HasPermission("autofish.admin")) return false;

        var sub = args.Parameters[0].ToLower();

        if (args.Parameters.Count == 1)
        {
            switch (sub)
            {
                case "gmore":
                    var multiEnabled = AutoFish.Config.GlobalMultiHookFeatureEnabled;
                    AutoFish.Config.GlobalMultiHookFeatureEnabled = !multiEnabled;
                    var multiToggle = multiEnabled ? "禁用" : "启用";
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{multiToggle}]多线模式");
                    AutoFish.Config.Write();
                    return true;
                case "gbuff":
                    var buffCfgEnabled = AutoFish.Config.GlobalBuffFeatureEnabled;
                    AutoFish.Config.GlobalBuffFeatureEnabled = !buffCfgEnabled;
                    var buffToggleText = buffCfgEnabled ? "禁用" : "启用";
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{buffToggleText}]全局钓鱼BUFF");
                    AutoFish.Config.Write();
                    return true;
                case "gmod":
                    var modEnabled = AutoFish.Config.GlobalConsumptionModeEnabled;
                    AutoFish.Config.GlobalConsumptionModeEnabled = !modEnabled;
                    var modToggle = modEnabled ? "禁用" : "启用";
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{modToggle}]消耗模式");
                    AutoFish.Config.Write();
                    return true;
                case "gstack":
                    AutoFish.Config.GlobalSkipNonStackableLoot = !AutoFish.Config.GlobalSkipNonStackableLoot;
                    args.Player.SendSuccessMessage($"已[c/92C5EC:{(AutoFish.Config.GlobalSkipNonStackableLoot ? "启用" : "禁用")}]全局过滤不可堆叠渔获。");
                    AutoFish.Config.Write();
                    return true;
                case "gmonster":
                    AutoFish.Config.GlobalBlockMonsterCatch = !AutoFish.Config.GlobalBlockMonsterCatch;
                    args.Player.SendSuccessMessage($"已[c/92C5EC:{(AutoFish.Config.GlobalBlockMonsterCatch ? "启用" : "禁用")}]全局不钓怪物。");
                    AutoFish.Config.Write();
                    return true;
                case "gani":
                    AutoFish.Config.GlobalSkipFishingAnimation = !AutoFish.Config.GlobalSkipFishingAnimation;
                    args.Player.SendSuccessMessage($"已[c/92C5EC:{(AutoFish.Config.GlobalSkipFishingAnimation ? "启用" : "禁用")}]全局跳过上鱼动画。");
                    AutoFish.Config.Write();
                    return true;
                case "gset":
                    args.Player.SendInfoMessage($"当前消耗物品数量：{AutoFish.Config.BaitConsumeCount}，推荐：2");
                    return true;
                case "gtime":
                    args.Player.SendInfoMessage($"当前消耗自动时长：{AutoFish.Config.RewardDurationMinutes}，单位：分钟，推荐：30-45");
                    return true;
                default:
                    return false;
            }
        }

        if (args.Parameters.Count == 2)
        {
            var matchedItems = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
            if (matchedItems.Count > 1)
            {
                args.Player.SendMultipleMatchError(matchedItems.Select(i => i.Name));
                return true;
            }

            if (matchedItems.Count == 0)
            {
                args.Player.SendErrorMessage(
                    "不存在该物品，\"物品查询\": \"[c/92C5EC:https://terraria.wiki.gg/zh/wiki/Item_IDs]\"");
                return true;
            }

            var item = matchedItems[0];

            switch (sub)
            {
                case "gadd":
                    if (AutoFish.Config.BaitItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在指定鱼饵表中!", item.Name);
                        return true;
                    }

                    AutoFish.Config.BaitItemIds.Add(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功将物品添加指定鱼饵表: [c/92C5EC:{0}]!", item.Name);
                    return true;
                case "gdel":
                    if (!AutoFish.Config.BaitItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 {0} 不在指定鱼饵表中!", item.Name);
                        return true;
                    }

                    AutoFish.Config.BaitItemIds.Remove(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功从指定鱼饵表移出物品: [c/92C5EC:{0}]!", item.Name);
                    return true;
                case "gaddloot":
                    if (AutoFish.Config.ExtraCatchItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在额外渔获表中!", item.Name);
                        return true;
                    }

                    AutoFish.Config.ExtraCatchItemIds.Add(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功将物品添加额外渔获表: [c/92C5EC:{0}]!", item.Name);
                    return true;
                case "gdelloot":
                    if (!AutoFish.Config.ExtraCatchItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 {0} 不在额外渔获中!", item.Name);
                        return true;
                    }

                    AutoFish.Config.ExtraCatchItemIds.Remove(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功从额外渔获移出物品: [c/92C5EC:{0}]!", item.Name);
                    return true;
                case "gset":
                    if (int.TryParse(args.Parameters[1], out var consumeNum))
                    {
                        AutoFish.Config.BaitConsumeCount = consumeNum;
                        AutoFish.Config.Write();
                        args.Player.SendSuccessMessage("已成功将物品数量要求设置为: [c/92C5EC:{0}] 个!", consumeNum);
                    }

                    return true;
                case "gduo":
                    if (int.TryParse(args.Parameters[1], out var maxNum))
                    {
                        AutoFish.Config.GlobalMultiHookMaxNum = maxNum;
                        AutoFish.Config.Write();
                        args.Player.SendSuccessMessage("已成功将多钩数量上限设置为: [c/92C5EC:{0}] 个!", maxNum);
                    }

                    return true;
                case "gtime":
                    if (int.TryParse(args.Parameters[1], out var rewardMinutes))
                    {
                        AutoFish.Config.RewardDurationMinutes = rewardMinutes;
                        AutoFish.Config.Write();
                        args.Player.SendSuccessMessage("已成功将自动时长设置为: [c/92C5EC:{0}] 分钟!", rewardMinutes);
                    }

                    return true;
                default:
                    HelpCmd(args.Player);
                    return true;
            }
        }

        return false;
    }
}