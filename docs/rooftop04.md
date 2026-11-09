# 第四关 · 屋顶检修区

从第一关 `Warehouse01` 开始，第一、二关通关后按 **E**，使用手柄时按北侧键。第三关搭乘货梯到顶后，自动进入 `Rooftop04`。续关保留角色选择，每关从该角色的满生命值开始。想单测第四关，可以直接打开 `Assets/Game/Scenes/Rooftop04.unity` 并点 Play。

1. 从货梯落脚区向右走，跳过排汽口。琥珀闪光表示即将喷汽。
2. 跳上检修走道，在 **BREAKER A** 旁按 **E**。指示灯变绿并显示 **ON**。
3. 跨过两处错峰排汽口，落在高台前的空地，再跳到上层给 **BREAKER B** 通电。如果铁门仍显示 **LOCKED**，看左上角提示，往标出的方向找漏掉的电闸。
4. 门升起后，走到门后的绿色 **BOSS EXIT** 标记旁，按 **E** 撤离。走到最右侧护墙也可以按 **E** 撤离。看到 **Rooftop clear** 后，再按 **E** 进入第五关罐头守卫。按 **R** 重试当前关。

**Orange Cat → Build Rooftop 04** 可重建场景。需要现有的 PixelPreview、雨夜仓库、冷库和装卸台素材。第四关复用蒸汽预警和伤害判定，电闸状态及离开条件由 `RooftopEscape` 管理。

Unity 6000.3.28f1 编译与场景构建成功，PlayMode 49/49、EditMode 2/2 通过。用键盘输入测试了第三关登梯后续关，以及第四关跨越排汽口、开启两处电闸、撤离和重试。结果位于 `TestResults/Rooftop04`，Unity 相机预览位于 `docs/Rooftop04.png`。连续人工试玩和手柄实测待完成。

铁门提示与出口范围更新后，PlayMode 52/52、EditMode 2/2 通过。回归测试验证只开 B 时铁门仍挡路并提示向左找 A，开齐后走过出口标记也能在护墙边按 E 撤离。记录位于 `TestResults/RoofGate`。

## 背景素材

背景使用内置 imagegen 生成。原图保存在 `ArtSource/PixelPreview/RooftopBackdrop.png`，导入的 Sprite 位于 `Assets/Game/Art/PixelPreview/Rooftop/Blue hour.png`。

> Create an ultra-wide side-on 16-bit pixel art industrial rooftop panorama at blue hour for a Unity 2D platformer background. Strict side view, layered silhouettes of a rainy warehouse district, with muted navy and slate rooftop structures, a few warm distant factory windows, antennas and water towers, pale early dawn at the far right. Moody but readable. Keep the lower quarter darker and visually quiet behind the separately placed player and gameplay platforms. No continuous foreground floor, playable geometry, doors with signage, characters, UI, text, logos, strong foreground lights, or black borders. Crisp chunky hand-placed-looking pixel clusters, retro game palette, use an opaque full canvas. Composition should read well when camera sees only the left, middle, or right third.
