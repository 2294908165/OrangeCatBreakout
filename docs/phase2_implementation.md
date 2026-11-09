# Phase 2 实现说明

用 Unity 6.3 LTS 6000.3.28f1 打开工程，在 Project 面板双击 `Assets/Game/Scenes/CombatTest.unity`，然后按 Play。哈橘默认出场，HUD 可以切换姜爪、重置训练目标或重新开始。也可以通过菜单 **Orange Cat → Build or Update CombatTest** 重建生成场景和战斗 Prefab。构建器保留已存在的战斗数据资产，所以 Inspector 调参可以随工程保存。

## 代码与资产

| 文件 | 职责 |
| --- | --- |
| 新增 `Scripts/Combat/DamageInfo.cs` | 伤害数据、伤害结果、IDamageable 和可中断攻击源接口 |
| 新增 `Scripts/Combat/Health.cs` | 生命、治疗、统一扣血入口、死亡和生命变化事件、训练目标重置 |
| 新增 `Scripts/Combat/Hurtbox2D.cs` | 从独立触发体找到归属 Health，绘制青色范围 |
| 新增 `Scripts/Combat/AttackHitbox2D.cs` | 实例时钟、朝向镜像、区间查询、命中去重、攻击清理及事件 |
| 新增 `Scripts/Player/PlayerCombat.cs` | 连击、格挡、反击、受击硬直和受击保护 |
| 新增 `Scripts/Combat/CombatFeedback.cs` | 受击闪白、完美格挡闪光、固定目标的视觉击退和结果文字 |
| 新增 `Scripts/Combat/TrainingAttackSource.cs` | 固定周期攻击、范围预警、被完美格挡时中断 |
| 新增 `Scripts/Combat/CombatTestSession.cs` | 场景内换角、重启、目标重置、HUD |
| 新增 `Editor/CombatTestBuilder.cs` | 生成战斗测试场景、战斗 Prefab、配置和 Hurtbox 层 |
| 新增 `Tests/PlayMode/CombatTestCases.cs` | T01–T23，含场景、时序、物理与 Input System 用例 |
| 修改 `PlayerMotor2D.cs`、`PlayerStateMachine.cs` | 保留原运动入口，新增受击速度、格挡移动和受限状态转移 |
| 修改 `CharacterDefinition.cs`、`AttackDefinition.cs` | 角色战斗能力与攻击窗口可配置 |
| 修改 `PlayerInputReader.cs`、`GameManager.cs` | Update 采样战斗输入，FixedUpdate 消费按键边沿，统一暂停入口 |
| 修改 `PhaseOneBuilder.cs` | 供 CombatTestBuilder 复用占位资源、地面和工程设置 |
| 生成资产 | CombatTest、HaoJuCombat／JiangZhuaCombat、七个战斗数据资产及其 Unity 元数据 |
| 工程设置 | TagManager、Physics2DSettings、EditorBuildSettings 增加战斗层和场景 |

脚本路径以上相对于 `Assets/Game`。CombatTest 使用独立战斗 Prefab 和角色配置，PhaseOne 场景及原 Prefab 继续作为运动回归环境。

## 逻辑与状态

- **攻击**。Update 缓存 J 的按下边沿。Motor 在 FixedUpdate 更新接地与朝向，PlayerCombat 判断能否开招。每次攻击锁定朝向并获得独立 ID。Startup、Active、Recovery 共用一个时钟。检测发生在与 Active 区间相交的物理步，因此一个步长跨越整个有效期时也会查询。
- **判定**。Body Collider 保留实体物理。Hurtbox 是独立的查询触发体，由掩码区分阵营。OverlapBox 可以找出多个目标，HashSet 按 Health 去重，跳过自身与死者。无敌、格挡和完美格挡的接触也占用该实例对该目标的一次尝试。
- **哈橘**。第一段接受窗口内的第二次按键，第一段结束时消费该缓冲并新建第二段实例。第一段只允许后摇后段、接地时翻滚取消。取消、受击和死亡立即清理实例和连击缓冲。
- **姜爪**。接地按住 K 可进入格挡。正面可格挡攻击在完美窗口内免伤并授予一格反击资格，其他正面可格挡攻击乘以 0.30。背击进入普通伤害。一次按住 K 只开启一次完美窗口。反击可从 Guarding 直接进入 Attacking，也可在松键后从 Free 进入。
- **受击**。Health 校验伤害、询问玩家防御、扣血并发事件。普通受击打断攻击，Motor 在固定步施加击退并保留重力。格挡减伤维持 Guarding。受击保护与翻滚无敌分开维护，任一生效即可挡住伤害。归零进入终态 Dead，玩家重开或换角后使用新实例。
- **反馈**。命中目标闪白，完美格挡闪青。固定目标的渲染子物体会后移并复位。暂停使用原有 GameManager 统一控制 Time.timeScale，场景重载恢复时钟。当前反馈以颜色、位移与 HUD 为主，命中停顿和音效待手感验收后加入。

| 状态 | 运行期转移 |
| --- | --- |
| Free | 地面翻滚、合法普通攻击或反击、接地格挡 |
| Attacking | 攻击完成回 Free，哈橘第一段窗口内接地可切 Rolling，受击或死亡可中断 |
| Rolling | 到时回 Free，无敌窗口外可受击 |
| Guarding | 松键或离地回 Free，持资格可反击，背击或不可格挡攻击可打断 |
| Hurt | 硬直到时回 Free，致死进入 Dead |
| Dead | 本实例终止战斗与控制 |
