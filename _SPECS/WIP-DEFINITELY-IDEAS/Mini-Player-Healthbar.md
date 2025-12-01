TODO: write a spec for a small character health/mana bar that appears just BELOW the character's feet when meeting certain conditions (this is akin to the healthbar that appears above the head in ARPGS like diablo and POE, but for terraria it will be better below the character).... but auto-hides when not necessary... goal is to make it easier to see dmg at a glance when you are in trouble, without having to look all the way at the upper right corner OR have a mod that puts a giant healthbar in the middlebottom of your screen.. the conditions for activating it might be "health < 50% or 4 hearts", recent incoming dmg > 25% of health.....

the health bar will match the native npc healthbars, in their pixel art style

- "pilll inner background"
- "current available health" (green -> yellow -> red based on current % health) (slightly "bigger" than the inner pill background)
- "recently lost health" (darker version of current color) an animated shrinking of the healthbar that helps communicating how big that recent hit was
- "shield" (blue) if an armor shield is active. if health is at 100% this should overflow "beyond" the 100% width of the healthbar


The health bars you see under enemies (and the "Classic" style player health bar) are **not** single static sprites. They are procedurally drawn using two very small, generic textures that are stretched and colored by the code at runtime.

### 1. The Built-in Textures

The game uses two specific assets found in `Images/UI/`. In `tModLoader`, you access them via `TextureAssets`.

* **The Fill (`HB1`)** :
* **Internal Name:** `TextureAssets.Hb1` (loads `Images/UI/HB1`)
* **Description:** This is a small, grayscale (white) rectangle.
* **Function:** This acts as the "fill" of the bar. Because it is white, the game can tint it any color (Green, Yellow, Red) using `Color` parameters in the `SpriteBatch.Draw` call.
* **The Border/Background (`HB2`)** :
* **Internal Name:** `TextureAssets.Hb2` (loads `Images/UI/HB2`)
* **Description:** This is slightly larger and darker.
* **Function:** This is drawn *behind* the fill to create the dark grey border and background.

### 2. How It Is Drawn (`Main.DrawHealthBar`)

The logic is contained in `Main.DrawHealthBar`. It doesn't just "paste" a health bar image; it constructs it every frame.

Here is the pseudo-logic of what happens when that Blue Slime is drawn:

1. **Calculate Size:** The game decides a width (usually around 36 pixels for enemies) and calculates the fill width based on `npc.life / npc.lifeMax`.
2. **Determine Color:** The game calculates the color based on the percentage:
   * **> 60%:** R = 0, G = 255 (Green)
   * **< 60%:** It interpolates from Green to Yellow to Red.
3. **Draw Background (HB2):**
   * It draws `TextureAssets.Hb2` at the entity's position, stretched to the full width of the bar.
4. **Draw Fill (HB1):**
   * It draws `TextureAssets.Hb1`  *on top* , but sets the `destinationRectangle` width to the calculated fill width.
   * Crucially, it passes the **Calculated Color** (e.g., bright green) into the draw call, which tints the white `HB1` texture.

To match the vanilla "World Space" health bar style (the one under enemies), you need to replicate three things: the  **Texture Layering** , the **Color Interpolation** (Green **→** Yellow **→** Red), and the  **World-to-Screen Positioning** .

Here is the exact recipe to get that "Vanilla Feel."

### 1. The Color Logic (The "Traffic Light" Algo)

Vanilla doesn't just fade from Green to Red. It passes through a bright Yellow.
Here is a helper method to replicate that exact transition based on `current / max` health:

**C#**

```
public Color GetHealthColor(float current, float max)
{
    float percent = current / max;

    // 1. High Health (Above 60%): Stay purely Green
    // In vanilla, it actually stays green for a while before fading to yellow.
    if (percent > 0.6f) 
    {
        return new Color(0, 255, 0); // Pure Green
    }
  
    // 2. Medium Health (30% - 60%): Fade Green -> Yellow
    if (percent > 0.3f)
    {
        // Normalize 0.3-0.6 to 0.0-1.0
        float p = (percent - 0.3f) / 0.3f; 
        return Color.Lerp(Color.Yellow, Color.Lime, p);
    }

    // 3. Low Health (0% - 30%): Fade Yellow -> Red
    // Normalize 0.0-0.3 to 0.0-1.0
    float p2 = percent / 0.3f;
    return Color.Lerp(Color.Red, Color.Yellow, p2);
}
```

### 2. The Implementation (ModSystem)

Using `ModSystem.PostDrawInterface` is usually easiest for UI, but if you want it to feel like it's "in the world" (behind trees, affected by lighting slightly, or just sticking to the player strictly), you actually want to draw it during the **Entity Drawing** phase or just calculate the screen position carefully.

Here is a `PostDrawInterface` approach that locks to the player's position, which is the most stable way to do it without jitter.

**C#**

```
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent; // For TextureAssets
using Terraria.ModLoader;
using Terraria.UI;

public class PlayerWorldHealthBar : ModSystem
{
    public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
    {
        // Insert our layer right after the Vanilla Entity Health Bars
        // so it renders on top of enemies if they overlap, but under the main UI.
        int resourceIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Entity Health Bars"));
      
        if (resourceIndex != -1)
        {
            layers.Insert(resourceIndex + 1, new LegacyGameInterfaceLayer(
                "MyMod: PlayerUnderBar",
                delegate {
                    DrawPlayerHealthBar();
                    return true;
                },
                InterfaceScaleType.Game)
            );
        }
    }

    private void DrawPlayerHealthBar()
    {
        Player player = Main.LocalPlayer;
        if (player.dead || player.ghost) return;

        // 1. Calculate Screen Position
        // World Position + Offset - Camera Position
        // We put it 10 pixels below the player's feet
        Vector2 worldPos = new Vector2(
            player.Center.X, 
            player.position.Y + player.height + 10 
        );
        Vector2 screenPos = worldPos - Main.screenPosition;

        // 2. Get Textures
        Texture2D textureBackground = TextureAssets.Hb2.Value; // The dark border
        Texture2D textureFill = TextureAssets.Hb1.Value;       // The white fill

        // 3. Calculate Dimensions
        // Vanilla enemy bars are roughly 36 pixels wide.
        int barWidth = 36; 
        int barHeight = textureBackground.Height; // Keep vanilla height

        // 4. Calculate Fill
        float quotient = (float)player.statLife / player.statLifeMax2;
        quotient = MathHelper.Clamp(quotient, 0f, 1f);
      
        int fillWidth = (int)(barWidth * quotient);

        // 5. Centering Logic
        // We need to center the bar horizontally relative to the player
        Vector2 drawOrigin = new Vector2(barWidth / 2, barHeight / 2);
        Vector2 drawPos = screenPos; 
        // Note: We don't subtract origin here because we use the destination rect for centering logic below, 
        // but for spritebatch rotation/origin it is easier to just offset the rect X/Y.

        // 6. Draw Background (HB2)
        // We draw it centered on 'screenPos'
        Rectangle bgRect = new Rectangle(
            (int)(screenPos.X - barWidth / 2), 
            (int)(screenPos.Y), 
            barWidth, 
            barHeight
        );
      
        // Lighting: If you want it to glow like UI, use Color.White.
        // If you want it affected by darkness, use Lighting.GetColor(...)
        // Vanilla health bars usually 'pop' so we use pure white for the border opacity.
        Main.spriteBatch.Draw(textureBackground, bgRect, Color.White);

        // 7. Draw Fill (HB1)
        // The fill needs to be left-aligned within the background, but the whole group is centered.
        // We assume HB1 has a 2-pixel transparent padding or similar in vanilla, 
        // but usually you just draw it directly over with a slight offset if needed.
        // Vanilla HB2 actually has about 2 pixels of padding on the left.
        int paddingX = 2; 
        int paddingY = 2; // rough guess, adjust to taste

        Rectangle fillRect = new Rectangle(
            bgRect.X + paddingX,
            bgRect.Y + paddingY,
            fillWidth - (paddingX * 2), // Shrink fill to fit inside border
            barHeight - (paddingY * 2)
        );

        // Get the Traffic Light Color
        Color fillColor = GetHealthColor(player.statLife, player.statLifeMax2);
      
        // Draw the fill
        Main.spriteBatch.Draw(textureFill, fillRect, fillColor);
    }
  
    private Color GetHealthColor(float current, float max)
    {
        float percent = current / max;
        if (percent > 0.6f) return new Color(0, 255, 0); // Vanilla Green
        if (percent > 0.3f) return Color.Lerp(Color.Yellow, Color.Lime, (percent - 0.3f) / 0.3f);
        return Color.Lerp(Color.Red, Color.Yellow, percent / 0.3f);
    }
}
```

### 3. Key Thematic Details to Note

1. **HB2 (Background)** is larger than  **HB1 (Fill)** . You must pad the fill drawing slightly (roughly 2-4 pixels depending on scale) so the dark border of HB2 is visible surrounding the color.
2. **Floats vs Ints:** Cast to `int` for your `Rectangle` coordinates. If you leave them as floats in a Vector2 draw call without snapping, the bar might "shimmer" as the player moves because of sub-pixel rendering.
3. **Lighting:** Vanilla Enemy health bars are **fully bright** (they ignore the darkness of the cave). If you want that, use `Color.White` for the background and your calculated color for the fill. If you want the bar to be shadowed by the cave darkness, use `Lighting.GetColor((int)player.Center.X / 16, (int)player.Center.Y / 16)` and multiply your colors by that.
