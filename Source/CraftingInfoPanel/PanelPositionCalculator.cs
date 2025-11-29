using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DuravoQOLMod.Source.CraftingInfoPanel;

/// <summary>
/// Stores an element's relative bounds and associated payload for hit testing.
/// </summary>
public struct PanelElement<TPayload>
{
    public Rectangle RelativeBounds;
    public TPayload Payload;

    public PanelElement(int relativeX, int relativeY, int width, int height, TPayload payload)
    {
        RelativeBounds = new Rectangle(relativeX, relativeY, width, height);
        Payload = payload;
    }
}

/// <summary>
/// Simple class for calculating panel dimensions and coordinate conversions.
/// Elements are placed at relative positions with payloads, panel tracks max extent,
/// then screen position is set for coordinate conversions and hit testing.
/// </summary>
/// <typeparam name="TPayload">The data type associated with each element (e.g., ItemID, SlotInfo)</typeparam>
public class PanelPositionCalculator<TPayload>
{
    private readonly List<PanelElement<TPayload>> elements = new();
    private int maxRight = 0;
    private int maxBottom = 0;

    public int Padding { get; }
    public int ScreenX { get; private set; }
    public int ScreenY { get; private set; }

    /// <summary>Calculated width including padding on both sides</summary>
    public int CalculatedWidth => Padding + maxRight + Padding;

    /// <summary>Calculated height including padding on both sides</summary>
    public int CalculatedHeight => Padding + maxBottom + Padding;

    /// <summary>All registered elements for iteration during drawing</summary>
    public IReadOnlyList<PanelElement<TPayload>> Elements => elements;

    public PanelPositionCalculator(int padding = 8)
    {
        Padding = padding;
    }

    /// <summary>
    /// Register an element at a relative position with its payload.
    /// Tracks extent for size calculation and stores for hit testing.
    /// Returns the element for convenience.
    /// </summary>
    public PanelElement<TPayload> AddElement(int relativeX, int relativeY, int width, int height, TPayload payload)
    {
        int right = relativeX + width;
        int bottom = relativeY + height;

        if (right > maxRight) {
            maxRight = right;
        }
        if (bottom > maxBottom) {
            maxBottom = bottom;
        }

        var element = new PanelElement<TPayload>(relativeX, relativeY, width, height, payload);
        elements.Add(element);
        return element;
    }

    /// <summary>
    /// Set the screen position of this panel (top-left corner including padding).
    /// Call this after comparing all panel heights to decide placement.
    /// </summary>
    public void SetScreenPosition(int screenX, int screenY)
    {
        ScreenX = screenX;
        ScreenY = screenY;
    }

    /// <summary>
    /// Convert a relative position to screen coordinates.
    /// Accounts for padding and screen position.
    /// </summary>
    public Vector2 RelativeToScreen(int relativeX, int relativeY)
    {
        return new Vector2(
            ScreenX + Padding + relativeX,
            ScreenY + Padding + relativeY
        );
    }

    /// <summary>
    /// Convert a relative position to screen coordinates as integers.
    /// </summary>
    public (int screenX, int screenY) RelativeToScreenInt(int relativeX, int relativeY)
    {
        return (ScreenX + Padding + relativeX, ScreenY + Padding + relativeY);
    }

    /// <summary>
    /// Convert screen coordinates to relative position within panel content area.
    /// </summary>
    public (int relativeX, int relativeY) ScreenToRelative(int screenX, int screenY)
    {
        return (screenX - ScreenX - Padding, screenY - ScreenY - Padding);
    }

    /// <summary>
    /// Check if a screen point is within this panel's bounds.
    /// </summary>
    public bool ContainsScreenPoint(int screenX, int screenY)
    {
        return screenX >= ScreenX
            && screenX < ScreenX + CalculatedWidth
            && screenY >= ScreenY
            && screenY < ScreenY + CalculatedHeight;
    }

    /// <summary>
    /// Check if a screen point is within the content area (excludes padding).
    /// </summary>
    public bool ContainsScreenPointInContent(int screenX, int screenY)
    {
        int contentLeft = ScreenX + Padding;
        int contentTop = ScreenY + Padding;

        return screenX >= contentLeft
            && screenX < contentLeft + maxRight
            && screenY >= contentTop
            && screenY < contentTop + maxBottom;
    }

    /// <summary>
    /// Hit test: find which element (if any) is under the screen point.
    /// Returns true if found, with the payload in the out parameter.
    /// </summary>
    public bool TryGetElementAtScreenPoint(int screenX, int screenY, out TPayload payload)
    {
        var (relativeX, relativeY) = ScreenToRelative(screenX, screenY);

        foreach (var element in elements) {
            if (element.RelativeBounds.Contains(relativeX, relativeY)) {
                payload = element.Payload;
                return true;
            }
        }

        payload = default!;
        return false;
    }

    /// <summary>
    /// Get the screen-space rectangle for an element by its relative bounds.
    /// </summary>
    public Rectangle GetElementScreenBounds(Rectangle relativeBounds)
    {
        return new Rectangle(
            ScreenX + Padding + relativeBounds.X,
            ScreenY + Padding + relativeBounds.Y,
            relativeBounds.Width,
            relativeBounds.Height
        );
    }

    /// <summary>
    /// Get the panel bounds as a screen-space Rectangle.
    /// </summary>
    public Rectangle GetScreenBounds()
    {
        return new Rectangle(ScreenX, ScreenY, CalculatedWidth, CalculatedHeight);
    }

    /// <summary>
    /// Get the content area bounds (excluding padding) as a screen-space Rectangle.
    /// </summary>
    public Rectangle GetContentScreenBounds()
    {
        return new Rectangle(ScreenX + Padding, ScreenY + Padding, maxRight, maxBottom);
    }

    /// <summary>
    /// Clear all elements. Use when rebuilding the panel layout.
    /// </summary>
    public void Clear()
    {
        elements.Clear();
        maxRight = 0;
        maxBottom = 0;
    }
}