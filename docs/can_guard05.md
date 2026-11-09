# 第五关 · 罐头守卫

从第四关的绿色出口撤离后，再按 **E** 进入屋顶机房。也可以在 Unity 打开 `Assets/Game/Scenes/CanGuard05.unity`，点击 Play。Boss 开战前可以换角，进关时会沿用上一关选择的角色。

- **冲撞**先亮起金色路线。看到蓄力姿势后准备翻滚或二段跳。守卫到墙边会停下约 1.55 秒，可以靠近反打。
- **砸地**先标出粉色落点。守卫落地后，两道贴地冲击波向左右分开。拉开一点距离，等波靠近时长按跳跃，落地后抓住约 1.65 秒的收招窗口。
- 守卫有 32 点生命，冲撞伤害为 2，地波伤害为 1。守卫倒下后门会升起，走到最右侧拿起最后一罐即通关。按 **R** 重试，通关后按 **E** 从第一关再开一局。

`CanGuardBrain` 管理预警、冲撞、跃起、收招和倒地。攻击使用既有 `AttackHitbox2D`、`Hurtbox2D` 和伤害流程，两个攻击数据位于 `Assets/Game/Data/Combat/CanGuardCharge.asset` 与 `CanGuardWave.asset`。**Orange Cat → Build Can Guard 05** 可重建场景，素材可在 `Assets/Game/Art/PixelPreview/CanGuard` 查看。

Unity 6000.3.28f1 编译、场景构建和相机渲染成功，PlayMode 51/51、EditMode 2/2 通过。自动测试覆盖屋顶续关保留姜爪、冲撞命中和撞墙收招、落点锁定、长按跳跃越过地波、倒地停止攻击，以及玩家爪击击败守卫后拾罐。记录位于 `TestResults/CanGuard05`。Boss 战时长和整局连续手感尚未人工试玩。

## 像素素材

下列源图位于 `ArtSource/PixelPreview`，均使用内置 imagegen。待机使用经确认的设定图，动作图以该图为参考。

### CanGuardIdle.png

> Create ONE full-body character concept image for the boss “Can Guard” in a 2D side-scrolling 16-bit pixel action game. The attached screenshot is the character reference. Ignore the phone interface, black borders and background. Faithfully translate its very distinctive comical expression and proportions into crisp hand-placed-looking pixel clusters. A stocky blue-violet cat with oversized swept pointed ears with dusty pink interiors, very round broad pink face, deep indigo cheek stripes, tiny dark cat nose, large half-lidded gray-green eyes with visibly thick black lashes and eyeliner, glossy conspicuously full red lips, pink belly, dark royal-blue collar and chunky bronze bell. Keep the face exceptionally recognizable, mischievous and self-assured. Invent the unseen lower body in proportion: solid short hind legs with wide padded cat feet planted in a wide stance, thick gently curling tail with indigo rings, heavy rounded front paws held slightly forward. Oversized relative to a game protagonist, broad and grounded, strong silhouette, agile enough to charge and leap. Three-quarter side-facing LEFT like a 2D boss idle sprite, keep both eyes readable and avoid a flat front view. One full figure centered, every ear, paw and tail tip within frame, no lettering or labels. Restrained 16-bit palette with hard pixel edges and distinct colored outlines, limited shading steps, authentic enlarged 96-128 pixel sprite aesthetic, clear face features at game scale. Isolated on genuine transparent background, no added scenery, objects, light bloom, gradients or UI.

### CanGuardPoses.png

> Create exactly six separate crisp 16-bit pixel game sprites of the approved reference boss. Genuine transparent background. Exact 3 columns by 2 rows, wide transparent margins and gutters, each entire figure isolated. Facing LEFT three-quarter in every cell. Preserve the recognizable indigo fur, giant pink pointed ears, broad pink round cheeks and belly with indigo crescent, thick lashes, red lips, blue collar and bronze bell, short sturdy legs, wide cat paws and thick striped tail. Shared scale and ground baseline, no resizing per pose. TOP LEFT low crouched charge anticipation, front paws tensed, TOP CENTER fast forward shoulder charge with low leading silhouette, TOP RIGHT sitting back dazed with sagging paws. BOTTOM LEFT airborne with both forepaws raised overhead and hind legs tucked, BOTTOM CENTER both open front paws down low in a heavy landing with flexed hind legs, BOTTOM RIGHT collapsed and lying on the ground with eyes closed, head left and tail right. No combat effects, stars, impact streaks, text, shadows, stage, grid, UI or scenery. Hard-edge SNES pixel clusters, visible paws, keep all ears and tails inside each cell.

### CanGuardRoom.png

> A wide 3:1 panoramic 16-bit pixel art background for a Unity side-scrolling industrial boss arena. Strict orthographic side view of a rooftop machine room at dawn. In the middle a massive muted violet boiler and pressure pipes; behind it tall rain-streaked industrial windows show a deep blue skyline and a little amber dawn light. On the far ends are steel ribs and warm amber maintenance lamps. Lower quarter is subdued dark muted navy behind gameplay, foreground geometry is drawn separately. Clearly separated chunky 16-bit pixel clusters, muted palette for a bright purple-and-pink character to read well, no characters, text, logo, UI, foreground floor, props that look interactive, borders or walkable platforms. Opaque full-bleed image.
