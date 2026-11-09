# 第三关 · 装卸升降台

第二关从装卸口撤离后按 **E**，保留当前角色进入第三关。也可以直接在 Unity 打开 `Assets/Game/Scenes/LoadingBay03.unity` 后点 Play。

入口先经过安全练习台，随后用中间的升降台或二段跳跨过地刺床。过了地刺后会遇到一只笑肚怪，可以交战，也可以绕开。走到右侧琥珀色按钮旁按 **E**，等货梯下降，跳上货梯并站稳约 0.36 秒。站在货梯上抵达最高点后，约 0.8 秒自动进入第四关屋顶检修区，也可以按 **E** 立即继续。货梯到顶时会短暂等候还在空中的乘客。按 **R** 重试当前关。

升降台使用单向实体碰撞，角色站上去会随它移动，起跳保留当时的升降速度。关卡可用 **Orange Cat → Build Loading Bay 03** 重建。仅更新敌人时可用 **Orange Cat → Update Loading Bay Enemy**。背景源图位于 `ArtSource/PixelPreview/LoadingBayBackdrop.png`，由内置 imagegen 生成。

Unity 6000.3.28f1 编译、构建和相机渲染成功，PlayMode 48/48 通过。验证覆盖乘台上升、离台起跳、远处无法呼叫货梯、第二关带当前角色续关，以及哈橘通过实际按键跨过地刺、绕开两只存活敌人并登梯通关。证据在 `TestResults/LoadingBay03`。平台节拍和预警可读性仍需人工试玩。

自动续关更新后，PlayMode 52/52、EditMode 2/2 通过。回归测试从第一关开始，缩短前两关战斗后触发原有出口，使用键盘通关第三关并确认自动加载第四关。另一个用例验证到顶前跳起后落回踏板仍能完成撤离。记录位于 `TestResults/CargoAdvance`。

## 笑肚怪

笑肚怪在近身时先亮出地面预警，再向前挺肚一小步，命中造成 1 点伤害。受到攻击会取消肚顶，先后仰并显示火花，接着在两张捧腹帧之间交替。生命耗尽时切到倒地帧。

待机时保持侧脸，双手搭在腹部。玩家进入正前方 4.5 units、且中间没有地形遮挡时，笑肚怪沉身抬掌，持续转向已发现的玩家。招架至少维持 0.24 秒，随后只有进入近身范围才会触发肚顶预警。离开视野会恢复待机。招架期间仍可被玩家击中并触发捧腹反馈。

侧脸与招架更新后，PlayMode 55/55、EditMode 2/2 通过。验证覆盖背后接近、地形遮挡、正面入视野、转向追踪及离开视野后恢复待机。记录位于 `TestResults/BellyStances`，场景相机预览位于 `docs/BellyLaughStances.png`。

Unity 编译、第三关场景更新和相机渲染成功，PlayMode 54/54、EditMode 2/2 通过。用例验证了旧敌人替换、肚顶伤害、受击打断、捧腹帧、倒地以及绕过敌人乘梯进入第四关。测试记录位于 `TestResults/BellyLaugh`。

两份源图使用内置 imagegen 生成，保存在 `ArtSource/PixelPreview`。

### BellyLaughReactions.png

> Create one four-frame character design and hit-reaction sprite sheet for a colorful 2D side-scrolling 16-bit pixel action game. Reference image 1 is the neutral character anatomy and palette. Reference image 2 is the exact emotional and body-acting reference for the belly laugh on damage. Faithfully draw a plump golden yellow pear-shaped creature with a smooth rounded head blending straight into the shoulders, two protruding lime-green eyes with small black pupils, a small flat mouth, a very large oval pale cream belly, long soft arms with charcoal-gray fingertips, and thick tapered golden legs. No ears, nose, fur, clothes, tail, weapons or accessories. Its expression should be odd, deadpan and funny in neutral. Precisely four separated full-body figures in a spacious 2-by-2 grid, each wholly inside its own cell with generous transparent gutters, consistent scale and foot baseline, facing left in three-quarter view. TOP LEFT neutral standing, relaxed arms at the sides, belly prominent, green eyes open. TOP RIGHT one beat of actual hit recoil, shoulders jerk back and torso compresses slightly, arms flinch inward, eyes squeeze, one foot braced. BOTTOM LEFT peak belly laugh, bends forward at waist, both gray hands firmly clasp the lower belly, eyes are squeezed into closed happy arcs and the wide open laughing mouth clearly shows a few white upper teeth; looks like the reference's involuntary doubled-over laughter. BOTTOM RIGHT follow-through a beat later, both hands still holding the belly, knees spring back up slightly and mouth is still open laughing, a distinct rebound silhouette rather than a duplicated pose. Natural coherent animation timing, each drawing has the exact same identity and proportions. Crisp chunky native SNES game sprite clusters, limited warm honey and yellow palette, dark warm outlines, clean readable silhouettes at 72–90 gameplay pixels high. Genuine transparent background. No motion streaks, text, labels, grids, stars, hit flashes, cast shadows, floor, UI or scenery.

### BellyLaughActions.png

> Create one transparent three-frame 16-bit pixel game sprite strip, exactly THREE isolated full-body figures in a single horizontal row with wide empty gutters and margins. Use only the golden yellow pear-shaped green-eyed monster shown in the attached approved game sprite sheet, preserve its simple round seamless head and shoulders, two protruding lime eyes, small horizontal mouth, huge pale cream belly, gray fingertips, soft wide body and short tapered legs. Match its chunky native SNES pixel clusters, limited honey yellow palette, and deep warm outline. All figures face left three-quarter, share one consistent physical scale and foot baseline. LEFT FRAME charge anticipation: weight pulls back, heels brace, elbows pulled backwards and stomach drawn slightly in, green eyes focus forward. CENTER FRAME distinct short belly-bump attack: thrust the enormous cream belly out in front toward the left, entire body leans backward and dark hands sweep behind the hips, front foot stepping, same little focused face, very clear belly-led silhouette. RIGHT FRAME defeated: same monster lying visibly horizontal on its side on the floor facing left with eyes closed, large round belly, arms and legs relaxed, gently comic, no injury. Keep all feet and hands completely within their respective cells. True alpha transparency, no text, border, floor, shadows, lights, impact flashes, accessories, extra figures or environment.

### BellyLaughStances.png

内置 imagegen 生成，工程使用右侧招架帧。

> Make exactly two separated full-body sprites side-by-side for the same golden yellow green-eyed pear-shaped monster. Image one specifies the idle pose; image two specifies the guard pose; image three is only the established in-game sprite identity and chunky SNES pixel art style. Both frames face LEFT three-quarter, mirror the second photo's pose to face left. Ignore screenshot chrome. Left cell: tall relaxed idle, both olive-brown hands resting on opposite sides of the lower cream belly, legs naturally apart with long rounded olive-brown toes, calm green eyes and straight tiny mouth. Right cell: exactly the martial-arts guard of image two, wide low stance with visibly bent knees, front arm extended left and up with clearly open upright olive-brown palm facing outward, rear elbow bent and rear closed hand held near the cream belly, upright alert head with same green eyes and straight mouth. The body retains same volume, color and head size throughout, feet on aligned ground baseline, each complete figure fully contained with wide transparent margins and gutter. Hard crisp pixel clusters, clean warm dark outline, warm mustard/yellow shading and muted cream belly. Neither sprite has scenery, floor, shadows, effects, text, accessories, or UI. Genuine alpha transparency.

### BellyLaughProfileIdle.png

内置 imagegen 生成，工程使用这张贴近图一的侧脸待机帧。

> Draw ONE isolated full-body LEFT-FACING pixel-art sprite matching the first reference photo extremely closely, especially the face angle. The first photo is authoritative for anatomy, gaze and pose. The second sheet is only for the warm 16-bit sprite palette and pixel outlines. The camera sees a strong side view, about 75 degrees away from straight-on. The nose-less short mouth line lies at the LEFT silhouette, on a subtly projecting muzzle; the round forehead and rear of the head extend to the right. Show only ONE full round near-side lime-green eye with a forward-looking dark pupil, and just a very NARROW DARK SLIVER of the far eye occluded behind the left facial contour, never two equally readable green discs. The creature is looking at someone off-screen left, not toward the camera. Reproduce the long neck seamlessly tapering into the broad shoulders, big pale cream belly, both olive-brown hands resting on opposite sides of the belly, tapered thick legs and long olive toes, calm blank expression. Hard clustered SNES pixel art, deep warm outline, matching the reference sprite style. Entire sprite visible on actual transparent background, no screenshot elements, shadows, scene, text or effects.

## 最终背景生成提示词

> Generate a 3:1 panoramic BACKGROUND ONLY for level three of this original 2D SNES-style pixel action game. Use reference only for restrained crisp pixel technique, not its cold room. Subject: night-time industrial freight loading dock viewed strictly orthographic side-on. Left sector simple warm sodium lamps and dark loading warehouse panels, middle open freight yard backed by distant rain-blue rooftops and industrial support beams, right sector a clearly visible tall lift shaft outlined by amber indicator lamps and a steel loading tower, with no elevator car. Warm amber, weathered charcoal teal, navy night. Aligned vertical structural silhouettes and a dark clean low-contrast band behind the gameplay. Lower quarter is an empty dark wall-base area so a separate game floor can be drawn. No drawn ground, stairs, walkable platforms, spikes, elevator car, props that look interactive, characters, lettering, logos, UI, border or perspective tilt. Full bleed, readable coarse 16-bit pixel clusters. Background only.
