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
        if (player == null)
        {
        }
        else
        {
            //普通玩家
            if (!player.HasPermission("autofish.admin"))
            {
                var helpMessage = new StringBuilder();
                helpMessage.AppendFormat("          [i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");


                helpMessage.AppendFormat("\n/af -- 查看自动钓鱼菜单\n" +
                                         "/af status -- 查看个人状态\n" +
                                         "/af on -- 自动钓鱼[c/4686D4:开启]功能\n" +
                                         "/af off -- 自动钓鱼[c/F25055:关闭]功能\n" +
                                         "/af buff -- 开启丨关闭[c/F6B152:钓鱼BUFF]");

                if (AutoFish.Config.ExtraCatchItemIds.Any())
                    helpMessage.AppendFormat("\n/af loot -- 查看[c/F25055:额外渔获表]");

                if (AutoFish.Config.ConsumptionModeEnabled)
                    helpMessage.AppendFormat("\n/af list -- 列出消耗模式[c/F5F251:指定物品表]");

                player.SendMessage(helpMessage.ToString(), 193, 223, 186);
            }

            //管理员
            else
            {
                var helpMessage = new StringBuilder();
                helpMessage.AppendFormat("          [i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");

                helpMessage.AppendFormat("\n[个人] /af status -- 查看个人状态\n" +
                                         "[个人] /af on 或 off -- 自动钓鱼[c/4686D4:开启]|[c/F25055:关闭]功能\n" +
                                         "[个人] /af buff -- 开启丨关闭[c/F6B152:钓鱼BUFF]");

                if (AutoFish.Config.ConsumptionModeEnabled)
                    helpMessage.AppendFormat("\n[个人] /af list -- 列出消耗[c/F5F251:指定物品表]");

                helpMessage.AppendFormat("\n[个人] /af loot -- 查看[c/F25055:额外渔获表]");

                // 全局配置放在末尾并以 g 前缀标识
                helpMessage.AppendFormat("\n[全局] /af gbuff -- 开启丨关闭全局钓鱼BUFF\n" +
                                         "[全局] /af gmore -- 开启丨关闭多线模式\n" +
                                         "[全局] /af gduo 数字 -- 设置多线的钩子数量上限\n" +
                                         "[全局] /af gmod -- 开启丨关闭消耗模式\n" +
                                         "[全局] /af gset 数量 -- 设置消耗物品数量要求\n" +
                                         "[全局] /af gtime 数字 -- 设置自动时长(分钟)\n" +
                                         "[全局] /af gadd 物品名 -- 添加指定鱼饵\n" +
                                         "[全局] /af gdel 物品名 -- 移除指定鱼饵\n" +
                                         "[全局] /af gaddloot 物品名 -- 添加额外渔获\n" +
                                         "[全局] /af gdelloot 物品名 -- 移除额外渔获");

                player.SendMessage(helpMessage.ToString(), 193, 223, 186);
            }
        }
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
                args.Player.SendSuccessMessage("请输入该指令开启→: [c/92C5EC:/af on]");

            //开启了消耗模式
            else if (AutoFish.Config.ConsumptionModeEnabled)
                args.Player.SendMessage($"自动钓鱼[c/46C4D4:剩余时长]：[c/F3F292:{Math.Floor(remainingMinutes)}]分钟", 243, 181,
                    145);

            //检测到血月
            if (Main.bloodMoon)
                args.Player.SendMessage("当前为[c/F25055:血月]无法钓上怪物，可[c/46C4D4:关闭]插件：[c/F3F292:/af off]", 243, 181,
                    145);

            return;
        }

        if (args.Parameters.Count == 1)
        {
            var sub = args.Parameters[0].ToLower();
            switch (sub)
            {
                case "on":
                    if (!AutoFish.HasFeaturePermission(player, "autofish.fish"))
                    {
                        args.Player.SendErrorMessage("你没有权限开启自动钓鱼。");
                        return;
                    }

                    playerData.AutoFishEnabled = true;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:启用]自动钓鱼功能。");
                    return;
                case "off":
                    playerData.AutoFishEnabled = false;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:禁用]自动钓鱼功能。");
                    return;
                case "buff":
                    if (!AutoFish.HasFeaturePermission(player, "autofish.buff"))
                    {
                        args.Player.SendErrorMessage("你没有权限使用自动钓鱼BUFF功能。");
                        return;
                    }

                    var isEnabled = playerData.BuffEnabled;
                    playerData.BuffEnabled = !isEnabled;
                    args.Player.SendSuccessMessage(
                        $"玩家 [{args.Player.Name}] 已[c/92C5EC:{(isEnabled ? "禁用" : "启用")}]自动钓鱼BUFF");
                    return;
                case "status":
                    SendStatus(args.Player, playerData, remainingMinutes);
                    return;
                case "list" when AutoFish.Config.ConsumptionModeEnabled:
                    args.Player.SendInfoMessage("[指定消耗物品表]\n" + string.Join(", ",
                        AutoFish.Config.BaitItemIds.Select(x =>
                            TShock.Utils.GetItemById(x).Name + "([c/92C5EC:{0}])".SFormat(x))));
                    args.Player.SendSuccessMessage(
                        $"兑换规则为：每[c/F5F252:{AutoFish.Config.BaitConsumeCount}]个 => [c/92C5EC:{AutoFish.Config.RewardDurationMinutes}]分钟");
                    return;
                case "loot" when AutoFish.Config.ExtraCatchItemIds.Any():
                    args.Player.SendInfoMessage("[额外渔获表]\n" + string.Join(", ",
                        AutoFish.Config.ExtraCatchItemIds.Select(x =>
                            TShock.Utils.GetItemById(x).Name + "([c/92C5EC:{0}])".SFormat(x))));
                    return;
            }

            if (player.HasPermission("autofish.admin"))
            {
                switch (sub)
                {
                    case "gmore":
                        var multiEnabled = AutoFish.Config.MultiHookEnabled;
                        AutoFish.Config.MultiHookEnabled = !multiEnabled;
                        var multiToggle = multiEnabled ? "禁用" : "启用";
                        args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{multiToggle}]多线模式");
                        AutoFish.Config.Write();
                        playerData.MultiHookEnabled = AutoFish.Config.MultiHookEnabled;
                        return;
                    case "gbuff":
                        var buffCfgEnabled = AutoFish.Config.GlobalBuffEnabled;
                        AutoFish.Config.GlobalBuffEnabled = !buffCfgEnabled;
                        var buffToggleText = buffCfgEnabled ? "禁用" : "启用";
                        args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{buffToggleText}]全局钓鱼BUFF");
                        AutoFish.Config.Write();
                        return;
                    case "gmod":
                        var modEnabled = AutoFish.Config.ConsumptionModeEnabled;
                        AutoFish.Config.ConsumptionModeEnabled = !modEnabled;
                        var modToggle = modEnabled ? "禁用" : "启用";
                        args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{modToggle}]消耗模式");
                        AutoFish.Config.Write();
                        return;
                    case "gset":
                        args.Player.SendInfoMessage($"当前消耗物品数量：{AutoFish.Config.BaitConsumeCount}，推荐：2");
                        return;
                    case "gtime":
                        args.Player.SendInfoMessage($"当前消耗自动时长：{AutoFish.Config.RewardDurationMinutes}，单位：分钟，推荐：30-45");
                        return;
                }
            }
        }

        //管理权限
        if (player.HasPermission("autofish.admin") && args.Parameters.Count == 2)
        {
            Item item;
            var matchedItems = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
            if (matchedItems.Count > 1)
            {
                args.Player.SendMultipleMatchError(matchedItems.Select(i => i.Name));
                return;
            }

            if (matchedItems.Count == 0)
            {
                args.Player.SendErrorMessage(
                    "不存在该物品，\"物品查询\": \"[c/92C5EC:https://terraria.wiki.gg/zh/wiki/Item_IDs]\"");
                return;
            }

            item = matchedItems[0];

            switch (args.Parameters[0].ToLower())
            {
                case "gadd":
                {
                    if (AutoFish.Config.BaitItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在指定鱼饵表中!", item.Name);
                        return;
                    }

                    AutoFish.Config.BaitItemIds.Add(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功将物品添加指定鱼饵表: [c/92C5EC:{0}]!", item.Name);
                    break;
                }

                case "gdel":
                {
                    if (!AutoFish.Config.BaitItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 {0} 不在指定鱼饵表中!", item.Name);
                        return;
                    }

                    AutoFish.Config.BaitItemIds.Remove(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功从指定鱼饵表移出物品: [c/92C5EC:{0}]!", item.Name);
                    break;
                }

                case "gaddloot":
                {
                    if (AutoFish.Config.ExtraCatchItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在额外渔获表中!", item.Name);
                        return;
                    }

                    AutoFish.Config.ExtraCatchItemIds.Add(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功将物品添加额外渔获表: [c/92C5EC:{0}]!", item.Name);
                    break;
                }

                case "gdelloot":
                {
                    if (!AutoFish.Config.ExtraCatchItemIds.Contains(item.type))
                    {
                        args.Player.SendErrorMessage("物品 {0} 不在额外渔获中!", item.Name);
                        return;
                    }

                    AutoFish.Config.ExtraCatchItemIds.Remove(item.type);
                    AutoFish.Config.Write();
                    args.Player.SendSuccessMessage("已成功从额外渔获移出物品: [c/92C5EC:{0}]!", item.Name);
                    break;
                }

                case "gset":
                {
                    if (int.TryParse(args.Parameters[1], out var num))
                    {
                        AutoFish.Config.BaitConsumeCount = num;
                        AutoFish.Config.Write();
                        args.Player.SendSuccessMessage("已成功将物品数量要求设置为: [c/92C5EC:{0}] 个!", num);
                    }

                    break;
                }

                case "gduo":
                {
                    if (int.TryParse(args.Parameters[1], out var num))
                    {
                        AutoFish.Config.MultiHookMaxNum = num;
                        AutoFish.Config.Write();
                        args.Player.SendSuccessMessage("已成功将多钩数量上限设置为: [c/92C5EC:{0}] 个!", num);
                    }

                    break;
                }

                case "gtime":
                {
                    if (int.TryParse(args.Parameters[1], out var num))
                    {
                        AutoFish.Config.RewardDurationMinutes = num;
                        AutoFish.Config.Write();
                        args.Player.SendSuccessMessage("已成功将自动时长设置为: [c/92C5EC:{0}] 分钟!", num);
                    }

                    break;
                }

                default:
                {
                    HelpCmd(args.Player);
                    break;
                }
            }
        }
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

        if (AutoFish.Config.ConsumptionModeEnabled)
        {
            var minutesLeft = Math.Max(0, Math.Floor(remainingMinutes));
            var consumeLine = playerData.ConsumptionEnabled
                ? $"开启，剩余：{minutesLeft} 分钟"
                : "关闭";
            sb.AppendLine($"消耗模式：{consumeLine}");
        }

        player.SendInfoMessage(sb.ToString());
    }
}