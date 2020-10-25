using Godot;
using System;

public class Player : ColorRect
{

    private Label player_name;

    public override void _Ready()
    {
        player_name = (Label)GetNode("Label");
    }

    public void init(string name, Color color)
    {
        player_name.Text = name;
        this.Color = color;
    }
}
