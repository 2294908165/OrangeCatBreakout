# 第二关 · 冷库输送线

第一关拿到罐头后按 **E** 或点 **Next stage (E)**，所选角色会进入冷库。也可以直接打开 `Assets/Game/Scenes/ColdStorage02.unity` 并点 Play。R 重试当前关卡。

从冷库装卸口撤离后按 **E** 或点 **Next stage (E)**，保留所选角色进入 [第三关装卸升降台](loading_bay03.md)。

入口是向右的安全输送带，速度为 1.65 units/s。战斗区输送带向左，速度为 1.45 units/s。角色站在带上时会被带动，起跳带走起跳瞬间的水平速度。箭头与输送方向一致。

飞虫在头顶悬停。俯冲前锁定终点，琥珀色斜线标出路线。预警 0.46 秒，俯冲 0.32 秒，收招 0.72 秒。飞虫有 6 HP，俯冲打中造成 2 点伤害。哈橘可以跳起出爪或等飞虫落低时哈气。姜爪可以完美格挡使飞虫坠到反击范围。击败后右侧门开启，走到装卸口的灯标通关。

在 Unity 6000.3.28f1 编译、构建和相机渲染成功。PlayMode 46/46 通过，新增验证覆盖输送带接缝、起跳速度、飞虫预警锁定与俯冲伤害、完美格挡及反击、出门结算和一二关衔接。证据位于 `TestResults/ColdStorage02`。预警和输送带节奏仍需人工试玩。

可以用 **Orange Cat → Build Cold Storage 02** 重建生成场景。背景和飞虫源图为 `ArtSource/PixelPreview/ColdStorageBackdrop.png` 与 `ArtSource/PixelPreview/ColdFly.png`，由内置 imagegen 生成。

## 最终生成提示词

**飞虫**

> Create ONE true transparent pixel-game sprite atlas, EXACTLY 4 columns by 2 rows of 8 separate full-body poses. Reference atlas is ONLY for the restrained 16-bit SNES pixel rendering and layout, do NOT draw any rat. Subject is one small original cold-warehouse fly enemy, right facing in side-on 3/4 view, distinctive pale mint-gray chitin, two icy translucent blue wings and burnt-orange eyes, dark navy outline, tiny insect legs and round head. Design is compact (around 28 logical pixels tall) and reads well over blue industrial backgrounds. Keep identical body size and proportions, centered in each cell, broad transparent gutters, never touching cell boundaries. Top row four in-place hovering flight poses with wing beats high, mid, low, mid. Bottom row: one tight windup holding wings up, one wings swept back front-first diagonal downward dive, one lowered dazed recovery pose with wings half-open, one defeated limp pose with wings folded. No hit sparks, text, shadows, labels, glow, ground or background. Only the eight isolated game-ready pixel sprites, coherent hard nearest-neighbor pixel clusters, genuine alpha.

**冷库背景**

> Panoramic 3:1 BACKGROUND ONLY for level two of this original 2D SNES-style pixel art platform game. Match the reference's crisp 16-bit pixel clusters and restrained detail, but create a visibly different INDOOR refrigerated warehouse sorting room. Strictly side-on orthographic camera, aligned vertical walls and horizontal fixtures. Left sector frosted insulated loading-room panels and cold vapor around small pipe valves, center long sequence of chilled metallic wall panels and overhead blue fluorescent lamps, right warm amber light on the loading-bay exit frame. Steel navy, pale cyan and muted frost. Leave the lower quarter as a dark quiet wall base behind gameplay, no visible floor, no belt, no props that look like jump platforms. The game will layer characters and an actual conveyor belt separately. Make the combat band clean and mid-dark for sprite readability. Full bleed panorama, no perspective tilt, no characters, no animals, no text, no UI, no floor or foreground.
