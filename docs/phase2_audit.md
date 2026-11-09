# Phase 2 开工审计

Unity 6000.3.28f1 正在本机运行，主工程已打开 PhaseOne。开工前读了玩家、输入、相机、暂停和构建器源码，并检查了现有设置、输入资产和测试 XML。

| 范围 | 现状 |
| --- | --- |
| 移动与跳跃 | PlayerMotor2D 在 FixedUpdate 消费输入，处理加减速、朝向、接地、二段跳、土狼时间、跳跃缓冲和松键截断。没有单独的 PlayerController。 |
| 翻滚 | 接地进入，锁定方向，沿用 0.42 秒、9 units/s。`Invincible` 从状态及时间计算，身体 Collider 始终启用。 |
| FSM | Free 和 Rolling 有运行逻辑。Attacking、Guarding、Hurt、Dead 已定义，只有基础转移守卫。 |
| 配置与输入 | 两个角色资产存在，旧资产移动速度相同。AttackDefinition 仅有时序、伤害和矩形字段。J / K 已绑定 Attack / Guard，输入组件尚未消费。 |
| 场景 | PhaseOne、双占位 Prefab 和构建菜单已生成。当前 2D 矩阵隔离 Player / Enemy 身体，预留双方 Hitbox 层。 |
| 战斗 | 缺少生命、伤害入口、Hurtbox、攻击实例、连击、格挡、反击、训练对象和战斗反馈。 |
| 历史验证 | 已有实际 Import.log 与测试 XML，EditMode 2/2、PlayMode 1/1。现有 PlayMode 加载并等待真实落地，随后直接步进翻滚和跳跃计数。土狼、缓冲和跳高的端到端边界尚未覆盖。 |

复用现有 Motor、FSM、Input System、角色及攻击资产类型、跟随相机和 PhaseOneBuilder 的共享构建能力。新建 Combat 模块、玩家战斗协调组件、训练组件、CombatTest 构建入口和 PlayMode 用例。局部修改 Motor、FSM、输入、配置、暂停管理器及构建器。保留 PhaseOne 的场景和测试，CombatTest 使用单独增强的 Prefab。

风险集中在短有效窗跨物理步、多个 Hurtbox 重复命中、伤害回调同步打断攻击、按住格挡键自动重开完美窗、切换角色遗留输入和暂停时钟冲突。当前工程已被交互 Editor 打开，批处理将使用同内容的隔离副本，避免占用或关闭用户窗口。
