using Godot;
using System;

public class PauseMenu : PanelContainer
{
    public override void _Ready()
    {
        Visible = false;
    }

    public override void _Process(float delta)
    {
        if (Visible)
        {
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                Visible = false;
            }
        }
        else
        {
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                Visible = true;
            }
        }
    }

    private void _on_ResumeButton_pressed()
    {
        Visible = false;
    }

    private void _on_QuitButton_pressed()
    {
        if (GetTree().NetworkPeer is NetworkedMultiplayerENet enet)
        {
            enet.CloseConnection();
            GetTree().NetworkPeer = null;
        }
        GetTree().ChangeScene("res://src/game/Menus/MainMenu.tscn");
    }
}