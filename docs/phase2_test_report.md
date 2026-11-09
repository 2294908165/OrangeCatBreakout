# Phase 2 测试报告

验证日期为 2026-11-07，使用本机 Windows Unity 6.3 LTS 6000.3.28f1。交互编辑器已打开主工程，因此用同一份 Assets、Packages 和 ProjectSettings 建立隔离副本运行 batchmode。同步回主工程前核对脚本字节和已有 GUID，交付的是 Unity 验证过的场景、Prefab、数据和层设置。

| 执行项目 | 实际结果 | 证据 |
| --- | --- | --- |
| 初次编译并执行 CombatTestBuilder.Build | 退出码 0，生成场景、两个 Prefab 和数据 | TestResults/Phase2/Build.log |
| 对已存在场景再次执行构建 | 退出码 0，保留 PhaseOne 的 Build Settings 项 | TestResults/Phase2/Rebuild.log |
| EditMode | 2 通过，0 失败 | TestResults/Phase2/EditMode.xml |
| PlayMode | 24 通过，0 失败。包含 T01–T23 及原有 PhaseOne 运动回归 | TestResults/Phase2/PlayMode.xml |

最终编译及测试日志未出现 C# 编译错误或警告。PlayMode 加载了实际 CombatTest 场景。判定、物理步进、角色切换、暂停和场景重载均走工程组件。大部分边界测试在初始落地后直接调用生产固定步入口，T12 用实际训练装置的攻击范围触发完美格挡。T20 使用正常场景重载。T23 通过 Input System 虚拟键盘、Update 和 FixedUpdate 验证 J 键单次攻击。

| 用例 | 覆盖 |
| --- | --- |
| T01–T04 | 单次去重、多目标、多 Hurtbox、前摇和后摇排除 |
| T05–T07 | 二段独立 ID、需要二次按键、后摇翻滚取消 |
| T08–T09 | 翻滚窗口内外的伤害结果 |
| T10–T13 | 正面小数减伤、背击、完美格挡与装置中断、长按窗口到期 |
| T14–T15 | 反击 3 点伤害、消耗、超时恢复普攻 |
| T16–T20 | 死亡事件一次、清理旧攻击、换角隔离、受击恢复、暂停和重载 |
| T21–T23 | 单步跨有效窗、向左判定与木桩重置、Input System J 键路径 |
| PhaseOne 回归 | 真实落地、二段跳次数、翻滚速度和无敌、实体 Collider 保持启用 |

中途 T11 因测试暂停了物理却要求击退后重新接地而失败，改用显式 2D 物理步进验证。新增 T23 首次在无焦点批处理下收不到虚拟键盘事件，测试内临时将输入路由至 Game 视图并在结束时复原，生产输入配置沿用原值。以上用例均已在重复构建后的最终一轮通过。

## 待人工验收

批处理采用 nographics。画面、角色手感、真实键盘和手柄体验仍需在 Game 视图验收。现有 PhaseOne 回归没有单独量测土狼时间、跳跃缓冲和可变跳高的完整运动曲线。未执行独立 Windows Player 打包。命中停顿与音效留到手感调优阶段。
