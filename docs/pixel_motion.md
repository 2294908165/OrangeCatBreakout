# 像素角色动画

在 Unity 打开 `Assets/Game/Scenes/PixelPreview.unity` 后点 Play。两只角色各有四帧待机、四帧跑步和四帧翻滚。哈橘可用 J 打出低位掠爪、转肩上挑和双爪下劈。姜爪有架爪格挡、普攻和两帧强化反击。跳跃有上升、下落和屈膝落地三种姿势，受击有各自的后仰姿势。

## 哈橘三连击

每段各按一次 J。第一段压低身体横扫，第二段转肩抬身上挑，第三段举起双爪后斜向下劈。每段都有蓄势、出爪和收招三张姿势。橙色掠光、青色上挑弧和两道金色下劈弧跟随锁定的攻击方向，在有效窗口内展开并淡出。三段分别造成 2、2、3 点伤害，第三段的范围和击退更大。

Unity 6000.3.28f1 编译与动画构建成功，PlayMode 48/48 通过。测试覆盖三段命中、连段上限、各段姿势及爪光镜像。测试证据位于 `TestResults/ClawFlow`，三段出爪的相机对照图位于 `docs/ClawFlow.png`。

源图 `ArtSource/PixelPreview/HaoJuClawFlow.png` 使用内置 imagegen 生成，经 **Orange Cat → Build Pixel Animations** 切分并绑定。三段爪光由同一构建工具绘制为独立像素 Sprite。最终提示词如下。

> Nine separate pixel-game sprite frames of ONLY the left reference cat HaoJu, exact 3 columns x 3 rows; generous translucent-free gutters and clear isolated cells. Right-facing side-on three-quarter SNES pixel art, same orange folded-ear round head, compact cat torso, teal scarf, cream muzzle, big natural padded paws and striped tail, no clothes. Use fixed physical scale and a common foot baseline within each cell. This is expressive claw combat, never a human boxing punch. COLUMNS are consecutive attacks, ROWS are windup (top), active contact (middle), and follow-through (bottom). COLUMN 1: load low by leaning forward, then fast low horizontal open-paw rake, then remain in low forward stance with the paw sweeping down past the knee, scarf trailing. COLUMN 2: twist shoulders back to load the opposite paw, then rotate hips and rake upward with the opposite paw open and visibly high above the shoulder, then finish extended upward with the chest raised and back knee bent. COLUMN 3: crouched load with BOTH separated open paws held high over the head and knees flexed (not crossed), then both front paws swipe down together in a strong forward diagonal double-claw chop with visible open separated claws, then deep low follow-through with both paws near ground and scarf overshooting forward. Show a strongly distinct low, high and down silhouette across the three columns, and clear anticipation and follow-through within each column. Always match the same character and keep both paws, feet, ears and entire tail inside its own cell. Chunky 16-bit pixel clusters with stable palette and warm outline. True alpha, no slash marks, VFX, props, text, shadows, scenery or gridlines; only nine cat sprites.

出爪姿势由 PlayerPixelAnimator 读取攻击有效窗口，跑步由实际水平速度驱动，翻滚读取 FSM 经过时间。帧序列只替换 SpriteRenderer 的图片。地面锚点、控制器、实体碰撞体和攻击窗口仍由原组件处理。

两张生成源图位于 `ArtSource/PixelPreview/HaoJuMotion.png` 与 `ArtSource/PixelPreview/JiangZhuaMotion.png`。编辑图集后可执行 **Orange Cat → Build Pixel Animations** 重新切分与绑定。生成帧位于 `Assets/Game/Art/PixelPreview/Motion`，规格为 32 PPU、Point 过滤，每只角色共 12 个新增姿势。

Unity 6000.3.28f1 实际编译与动画构建成功，PlayMode 27/27 通过。两个新增测试验证跑步、翻滚、两段爪击、架爪和强化反击的姿势选择。Unity 相机渲染了二段爪击与强化反击的画面对照。批处理无法替代连续试玩和人工手感验收。证据位于 `TestResults/PixelMotion`。

## 哈橘哈气

在地面自由状态按 **Q**，手柄为右肩键。攻击锁定当前朝向，前摇 0.10 秒，有效 0.12 秒，后摇 0.24 秒。声波宽 1.50、高 0.72，伤害 1，冷却 1.20 秒。战斗数据位于 `Assets/Game/Data/Combat/HaoJuHiss.asset`。生成源图为 `ArtSource/PixelPreview/HaoJuHiss.png`，更新素材后可运行 **Orange Cat → Update HaoJu Hiss**。实际 PlayMode 29/29 通过，包含 Q 输入、单次命中、左右方向、范围、冷却、角色限制和声波姿势验证。证据位于 `TestResults/Hiss`。音效使用短合成气声，仍需人工试听。

哈气姿势使用内置 imagegen 生成，最终提示词如下。

> Create exactly ONE transparent 16-bit pixel-art game sprite of ONLY HaoJu, the left orange cat in the reference. Same round head, folded small ears, striped orange short fur, cream cheeks, teal scarf, no trousers, compact balanced build, short paws and thick striped tail. Strictly facing right with a near-profile three-quarter angle, both hind paws planted on a common baseline. HISSING POSE: shoulders raised, leaning forward from a grounded braced stance, front paws close to chest, ears slightly flattened and mouth visibly wide open, small triangular fangs and dark interior, recognizable expressive rounded face. Tail lifts and scarf trails to left, preserve proportions and color palette. At game size around 42 pixels high, use hard classic pixel-art clusters. Entire character contained within canvas with margin. Fully transparent isolated background. No lettering, no exhalation visual, no swoosh, no glow or shadows, no other characters or props. One pose only.

## 二段跳团身

两只猫在二段跳开始时复用地面翻滚的两张抱团帧，约 0.16 秒后用翻滚结束帧舒展，随后回到普通空中姿势。像素密度、SpriteRenderer 缩放、碰撞体和跳跃参数都沿用现有设置。二段跳仍由成功的空中跳跃触发。Unity PlayMode 30/30 通过，新增验证覆盖两只猫的团身、转身、舒展和落地。证据位于 `TestResults/AirCurl`。

## 角色受击反馈

在 Warehouse01 和 PixelPreview 中，角色遭到普通命中时短暂闪红、播放独立的后仰姿势，并在命中点显示火花和播放击打音。格挡减伤使用冷色反馈，翻滚无敌没有受伤火花。Unity PlayMode 39/39 通过，新增用例覆盖老鼠和机关的实际伤害、恢复、格挡和翻滚无敌。测试证据在 `TestResults/PlayerHurt`，击打音复用现有音效，尚未人工试听。

源图 `ArtSource/PixelPreview/PlayerHurt.png` 使用内置 imagegen 生成，经 **Orange Cat → Build Pixel Animations** 切分绑定。最终提示词如下。

> Create a single game-ready transparent pixel sprite atlas with exactly TWO separate full-body sprites side by side, very wide transparent middle gutter. Same two 16-bit game cats as input. Left HaoJu, orange round-headed folded-ear tabby with compact build, long striped tail and teal scarf. Right JiangZhua, cream slim cat with tall upright ears, green collar and small silver bell. Both face RIGHT, both reacting to being hit from their front: torso tilted backward with back leg braced, head pulls back, eyes squeeze shut, front paws come up defensively without a guard stance, tail and scarf flick forward, distinct flinch pose readable at game scale. Maintain original head and torso volumes, biped gameplay silhouette, full body visible, natural cat paws and same pixel palette, around 42-46 effective game pixels tall with crisp hard 16-bit clusters. Each sprite wholly within its own half of the canvas. No text, no ground, no effects, no shadow, no glow, no red edges, just the two cats on genuine transparency.

## 倒地动作

哈橘和姜爪进入 Dead 后显示倒下帧。至少经过 0.16 秒并且实际接地后，才切到闭眼躺地的姿势。空中失去全部生命时仍依原有重力下落。Unity PlayMode 40/40 通过，验证了地面倒地、空中死亡后落地、持续躺姿及实体碰撞。证据位于 `TestResults/PlayerDeath`。

源图 `ArtSource/PixelPreview/PlayerDeath.png` 使用内置 imagegen 生成，由 **Orange Cat → Build Pixel Animations** 切分并绑定到两只猫。最终提示词如下。

> Exactly FOUR independent pixel-art gameplay sprite frames arranged in an exact 2-column by 2-row atlas. Completely transparent PNG, large transparent margins and gutters, each frame fully within its own cell. LEFT COLUMN always HaoJu, the reference orange round-faced folded-ear tabby with teal scarf, cream muzzle and striped tail. RIGHT COLUMN always JiangZhua, the reference cream slim tall-ear cat with green collar and silver bell. TOP ROW: initial collapse pose facing right, eyes gently shut, knees buckle and torso tilts backward/down, one forepaw slack, body is clearly falling toward ground, feet visible. BOTTOM ROW: final clearly horizontal side-lying defeated pose; head at the RIGHT, cheek resting at the same baseline as the body, eyes closed, tail relaxed to the left, visible forepaws also resting on the baseline, no raised torso, no sitting. Keep the head and body volume consistent across both rows; the bottom row silhouette must be much lower and wider, roughly half the height of the collapse pose. Clean coherent SNES-style 16-bit pixel blocks at scale of 42-46 game pixels tall for the original standing characters. Non-graphic, no blood or wounds, no extra props or effects, no text or grid, no drop shadow, no colored glow. Preserve their original recognizable silhouettes, palettes and accessories.

## 跳跃与落地

上升和下落姿势由竖直速度切换。接地时显示约 0.11 秒的屈膝姿势和约 0.16 秒的水花。水花留在落点，角色可立即移动或再起跳。二段跳保留翻滚式团身。PlayMode 43/43 通过，证据位于 `TestResults/FirstJump`。

源图 `ArtSource/PixelPreview/PlayerJump.png` 使用内置 imagegen 生成，运行 **Orange Cat → Build Pixel Animations** 可重新切分。最终提示词如下。

> Create exactly SIX isolated transparent gameplay pixel sprites in an exact 3 columns by 2 rows atlas, with generous transparent margins and wide gutters. Top row always the reference's left cat HaoJu, round folded ears, orange fur stripes, teal scarf, balanced stocky silhouette. Bottom row always reference's right cat JiangZhua, cream fur, pointed ears, green collar with tiny bell and lean build. All face RIGHT in platformer side-on three-quarter style. First column: FIRST JUMP rise pose, pushing off into air, head and chest lead slightly upward, one hind paw trailing extended behind, other bent, forepaws rising, tail and scarf drag down, do not curl into a ball. Second column: FALLING pose, torso upright, two natural front paws open for balance, hind paws extend down and forward ready to catch the ground, tail and scarf lift slightly. Third column: LANDING pose, both feet planted on a common baseline, knees flexed and torso a little lower, head and body volume remain the same, tail lowers naturally. Same scale for all frames, full cat and tail fully inside each cell, with fixed foot anchors. Match existing game silhouettes, original head size, palette and crisp SNES 16-bit pixel clusters, about 42-46 effective game pixels tall. Do not add water, lines, effects, props, lettering, labels, shadows, glow or background. Genuine alpha transparency, three well-separated columns and two well-separated rows.

## 待机循环

两只猫在地面站定时播放 0.96 秒的四帧循环，呈现呼吸、摆尾和眨眼。每组帧用同一比例并对齐脚底。暂停冻结循环，移动、跳跃和战斗状态各用自己的姿势。PlayMode 41/41 通过，已验证两只猫的四帧循环、无位移、暂停和起跳落地切换。证据位于 `TestResults/PlayerIdle`。

源图 `ArtSource/PixelPreview/PlayerIdle.png` 由内置 imagegen 生成，使用 **Orange Cat → Build Pixel Animations** 切分绑定。最终提示词如下。

> A production 16-bit pixel-art IDLE sprite atlas for exactly TWO reference characters. Exactly 4 columns by 2 rows with eight fully separate sprites, each contained in its own equal-size cell, centered on the same foot baseline. Very wide transparent gutters and no grid lines. Top row all four frames of HaoJu, the left reference cat: round folded-ear orange tabby with soft belly, striped tail, and flowing teal scarf. Bottom row all four frames of JiangZhua, the right reference cat: slim cream cat with tall erect ears, green collar and a tiny bell, pale curled tail. Both are standing biped in a right-facing ready posture and always face the same way. These are subtle frames of one seamless idle loop, preserve the head size, body width, leg lengths and foot positions consistently across frames. Frame 1 relaxed neutral, frame 2 slight breathing up with tail curling upward and ears alert, frame 3 peak inhale with small paw shift and visible scarf flutter for HaoJu / tiny collar bell and ear twitch for JiangZhua, frame 4 exhale back near neutral with a soft blink or partly closed eyes. Both feet always firmly planted in the same stance, no steps, no running, no jumping, no crouching, movement confined to chest bob, ears, tails, scarf and expressive eyes. Exact same character design and pixel palette as reference. Low-res chunky crisp SNES pixel clusters, no background, shadow, glow, text or visual effects. Genuine alpha transparency.
