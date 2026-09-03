using FunGame.Incident;
using FunGame.Tools;

namespace FunGame.Demo
{
    /// <summary>
    /// 导航目标的语义类型。表现层把类型映射到当前场景实例，不把场景名称写进规则。
    /// </summary>
    public enum DemoGuidanceTargetKind
    {
        None = 0,
        PressureGauge = 1,
        PumpInspection = 2,
        CircuitBridgerRack = 3,
        CircuitInterlock = 4,
        SealantRack = 5,
        Leak = 6,
        ImpactWrenchRack = 7,
        Enemy = 8,
        Fastener = 9,
        ReplacementPipe = 10,
        CoolingConsole = 11,
        Relay = 12,
        CampaignConsole = 13,
        SecretPlate = 14
    }

    public readonly struct DemoGuidanceInstruction
    {
        public DemoGuidanceInstruction(
            DemoGuidanceTargetKind primaryTarget,
            string actionText,
            DemoGuidanceTargetKind secondaryTarget = DemoGuidanceTargetKind.None,
            string secondaryText = "")
        {
            PrimaryTarget = primaryTarget;
            ActionText = actionText;
            SecondaryTarget = secondaryTarget;
            SecondaryText = secondaryText;
        }

        public DemoGuidanceTargetKind PrimaryTarget { get; }
        public string ActionText { get; }
        public DemoGuidanceTargetKind SecondaryTarget { get; }
        public string SecondaryText { get; }
    }

    /// <summary>
    /// 把章节状态转换成明确的“去哪—拿什么—按哪个键”指令。
    /// </summary>
    public static class DemoGuidanceRules
    {
        public static DemoGuidanceInstruction ResolveCooling(
            CoolingIncidentRunState runState,
            CoolingIncidentPhase phase,
            bool pressureInspected,
            bool pumpInspected,
            ToolKind equippedTool,
            bool isHoldingReplacement,
            bool combatActive)
        {
            if (runState == CoolingIncidentRunState.Failed)
            {
                return new DemoGuidanceInstruction(
                    DemoGuidanceTargetKind.CoolingConsole,
                    "事故已失控：跟随橙色标记前往冷却控制台，准星对准后按 E 重新开始。 ");
            }

            if (runState == CoolingIncidentRunState.Succeeded)
            {
                return new DemoGuidanceInstruction(DemoGuidanceTargetKind.None, "支路已恢复，正在切换下一项任务。 ");
            }

            if (combatActive)
            {
                return equippedTool == ToolKind.ImpactWrench
                    ? new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.Enemy,
                        "设备遭到干扰：靠近紫色干扰体，用准星瞄准并点击左键击退。 ")
                    : new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.ImpactWrenchRack,
                        "设备遭到干扰：先跟随标记到工具架，按 E 装备冲击扳手。 ",
                        DemoGuidanceTargetKind.Enemy,
                        "紫色标记是正在攻击设备的干扰体");
            }

            switch (phase)
            {
                case CoolingIncidentPhase.AssessSymptoms:
                    if (!pressureInspected)
                    {
                        return new DemoGuidanceInstruction(
                            DemoGuidanceTargetKind.PressureGauge,
                            "第一步：跟随橙色标记找到圆形压力表，准星对准后按 E 读取压力。 ");
                    }

                    return !pumpInspected
                        ? new DemoGuidanceInstruction(
                            DemoGuidanceTargetKind.PumpInspection,
                            "第二步：前往冷却泵的发光检查面板，准星对准后按 E 检查振动。 ")
                        : new DemoGuidanceInstruction(DemoGuidanceTargetKind.None, "诊断完成，等待控制系统更新。 ");
                case CoolingIncidentPhase.RestoreControlPower:
                    return RequireTool(
                        equippedTool,
                        ToolKind.CircuitBridger,
                        DemoGuidanceTargetKind.CircuitBridgerRack,
                        DemoGuidanceTargetKind.CircuitInterlock,
                        "前往紫色联锁箱，保持准星对准并点击左键三次完成桥接。 ");
                case CoolingIncidentPhase.ContainLeak:
                    return RequireTool(
                        equippedTool,
                        ToolKind.SealantGun,
                        DemoGuidanceTargetKind.SealantRack,
                        DemoGuidanceTargetKind.Leak,
                        "前往喷出蓝光的泄漏管段，保持准星对准并按住左键完成密封。 ");
                case CoolingIncidentPhase.LoosenConnection:
                    return RequireTool(
                        equippedTool,
                        ToolKind.ImpactWrench,
                        DemoGuidanceTargetKind.ImpactWrenchRack,
                        DemoGuidanceTargetKind.Fastener,
                        "前往带六角螺栓的损坏接头，准星对准后点击左键松开。 ");
                case CoolingIncidentPhase.InstallReplacementPipe:
                    return isHoldingReplacement
                        ? new DemoGuidanceInstruction(
                            DemoGuidanceTargetKind.Fastener,
                            "把手中的替换管带到损坏接头，准星对准后按 E 安装。 ")
                        : new DemoGuidanceInstruction(
                            DemoGuidanceTargetKind.ReplacementPipe,
                            "前往黄色双环替换管，准星对准后按 E 拿起；拿错方向可按 Q 放下。 ");
                case CoolingIncidentPhase.TightenConnection:
                    return RequireTool(
                        equippedTool,
                        ToolKind.ImpactWrench,
                        DemoGuidanceTargetKind.ImpactWrenchRack,
                        DemoGuidanceTargetKind.Fastener,
                        "回到机械接头，准星对准六角螺栓并点击左键紧固。 ");
                case CoolingIncidentPhase.VerifyPressure:
                    return new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.PressureGauge,
                        "返回圆形压力表，准星对准后按 E 验证压力回升。 ");
                case CoolingIncidentPhase.ResetPump:
                    return new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.CoolingConsole,
                        "前往橙色冷却控制台，准星对准后按 E 复位冷却泵。 ");
                default:
                    return new DemoGuidanceInstruction(DemoGuidanceTargetKind.None, "冷却支路已稳定。 ");
            }
        }

        public static DemoGuidanceInstruction ResolveRelay(
            bool chapterFailed,
            int remainingRelays,
            int remainingEnemies,
            ToolKind equippedTool)
        {
            if (chapterFailed)
            {
                return new DemoGuidanceInstruction(
                    DemoGuidanceTargetKind.CampaignConsole,
                    "继电器防卫失败：前往风暴控制台，按 E 重启本章。 ");
            }

            if (remainingRelays > 0)
            {
                return equippedTool == ToolKind.CircuitBridger
                    ? new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.Relay,
                        $"稳定剩余 {remainingRelays} 座继电器：对准紫色线圈并点击左键三次。 ",
                        remainingEnemies > 0 ? DemoGuidanceTargetKind.Enemy : DemoGuidanceTargetKind.None,
                        remainingEnemies > 0 ? $"同时留意 {remainingEnemies} 个正在接近设备的干扰体" : "")
                    : new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.CircuitBridgerRack,
                        "先到工具架按 E 装备线路桥接器，再依次处理紫色继电器。 ",
                        remainingEnemies > 0 ? DemoGuidanceTargetKind.Enemy : DemoGuidanceTargetKind.None,
                        remainingEnemies > 0 ? $"同时留意 {remainingEnemies} 个正在接近设备的干扰体" : "");
            }

            if (remainingEnemies > 0)
            {
                return equippedTool == ToolKind.ImpactWrench
                    ? new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.Enemy,
                        $"继电器已全部稳定：用冲击扳手清除剩余 {remainingEnemies} 个干扰体。 ")
                    : new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.ImpactWrenchRack,
                        "继电器已稳定：到工具架按 E 换取冲击扳手，再清除干扰体。 ",
                        DemoGuidanceTargetKind.Enemy,
                        $"剩余干扰体 {remainingEnemies} 个");
            }

            return new DemoGuidanceInstruction(DemoGuidanceTargetKind.None, "继电器与防卫均已完成，正在进入核心校准。 ");
        }

        public static DemoGuidanceInstruction ResolveStorm(
            bool chapterFailed,
            bool awaitingCalibration,
            int remainingEnemies,
            ToolKind equippedTool,
            bool campaignCompleted)
        {
            if (campaignCompleted)
            {
                return new DemoGuidanceInstruction(
                    DemoGuidanceTargetKind.SecretPlate,
                    "主线已完成。你可以退出，也可以寻找舱内积灰的旧维修铭牌。 ");
            }

            if (chapterFailed)
            {
                return new DemoGuidanceInstruction(
                    DemoGuidanceTargetKind.CampaignConsole,
                    "核心防卫失败：前往风暴控制台，按 E 重启第三章。 ");
            }

            if (awaitingCalibration)
            {
                return new DemoGuidanceInstruction(
                    DemoGuidanceTargetKind.CampaignConsole,
                    "本波已清除：返回风暴控制台，按 E 写入校准并启动下一波。 ");
            }

            if (remainingEnemies > 0)
            {
                return equippedTool == ToolKind.ImpactWrench
                    ? new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.Enemy,
                        $"保护风暴核心：瞄准并点击左键击退剩余 {remainingEnemies} 个干扰体。 ")
                    : new DemoGuidanceInstruction(
                        DemoGuidanceTargetKind.ImpactWrenchRack,
                        "前往工具架按 E 装备冲击扳手，随后保护风暴核心。 ",
                        DemoGuidanceTargetKind.Enemy,
                        $"剩余干扰体 {remainingEnemies} 个");
            }

            return new DemoGuidanceInstruction(DemoGuidanceTargetKind.None, "等待风暴核心确认本波状态。 ");
        }

        private static DemoGuidanceInstruction RequireTool(
            ToolKind equippedTool,
            ToolKind requiredTool,
            DemoGuidanceTargetKind rackTarget,
            DemoGuidanceTargetKind actionTarget,
            string actionText)
        {
            return equippedTool == requiredTool
                ? new DemoGuidanceInstruction(actionTarget, actionText)
                : new DemoGuidanceInstruction(
                    rackTarget,
                    $"先跟随标记到工具架，准星对准后按 E 装备{requiredTool.GetDisplayName()}。 ");
        }
    }
}
