# 实际验证记录

日期：2026-11-06（Asia/Shanghai）。

Unity Hub 3.22.5 已安装到 D:/Unity/Hub；Unity Editor 6000.3.28f1_8f082d2fe476 已安装到 D:/Unity/6000.3.28f1。用户已登录并激活许可证，批处理实际取得授权。

官方版本：https://unity.com/cn/releases/editor/whats-new/6000.3.28f1。

已执行 Unity batchmode 导入、C# 编译及 PhaseOneBuilder.Build；退出码 0，无 error CS / warning CS。实际生成 PhaseOne 场景、双角色 Prefab、角色数据、占位图片及物理材质，并加入 Build Settings。Input System 由 Unity 自动更新至 1.20.1，Test Framework 为 1.6.0。

- EditMode: 2/2 passed, 0 failed.
- PlayMode: 1/1 passed, 0 failed.

证据：TestResults/Import.log、EditMode.xml、EditMode.log、PlayMode.xml、PlayMode.log。

PlayMode 测试真实加载生成场景并等待物理落地，随后通过生产 Simulate 接口验证翻滚状态、速度、无敌与身体碰撞器保持启用，以及二段跳次数上限。

尚未验证：使用 batchmode/nographics 运行，没有人工键盘/手柄体验或画面验收；现有测试没有完整覆盖土狼时间、跳跃缓冲与可变跳高的真实物理边界。未进行独立 Windows Player 打包测试。

EditMode 日志有 UnityConnect 云配置请求超时，不影响测试通过。Phase 2 战斗、格挡反击、敌人及 Boss 未实现。
