# 游戏结局

击败罐头守卫并拾取最后一罐，约 0.8 秒后会自动进入结局。也可以直接在 Unity 打开 `Assets/Game/Scenes/EndingScreen.unity` 预览。

- 「再玩一次」从第一关开始，对应 Enter 或手柄南键。
- 「返回标题」回到跳舞的开始界面，对应 Esc 或手柄东键。
- 角色倒地后，关卡显示失败界面。R 或手柄南键重试当前关，T 或手柄东键返回标题。

结局使用原创的轻柔合成器循环 `Assets/Game/Audio/EndingDawn.wav`。**Orange Cat → Build Ending Screen** 可重建结局场景。

结局背景使用内置 imagegen 生成。源图位于 `ArtSource/PixelPreview/EndingDawn.png`，预览位于 `docs/EndingScreen.png`。

> Full-bleed 16:9 pixel art ending scene for Orange Cat Breakout, matching the attached title dock background and established cat designs. At a quiet industrial loading dock just after a long night of rain, sunrise fills the right side with warm golden light reflected on wet planks. On the right between x=57% and x=91% of canvas, the two cat protagonists are clearly shown full-body seated side by side, sharing a moment of quiet victory. The orange round-faced folded-ear tabby HaoJu has a teal scarf and striped tail, gently holds a single small steel food can with amber label in both front paws at lap height, and looks toward his friend. The taller cream pointed-ear cat JiangZhua wears a muted green collar and little bell, and leans lightly toward HaoJu, relaxed with eyes half-closed. Use authentic chunky crisp 16-bit pixels, cohesive hand-placed outlines and cat proportions, seated pose with readable paws and tails. Their heads and can are a major focal point. Left 44% of canvas is a deliberately quiet dark navy-to-teal shaded dock interior for later UI, minimal high contrast detail, scenery continuous across image. On right distant industrial rooftops and calm peach dawn, no active rain. Preserve visual connection to the title, with steel beams above, a wet platform at about 78% down and warm reflection below. No lettering, typography, UI, watermarks, logos, borders, additional characters or duplicate cans. Render one final landscape image, opaque.
