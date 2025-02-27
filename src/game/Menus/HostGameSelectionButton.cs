using Godot;
using System;

public class HostGameSelectionButton : PanelContainer
{

    //=================================================================

    public CheckButton toggle_button;
    public TextureRect role_texture;
    public Label role_name;
    public RichTextLabel team_name;
    public SpinBox role_amount;

    //=================================================================

    public CardDatabase.roles role {get; private set;}

    public bool initialised {get; protected set;} = false;
    private HostGame parent;

    public int current_amount {get; private set;} = 0;
    private int maximum_amount = 9999;
    public bool role_enabled {get; private set;} = false;

    //=================================================================

    public override void _Ready()
    {
        toggle_button = (CheckButton)GetNode("HBoxContainer/ToggleRoleButton");
        role_texture = (TextureRect)GetNode("HBoxContainer/RoleTextureData");
        role_name = (Label)GetNode("HBoxContainer/VBoxContainer/RoleNameData");
        team_name = (RichTextLabel)GetNode("HBoxContainer/VBoxContainer/RoleTeamData");
        role_amount = (SpinBox)GetNode("HBoxContainer/RoleAmountData");

        toggle_button.Pressed = false;
    }


    public void initialise(HostGame parent, CardDatabase.roles role)
    {
        this.parent = parent;
        this.role = role;

        CardDatabase cdb = new CardDatabase();
        CardDatabase.card_data data = cdb.role_data[role];



        role_texture.Texture = (Texture)GD.Load(data.card_art);

        role_name.Text = data.card_name;
        team_name.BbcodeText = cdb.team_data[data.card_team];

        maximum_amount = data.role_max_amount;
        if (maximum_amount <= -1)
        {
            role_amount.MaxValue = 1000;
        }
        else
        {
            role_amount.MaxValue = maximum_amount;
            role_amount.Suffix = $"/{maximum_amount}";
        }
        
        toggle_button.Connect("toggled", this, "_toggle_button_toggled");
        role_amount.Connect("value_changed", this, "_role_amount_changed");

        initialised = true;
    }

    public void iterate_amount()
    {
        current_amount++;
        role_amount.Value = current_amount;
    }
    public void set_button_pressed(bool pressed)
    {
        toggle_button.Pressed = pressed;
        role_enabled = pressed;
    }

    //===============================================================
    //Connected functions
    private void _role_amount_changed(float value)
    {
        current_amount = (int)value;
        parent.update_stats();
    }

    private void _toggle_button_toggled(bool value)
    {
        set_button_pressed(value);
        parent.update_stats();
    }

    //===============================================================



}
