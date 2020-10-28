using Godot;
using System;

public class GUI : CanvasLayer
{
    [Signal]
    public delegate void TextSubmitted(string message);

    //=====================================================================

    private Control first_node;

    private Label original_role_node;
    private Label original_team_node;
    private Label current_turn_node;

    private Label game_help_node;
    private Label game_chat_node;
    private LineEdit text_enter_node;

    //=====================================================================

    private Timer timer;
    private Label time_left_node;
    private int time_remaining;

    //=====================================================================

    public override void _Ready()
    {
        first_node = (Control)GetChild(0);

        original_role_node = (Label)FindNode("OGRoleData");
        original_team_node = (Label)FindNode("OGTeamData");
        current_turn_node = (Label)FindNode("CurrentTurnData");

        game_help_node = (Label)FindNode("GameInfo");

        game_chat_node = (Label)FindNode("GameChatNode");
        text_enter_node = (LineEdit)FindNode("TextEnterNode");
        text_enter_node.Connect("text_entered", this, "_text_submitted");

        timer = (Timer)GetNode("Timer");
        time_left_node = (Label)FindNode("TimeLeft");
        timer.Connect("timeout", this, "_on_timer_timeout");
    }

    //=====================================================================

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

    public void set_game_help(string new_text)
    {
        game_help_node.Text = new_text;
    }

    public void add_game_help(string new_text)
    {
        game_help_node.Text += $"\n{new_text}";
    }


    public void set_visible(bool value)
    {
        first_node.Visible = value;
    }

    public void start_timer(int start_time)
    {
        time_remaining = start_time;
        time_left_node.Text = $"{time_remaining}";
        timer.Start();
    }

    private void _on_timer_timeout()
    {
        time_remaining--;
        time_left_node.Text = $"{time_remaining}";

        if (time_remaining == 0)
        {
            timer.Stop();
        }
    }

    public void add_message(string sender, string message)
    {
        game_chat_node.Text  += $"\n{sender}: {message}";
    }

    public void add_message(string message)
    {
        game_chat_node.Text  += $"\n{message}";
    }

    private void _text_submitted(string message)
    {
        text_enter_node.Clear();
        EmitSignal(nameof(TextSubmitted), message);
    }

    public void set_text_submission(bool value)
    {
        text_enter_node.Visible = value;
    }

}
