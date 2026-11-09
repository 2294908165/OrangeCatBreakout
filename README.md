# 橘猫突围：最后一罐

**从标题界面开始**：在 Unity 打开 `Assets/Game/Scenes/TitleScreen.unity`，点 Play，再点「开始游戏」进入第一关。标题界面支持 Enter、空格和手柄南键，M 切换音乐。详见 `docs/title_screen.md`。

**完整试玩路线**从 `Assets/Game/Scenes/Warehouse01.unity` 开始。第一、二、四关通关后按 **E** 续关。第三关乘货梯到顶后自动进入第四关。第四关在屋顶给两处电闸通电，撤离后进入 **第五关·罐头守卫**。击败守卫并拿到最后一罐即通关。详见 `docs/rooftop04.md` 和 `docs/can_guard05.md`。

**第三关·装卸升降台**可从第二关通关后按 **E** 进入，或直接打开 `Assets/Game/Scenes/LoadingBay03.unity`。跨过地刺后会遇到笑肚怪。在右侧按钮旁按 **E** 呼叫货梯，登梯撤离。见 `docs/loading_bay03.md`。

**第二关·冷库输送线**已加入工程。第一关拿到罐头后按 **E**，或直接打开 `Assets/Game/Scenes/ColdStorage02.unity`。在输送带上应对飞虫俯冲，击败它后从装卸口离开。见 `docs/cold_storage02.md`。

**第一关·雨夜仓库**已加入工程。打开 `Assets/Game/Scenes/Warehouse01.unity`，按 Play。通过蒸汽井盖、地刺和工业风轮，击败流浪老鼠，穿过开启的门拿走最后一罐。见 `docs/warehouse01.md`。

**像素风试玩**已加入工程。打开 `Assets/Game/Scenes/PixelPreview.unity` 并按 Play，可试玩像素版哈橘和姜爪。构建方式和素材来源见 `docs/pixel_preview.md`。

**Phase 2 战斗测试**已加入工程。打开 `Assets/Game/Scenes/CombatTest.unity` 并按 Play。HUD 可换角、重置目标及重启。详细操作、参数、测试报告和下一阶段接口见 `docs/phase2_implementation.md`、`docs/phase2_tuning.md`、`docs/phase2_test_report.md` 与 `docs/phase3_handoff.md`。以下为 PhaseOne 场景说明。

## Phase 0 / Phase 1 原型记录

PhaseOne 原型在 Unity 6.3 LTS 6000.3.28f1 完成导入、编译和场景构建，首轮 EditMode 2/2、PlayMode 1/1 通过。以下操作和功能说明适用于 PhaseOne 场景。

## 安装和启动

1. Unity Hub → Add project from disk，选择本目录，用 Unity **6.3 LTS** 打开。当前验证版本为 6000.3.28f1。
2. 等待 Package Manager 导入 Input System 1.20.1、Test Framework 1.6.0；若联网解析失败，先解决包下载。已包含实际解析生成的 packages-lock.json。
3. 接受启用新 Input System 的 Editor 重启提示。工具也会写入 Active Input Handling = Input System Package。若未生效，在 Project Settings → Player 手动设定后重启。
4. 第一次编译完成后自动创建 `Assets/Game/Scenes/PhaseOne.unity`。若自动构建未发生，执行菜单 **Orange Cat → Build Phase 0-1 Scene**。
5. 打开 PhaseOne，点击 Play。A/D 或左右方向键移动，Space 跳跃，Left Shift 翻滚，Esc 暂停，F1 切换 HUD。手柄左摇杆移动，South 跳跃、East 翻滚、Start 暂停。
6. J / 手柄 West（攻击）、K / Left Shoulder（格挡）、E / North（交互）只配置输入，当前不驱动动作。切换测试角色时删除场景中的 HaoJu，再放入 JiangZhua Prefab，并将 GameManager 的 input、CameraFollow2D 的 target 改指新角色；单角色测试，不能同时放两名玩家。

工具建立 GameManager、相机及跟随引用、角色数据、输入引用、HUD 引用、地面和两块跳跃平台。两个 Prefab 均包含完整角色引用，使用相同初始参数、不同名称和占位颜色。支持重复执行：场景已存在时只打开它，不覆盖场景、Prefab 或角色数据。需要重新生成时先自行备份并移动旧 PhaseOne 场景。已有配置和资产不会被默认重建覆盖。

## 目录与职责

- `Assets/Game/Scripts/Input`：InputActionAsset 的独立实例；Update 采样，按键边沿排队至 FixedUpdate 消费，暂停清空。
- `Scripts/Player/PlayerMotor2D`：接地投射、水平加减速、朝向、跳跃和物理速度。只在 FixedUpdate 驱动物理；公开 Simulate 是测试入口，外部不应同时驱动。
- `Scripts/Player/PlayerStateMachine`：纯 C# FSM，维护状态和状态时钟；Animator 不参与状态决策。
- `CharacterDefinition`：角色可调数值；`AttackDefinition`：Phase 2 参数契约，尚无执行器。
- `Scripts/Core`：暂停管理、相机；角色 HUD 为独立组件。
- `Editor`：层、输入模式及场景/占位资产生成工具。
- `Scenes / Prefabs / Data / Art / Tests`：场景、预制体、配置、占位图和测试分别管理；场景和 Prefab 已由 Editor 实际生成。

## 状态规则

| 当前 | 目标 | 条件 |
|---|---|---|
| Free | Rolling | 本物理帧收到翻滚，且接地；方向在进入时锁定 |
| Rolling | Free | 状态经过时间达到 0.42 秒 |
| 任意非 Dead | Hurt / Dead | FSM 扩展接口；当前没有伤害调用者 |
| Free | Attacking / Guarding | FSM 扩展接口；当前未连接输入 |
| Attacking / Guarding / Hurt | Free | 未来执行器显式请求；当前无自动退出计时 |
| Dead | 任意 | 拒绝；重生应创建新角色实例 |

非 Free 状态不接受跳跃及普通水平移动；Rolling 以 9 units/s 锁定水平速度，仍受重力与地面实体碰撞影响。无敌为 `[0.10,0.30)` 秒逻辑属性，当前尚无伤害系统消费它；不会关闭身体碰撞器。时间按固定物理帧采样，默认步长 .02 秒，不应依赖渲染帧判断无敌边界。

接地仅接受 Ground 层、非 Trigger、向上法线 > .65，且上升速度 <= .1；身体向下投射 .06。接地重置跳跃计数；离地后 .10 秒仍可地面跳；超时消耗地面跳额度，保留一次空中跳。按下缓冲 .12 秒，条件允许时执行；二段跳立即重置竖直速度。松开跳跃仅截断一次上升速度，下降提高重力。可调参数均在 CharacterDefinition。

层：Ground、Player、Enemy、PlayerHitbox、EnemyHitbox、Hazard。工具从空闲 User Layer 分配，不能依赖固定数字。PlayerHitbox 仅与 Enemy 交互，EnemyHitbox 仅与 Player 交互，双方身体不互撞；Hazard 暂保留默认矩阵，Phase 2 再按触发器协议落实。没有提前创建空 Hitbox/Hurtbox 行为。

## 测试与验收

Window → General → Test Runner：分别 Run All EditMode / PlayMode。运行 PlayMode 前必须先构建场景，使 PhaseOne 出现在 Build Settings。测试会替换当前场景，先保存工作。

- EditMode：无敌窗口四个边界、翻滚和跳跃时间配置、FSM 终态/中断/计时重置。
- PlayMode：加载生成场景、真实物理落地，然后暂停自动物理时钟，通过生产 Simulate 入口检查翻滚速度/无敌/身体碰撞器、翻滚结束、最多二次跳跃。后半段是确定性控制器 tick，并非完整交互式运动测试。
- 手动验收：左右加速和停步、两种角色朝向、长按与短按跳跃高度、二段跳上平台、平台边缘 .10 秒宽限、落地前 .12 秒缓冲、空中翻滚被拒绝、翻滚撞墙/走出平台、暂停期间输入清空、F1 HUD 与手柄。
- 实际测试结果见 VALIDATION.md 和 TestResults 下的 XML 及日志。

## Phase 2 具体待办

1. 实现独立 Hitbox / Hurtbox、阵营过滤、单次攻击去重、伤害上下文和生命值；在 Hurtbox 入口读取 Invincible，保留身体碰撞。
2. 使用 AttackDefinition 实现前摇/生效/后摇，接入 Attacking 状态和输入，明确空中攻击及翻滚取消策略。
3. 接入 Guarding：朝向判断、格挡窗口、反击窗口和反馈；配置伤害/硬直数据。
4. 实现 Hurt / Dead 的受击、击退、退出条件与死亡重置流程；确定中断优先级。
5. 增加边缘跳跃、跳跃缓冲、短跳高度、翻滚墙体碰撞、暂停恢复、手柄输入的实际物理 PlayMode 测试，并先在 Unity 完成当前阶段验收。
6. 待用户授权后才进入机关、敌人 FSM、关卡及 Boss；本轮没有实现这些内容。
