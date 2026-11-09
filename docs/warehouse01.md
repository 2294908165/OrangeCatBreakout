# 第一关 · 雨夜仓库

在 Unity 双击 `Assets/Game/Scenes/Warehouse01.unity` 并按 Play。进入战斗前可在 HUD 选择哈橘或姜爪。A/D 移动，空格跳跃，左 Shift 翻滚，J 出爪。哈橘 Q 哈气，姜爪 K 格挡。完美格挡冲撞后按 J 强化反击。R 从入口重新开始。

前半段依次经过蒸汽井盖、地刺和工业风轮。井盖进入 0.55 秒琥珀色预警后才喷气，停喷时可直接通过。地刺可以跳过，风轮前的低木箱可作为起跳台。触及危险区扣 1 HP。越过风轮后才进入老鼠的警戒范围。

流浪老鼠靠近后会压低身体，并用地面的琥珀色预警带标出冲撞方向。冲撞以 2 点伤害结算，击中同一目标一次。前摇为 0.42 秒，冲撞窗口为 0.28 秒，收招为 0.62 秒。老鼠有 8 HP，完美格挡会停止冲撞并使其短暂僵直。击败后右侧门开启，接触金罐即通关。

可用 **Orange Cat → Build Warehouse 01** 从像素试玩场景重新生成本关。老鼠动作源图为 `ArtSource/PixelPreview/StrayRat.png`，由内置 imagegen 生成。攻击数值存放于 `Assets/Game/Data/Combat/StrayRatCharge.asset`。

老鼠被实际扣血时会后仰并短暂变暖色。击中处显示一闪而散的金色像素火花，并播放短击打音。火花停在接触点，老鼠继续按原物理击退移动。受击源图为 `ArtSource/PixelPreview/StrayRatHurt.png`，使用内置 imagegen 生成。音效为合成短音，尚未人工试听。

Unity 6000.3.28f1 实际编译和场景构建成功。PlayMode 37/37 通过，包含蒸汽预警与伤害阶段、地刺和风轮接触伤害、使用 D 和空格无伤走完整段障碍路线、老鼠受击和反击，以及击杀、开门和通关。证据位于 `TestResults/WarehouseHazards`。障碍的实际手感与通关用时仍需人工试玩。

后续 PlayMode 43/43 通过。使用虚拟键盘按键让哈橘和姜爪分别从入口无伤通过三道机关、躲避或完美格挡老鼠的冲撞，随后击倒老鼠并拿到罐头。证据位于 `TestResults/FirstJump`。预警是否容易读懂仍需人工试玩。

拿到罐头后，按 **E** 或点击 **Next stage (E)** 进入第二关，保留所选角色。见 [冷库输送线](cold_storage02.md)。

## 老鼠图集的最终生成提示词

> Create ONE transparent SNES-style production sprite atlas for the first enemy of this pixel-cat game. The reference is for pixel density and painterly pixel style ONLY, do not include either cat. Exact evenly spaced 4 columns by 2 rows; 8 independent right-facing poses of the same quadrupedal stray rat, each fully contained in its own cell, ample transparent gutters. Scrappy dark warm grey-brown rat, large scarred rounded ears, pointed cream muzzle, bright pale eyes, pink segmented tail, four paws, no clothes, clean readable face. About 34 game pixels tall with crisp nearest-neighbor pixel clusters and cohesive muted palette. The rat is lower and longer than the standing cats. Same body proportions, scale, and foot baseline across all cells. Top row: standing alert, scurry extended, scurry compressed, scurry opposite stride. Bottom row: visibly crouched anticipation with ears down and tail arched, airborne low forward lunge with paws forward, exhausted recovery with low head, slumped side-lying defeated pose (non-graphic). No shadows, text, background, grids, tiles, effects, or other animals. Genuine transparent background.

## 受击帧的最终生成提示词

> Create EXACTLY ONE isolated sprite of this same SNES-style quadrupedal stray rat, no other poses, transparent PNG. Match the reference dark warm gray-brown fur, scarred large rounded pink ears, pointed cream muzzle, pink segmented tail, pale eyes, body proportions and chunky coherent pixel grid. The rat still faces RIGHT. Make a very clear distinct NON-GRAPHIC HIT REACTION: head jerks backward and up, nose angled slightly upward, eyes squeezed shut, ears flattened, forelegs lift, rear paws brace and belly pulls inward. The whole rat is flinching backwards away from a blow arriving from the right, tail springs backwards. Full silhouette fully inside frame with margins, roughly same head size and normal body volume as reference. No glow, colored edge, shadow, labels, sparks, stars or other effects. Just the single rat pose on genuine transparency, hard-edged 16-bit pixel art.

## 机关素材

源图 `ArtSource/PixelPreview/WarehouseHazards.png` 由内置 imagegen 生成，构建器将四张独立素材切分导入。最终提示词如下。

> One game-ready transparent pixel art sprite atlas with four completely isolated props in an EXACT 2-column by 2-row grid. Reference image defines palette and crisp SNES warehouse pixel style only. Each prop sits wholly inside its cell, with large transparent gutters and margins, no floor or background. TOP LEFT: wide flat industrial manhole/steam vent, side-on 2D platformer view, dark navy cast iron horizontal grate with amber trim and rivets, very low rectangular silhouette. TOP RIGHT: one short row of five triangular sharp floor spikes on a low iron base, silver tips, rusty orange highlights, strictly side-on. BOTTOM LEFT: exposed circular four-bladed industrial metal fan ROTOR alone, straight frontal view with centered round bolt hub, each identical blade curves evenly out from center, steel-gray and worn orange hazard-striped tip accents, no housing/frame/pedestal, symmetrical centered for rotation, strong readable contrast. BOTTOM RIGHT: a compact vertically rising burst of hot white-blue steam with several sharply readable chunky billowing wisps tapering outward, no vent attached. Cohesive detailed but low-resolution 16-bit pixel clusters, clean hard edges. No background, no text, no UI, no grid, no extraneous elements, no ground shadows, full alpha transparency.
