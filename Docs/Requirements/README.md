# 需求文档索引

状态：讨论稿 v0.1

本目录描述我们准备制作什么、为什么制作、如何判断完成。尚未确认的内容不会因为写入文档就自动成为需求。

## 阅读顺序

1. [GAME_BRIEF.md](GAME_BRIEF.md)：一页产品摘要与关键开放问题。
2. [CORE_CONCEPT.md](CORE_CONCEPT.md)：已确认的巨构维修队核心概念。
3. [WORLD_THEME.md](WORLD_THEME.md)：已确认的风暴荒原主背景与未来主题边界。
4. [PRODUCT_REQUIREMENTS.md](PRODUCT_REQUIREMENTS.md)：产品目标、玩家、体验支柱、范围与成功标准。
5. [GAMEPLAY_REQUIREMENTS.md](GAMEPLAY_REQUIREMENTS.md)：游戏模式、单局循环、协作、谜题、失败与成长。
6. [RUN_AND_CAMPAIGN_STRUCTURE.md](RUN_AND_CAMPAIGN_STRUCTURE.md)：单局阶段、节奏、主线寿命和剧情驱动。
7. [NARRATIVE_REQUIREMENTS.md](NARRATIVE_REQUIREMENTS.md)：已确认的使命、核心谜团和叙事表现边界。
8. [COMBAT_REQUIREMENTS.md](COMBAT_REQUIREMENTS.md)：维修工具防卫、小型敌人、章节精英和最终 Boss 边界。
9. [PLAYER_INTERACTION_REQUIREMENTS.md](PLAYER_INTERACTION_REQUIREMENTS.md)：第一人称、简化输入、移动与搬运规则。
10. [TOOLS_AND_ITEMS_REQUIREMENTS.md](TOOLS_AND_ITEMS_REQUIREMENTS.md)：三类核心工具、人数适配、携带与反馈边界。
11. [COOP_AND_SYSTEM_PUZZLES.md](COOP_AND_SYSTEM_PUZZLES.md)：系统故障链、事故教学、协作层次与人数适配。
12. [PLAYER_STATE_AND_RESCUE.md](PLAYER_STATE_AND_RESCUE.md)：受伤、倒地、救援、恢复与退出规则。
13. [PROGRESSION_REQUIREMENTS.md](PROGRESSION_REQUIREMENTS.md)：局内、局外成长最小用例与个人成长后续边界。
14. [SYSTEM_REQUIREMENTS.md](SYSTEM_REQUIREMENTS.md)：功能系统清单、数据关系和阶段优先级。
15. [TECHNICAL_REQUIREMENTS.md](TECHNICAL_REQUIREMENTS.md)：联机、性能、可维护性、存档和构建等非功能需求。
16. [CONTENT_AND_ART.md](CONTENT_AND_ART.md)：3D 表现、资产预算、可读性和内容生产规则。
17. [MVP_AND_ACCEPTANCE.md](MVP_AND_ACCEPTANCE.md)：原型、MVP 边界、验收和完成定义。
18. [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md)：需要项目推动者明确选择的问题与决策顺序。

## 状态词

| 状态 | 含义 |
| --- | --- |
| 已确认 | 用户已明确提出，或双方已正式接受 |
| 建议 | 当前工程/设计建议，可在评审中修改 |
| 待决策 | 不能安全假定，需要讨论或原型验证 |
| 暂不做 | 不属于当前 MVP，但不代表永久拒绝 |
| 明确不做 | 与当前产品方向冲突，除非重新立项 |

## 优先级

- **MUST**：缺失则该阶段不可交付。
- **SHOULD**：有重要价值，但可在风险出现时降级。
- **COULD**：资源允许时加入。
- **LATER**：保留方向，不进入当前计划。

## 变更规则

- 需求修改必须说明原因与受影响的系统。
- 影响玩法方向、网络拓扑、存档兼容或范围的决定需要 ADR。
- 文档通过评审后才进入原型实现；实现不应悄悄改变需求。
- 测试、任务和提交应尽量引用需求编号。
