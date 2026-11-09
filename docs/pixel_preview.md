# 像素风可玩预览

在 Unity 的 Project 面板打开 `Assets/Game/Scenes/PixelPreview.unity`，点击 Play。HUD 上可以切换哈橘和姜爪，F1 展开战斗调试数据。重新生成时，在退出 Play 后执行 **Orange Cat → Build Pixel Preview**。该菜单重新生成 PixelPreview 场景、像素 Prefab 及对应导入纹理，正式美术制作可另存资产来保留手改内容。

- `ArtSource/PixelPreview/Cats.png` 为透明双角色图集，`RainWarehouse.png` 为独立背景图。两张源图使用内置 imagegen 生成，均参考最终的手绘双角色设计稿。
- 构建器按左右半幅切分、裁掉透明边、将角色规格统一至 32 PPU，再按脚掌中心设置底部锚点。哈橘 42 像素高，姜爪 46 像素高。图像使用 Point 过滤和无压缩导入。地面、平台和两个训练物件采用单独的像素纹理。
- Unity 6000.3.28f1 批处理编译、场景构建和 PlayMode 测试均成功。25/25 项通过，含加载新场景、两只角色的 Sprite 与锚点、切换和实际攻击扣血。证据位于 `TestResults/PixelPreview`。
- `PixelPreview.png` 由 Unity 相机渲染。截图里为了对照摆放两只角色，实际试玩通过 HUD 切换。后续已加入 [像素帧动画](pixel_motion.md)。

## 内置 imagegen 最终提示词

**Cats.png**

> Make a single Unity pixel-art sprite atlas PNG for the two playable cats in the top strip of the reference. Exactly two separate full-body sprites on genuine transparent background, side-by-side inside the left and right halves, very wide transparent gutter separating them, never touching the midpoint. Same common foot baseline near bottom. Left sprite HaoJu: stocky but balanced small orange biped cat with cute round head, small folded ears, tabby stripes and teal scarf, normal size hips and striped tail. Right sprite JiangZhua: slim cream cat with upright ears, green collar and silver bell, in natural guarded posture. Both face right in platformer ready stance with feet planted. Faithful to existing silhouettes and identity. Crisp 16-bit 2D SNES game pixel art, effective ~48 game pixels tall each, hard square pixel clusters with limited palette, no soft edges, no gradients. No environmental details, no text, no shadows, no outlines outside silhouette, no extra poses. Every paw and both tails fully visible with generous transparent margin. These must be individual sprite silhouettes suitable to slice on the exact middle of the image. Landscape 3:2 canvas.

**RainWarehouse.png**

> Generate just the SCENERY BACKGROUND in pixel art for a playable 2D side-scrolling cat game. Base it on the lower warehouse background of the reference. Very wide landscape side-on orthographic 16-bit SNES pixel art, useful panorama. No cats, no foreground crates or items, no HUD and no lettering. Low contrast cool navy warehouses with corrugated shutters in the midground and atmospheric rain haze and roof silhouettes, spaced subdued amber lamps. Keep the bottom third visually quiet dark cool slate wall base, since an independent playable ground is added there in Unity. Nothing should look like a platform. Clean repeating large shapes, coarse pixel blocks like 480x160 logical art upscaled, restrained palette, good readability of bright characters in front of it. Entire frame full bleed. No border.
