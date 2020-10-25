using Godot;
using System;

public class GUI : CanvasLayer
{
    private Control first_node;

    private Label original_role_node;
    private Label original_team_node;
    private Label current_turn_node;

    public override void _Ready()
    {
        first_node = (Control)GetChild(0);

        original_role_node = (Label)GetNode("Control/VBoxContainer/PanelContainer/GridContainer/OGRoleData");
        original_team_node = (Label)GetNode("Control/VBoxContainer/PanelContainer/GridContainer/OGTeamData");
        current_turn_node = (Label)GetNode("Control/VBoxContainer/PanelContainer2/VSplitContainer/CurrentTurnData");   
    }

    public void set_current_turn(string new_turn)
    {
        current_turn_node.Text = new_turn;
    }

    public void set_original_role(string new_role)
    {
        original_role_node.Text = new_role;
    }

    public void set_original_team(string new_team)
    {
        original_team_node.Text = new_team;
    }


    public void set_visible(bool value)
    {
        first_node.Visible = value;
    }

}
