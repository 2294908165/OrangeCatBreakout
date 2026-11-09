# 标题界面

在 Unity 打开 `Assets/Game/Scenes/TitleScreen.unity` 后点 Play。正式构建会先进入这个场景。点「开始游戏」，或按 Enter、空格、手柄南键，进入第一关。右上角的音乐开关也可以用 M 操作。

舞姿参考用户提供的视频。哈橘摆身点爪，姜爪抬爪摆动。标题场景使用用户提供的 MP3 `Assets/Game/Audio/TitleDance.mp3`，循环播放。舞步读取音频采样位置，静音时仍跟拍。第一关加载后，标题音乐随场景卸载。

**Orange Cat → Build Title Screen** 可重新生成场景。排版图和按钮文字位于 `Assets/Game/Art/PixelPreview/Title`。

## 图像来源

内置 imagegen 生成的源图保存在 `ArtSource/PixelPreview`。

### TitleDance.png

> Create exactly eight full-body 16-bit SNES pixel game sprite frames in a clean 4-column 2-row atlas. First input is the established characters, second input is a contact sheet of the reference dance. Use its body acting only. TOP ROW only the orange folded-ear round-faced tabby HaoJu with teal scarf, cream muzzle, padded paws and ringed tail. BOTTOM ROW only the cream pointed-ear cat JiangZhua with green collar and little bell, leaner and longer-legged. In every cell both feet, ears and entire tail stay within the cell, wide clear gutters, same apparent body scale, common foot baseline, cats upright on hind legs, front three-quarter toward viewer. Four DISTINCT sequential dance keyframes per row. HaoJu alternates compact sideways body sway with small paw taps: first upright with paws forward, then rock left and tap one bent paw across chest, then rock right and tap the other bent paw outward, then dip knees with both paws tucked. JiangZhua uses lively one-paw raises like the video: begin shoulders angled and both paws low, second raise one paw high above ear with other paw low, third turn and sweep the other paw sideways, fourth bend knees with one elbow raised and one paw near chest. Friendly impassive cat faces, no human bodybuilder anatomy, no clothing beyond their accessories. Crisp chunky pixel clusters, warm outlines, vivid but restrained palettes, true alpha transparent, no text, shadows, scenery, effects or gridlines.

### TitleBackdrop.png

> Full bleed 16:9 16-bit pixel art title-screen environment for an original game about two cat heroes stealing a can from a rainy industrial warehouse. Title UI will be added separately on left, so the left 45 percent is quiet low contrast almost-black navy indigo with sparse mist and subtle factory silhouettes. On the right a small abandoned outdoor loading dock turned cozy dance stage, with an unobstructed flat side-scrolling floor whose top aligns 76 percent down the image, a warm hanging industrial lantern above, teal and amber accent lights, wet steel beams and distant rain-blue roof silhouettes. Soft dusk skyline glow on far right, no bright objects behind the left UI, top and bottom full bleed. Visually sophisticated cinematic composition, hard-edged chunky SNES pixel clusters, late-90s platformer look, deep navy, petrol teal, golden amber, desaturated plum. No characters, no cats, no text, no logos, no buttons, no props on the central right platform in front of the dancing cats, no borders.
