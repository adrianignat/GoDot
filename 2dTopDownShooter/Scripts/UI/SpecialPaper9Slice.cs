using Godot;
using System.Collections.Generic;

public partial class SpecialPaper9Slice : Control
{
    [Export] public TextureRect TopLeft;
    [Export] public TextureRect Top;
    [Export] public TextureRect TopRight;
    [Export] public TextureRect Left;
    [Export] public TextureRect Center;
    [Export] public TextureRect Right;
    [Export] public TextureRect BottomLeft;
    [Export] public TextureRect Bottom;
    [Export] public TextureRect BottomRight;

    public override void _Ready()
    {
        LayoutPaper();
        Resized += LayoutPaper;
    }

    private void LayoutPaper()
    {
        Vector2 size = Size;

        float cornerW = TopLeft.Texture.GetSize().X;
        float cornerH = TopLeft.Texture.GetSize().Y;

        // --- Top row
        TopLeft.Position = Vector2.Zero;
        TopLeft.Size = new Vector2(cornerW, cornerH);

        Top.Position = new Vector2(cornerW, 0);
        Top.Size = new Vector2(size.X - cornerW * 2, cornerH);

        TopRight.Position = new Vector2(size.X - cornerW, 0);
        TopRight.Size = new Vector2(cornerW, cornerH);

        // --- Middle row
        Left.Position = new Vector2(0, cornerH);
        Left.Size = new Vector2(cornerW, size.Y - cornerH * 2);

        Center.Position = new Vector2(cornerW, cornerH);
        Center.Size = new Vector2(
            size.X - cornerW * 2,
            size.Y - cornerH * 2
        );

        Right.Position = new Vector2(size.X - cornerW, cornerH);
        Right.Size = new Vector2(cornerW, size.Y - cornerH * 2);

        // --- Bottom row
        BottomLeft.Position = new Vector2(0, size.Y - cornerH);
        BottomLeft.Size = new Vector2(cornerW, cornerH);

        Bottom.Position = new Vector2(cornerW, size.Y - cornerH);
        Bottom.Size = new Vector2(size.X - cornerW * 2, cornerH);

        BottomRight.Position = new Vector2(size.X - cornerW, size.Y - cornerH);
        BottomRight.Size = new Vector2(cornerW, cornerH);
    }
}
