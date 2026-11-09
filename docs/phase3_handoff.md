# Phase 3 战斗接入

敌人使用 Rigidbody2D 和 Body Collider 与地形交互。在子物体放置 EnemyHurtbox 层触发体和 Hurtbox2D，绑定根物体的 Health。需要多个部位时把它们指向同一 Health。带受击表现时可订阅 `Damaged`、`Changed`、`Died`，由敌人的 Motor 或表现组件执行击退与反馈。

敌人 FSM 可以在固定时钟调用 `AttackHitbox2D.Begin(definition, facing)`，在攻击状态持续调用 `Step(dt)`，在退出、打断和死亡时调用 `Cancel()`。攻击组件的目标掩码选择 PlayerHurtbox。攻击矩形由配置镜像，实例内部负责窗口、去重、发送 DamageInfo。需要被完美格挡打断时，在攻击根物体实现 `IParryable.OnParried()`。训练装置是现成的固定时钟和打断例子。

流浪老鼠、飞虫和 Boss 可共享 AttackDefinition、AttackHitbox2D、Hurtbox2D、Health 和事件机制。各自定义移动、目标选择、前摇提示和状态规则。范围攻击也应通过 Health 的统一伤害入口传递 DamageInfo。不可格挡招式由 AttackDefinition.Blockable 配置。攻击者同一次查询会跳过自身及死亡目标。

进入下一阶段前应确认以下边界。

- 现有矩形随朝向镜像，适合本阶段左右向攻击。投射物、垂直或旋转攻击需要独立轨迹与命中生命周期。
- 区域检测按固定步采样位置。高速飞行或穿透窄目标时应验证扫掠检测与多目标顺序。
- Health 当前的玩家防御从同根 PlayerCombat 获取。敌人若也需要护甲或方向防御，可在出现实际第二种使用者后提取通用防御契约。
- 同一攻击对无敌目标只判定一次接触。持续地形危险或多段 Boss 攻击需明确每段的新实例与重复伤害策略。
- 对地形遮挡、不同身高、移动 Hurtbox、击退碰撞与实际手感需要额外关卡验收。当前暂停时钟可供后续命中停顿扩展。
