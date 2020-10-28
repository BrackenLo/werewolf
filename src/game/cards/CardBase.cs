using Godot;
using System;
using System.Collections.Generic;

public class CardBase : PanelContainer
{
    [Signal]
    public delegate void CardSelected(CardBase card);
    [Signal]
    public delegate void CardUnselected(CardBase card);

    //=================================================================================

    private RichTextLabel role_name;
    private TextureRect role_image;
    private RichTextLabel role_team;

    private Panel card_back;

    private Tween tween;
    private AnimationPlayer anim;
    private Area2D rotating_mouse_area;
    private Area2D card_mouse_area;

    private Particles2D particle_1;
    private Particles2D particle_2;

    //=================================================================================

    public int playspace_position {get; private set;}
    public bool card_loaded {get; private set;} = false;
    private CardDatabase.roles card_role;

    //=================================================================================

    public bool card_visible {get; private set;} = false;
    public bool card_selectable = false;
    public bool card_selected {get; private set;} = false;

    private Color card_selected_color = new Color(1, 0.88F, 0.35F, 1);
    private Color card_default_color = new Color(1, 1, 1, 1);

    //=================================================================================

    public Vector2 locked_position {get; private set;} = new Vector2();
    public float locked_rotation {get; private set;} = 0;

    //=================================================================================

    public override void _Ready()
    {
        role_name = (RichTextLabel)GetNode("VBoxContainer/CardRoleData");
        role_image = (TextureRect)GetNode("VBoxContainer/CardImageData");
        role_team = (RichTextLabel)GetNode("VBoxContainer/PanelContainer/CardTeamData");

        card_back = (Panel)GetNode("CardBack");
        card_back.Visible = true;

        tween = (Tween)GetNode("Tween");
        RectPosition = new Vector2(0, 200) - new Vector2(GetRect().Size / 2);

        anim = (AnimationPlayer)GetNode("AnimationPlayer");

        particle_1 = (Particles2D)GetNode("NinePatchRect/Control/Particles2D");
        particle_2 = (Particles2D)GetNode("NinePatchRect/Control/Particles2D2");
        particle_1.Emitting = false;
        particle_2.Emitting = false;

        rotating_mouse_area = (Area2D)GetNode("RotatingArea");
        card_mouse_area = (Area2D)GetNode("CardArea");


        RectPivotOffset = (RectSize / 2);

        rotating_mouse_area.Connect("mouse_entered", this, "_on_mouse_entered_rotating");
        rotating_mouse_area.Connect("mouse_exited", this, "_on_mouse_exited_rotating");

        card_mouse_area.Connect("input_event", this, "_on_input_event_card");
        card_mouse_area.Connect("mouse_entered", this, "_on_mouse_entered_card");
        card_mouse_area.Connect("mouse_exited", this, "_on_mouse_exited_card");
    }

    public void init(int board_pos)
    {
        playspace_position = board_pos;
    }

    public void load_card(CardDatabase.roles new_role)
    {
        if (card_role != new_role || card_role == CardDatabase.roles.none)
        {
            card_role = new_role;

            CardDatabase cdb = new CardDatabase();
            CardDatabase.card_data data = cdb.role_data[new_role];


            role_name.BbcodeText = $"[center]{data.card_name}[/center]";
            role_team.BbcodeText = $"[center]{cdb.team_data[data.card_team]}[/center]";

            role_image.Texture = (Texture)GD.Load(data.card_art);

            card_loaded = true;
        }
    }

    public void unload_card()
    {
        role_name.BbcodeText = "";
        role_image.Texture = null;
        card_role = CardDatabase.roles.none;

        card_loaded = false;
    }

    //=================================================================================

    public void move_card(Vector2 new_pos, float move_speed = 1F)
    {
        tween.InterpolateMethod(this, "set_position", RectPosition, new_pos, move_speed, Tween.TransitionType.Expo, Tween.EaseType.InOut);
        tween.Start();
    }

    public void rotate_card(float new_rotation, float rotation_speed = 1F)
    {
        tween.InterpolateProperty(this, "rect_rotation", RectRotation, new_rotation, rotation_speed);
        tween.Start();
    }

    public void set_locked_position(Vector2 new_pos, float new_rotation)
    {
        locked_position = new_pos;
        locked_rotation = new_rotation;
    }

    public void set_light_mask(int value)
    {   
        LightMask = value;

        foreach(Node child in GetChildren())
        {
            set_light_mask(child, value);
        }
    }

    private void set_light_mask(Node node, int value)
    {
        if (node is CanvasItem item)
        {
            item.LightMask = value;
        }
        foreach(Node child in node.GetChildren())
        {
            set_light_mask(child, value);
        }
    }


    //==============================================================

    private void _on_mouse_entered_rotating()
    {
        review_card();
    }

    private void _on_mouse_exited_rotating()
    {
        un_review_card();
    }

    private void review_card()
    {
        if (card_visible)
        {
            tween.InterpolateProperty(this, "RectScale", RectScale, new Vector2(2, 2), 0.4F);
            if (RectRotation > 180)
            {
                rotate_card(360, 0.5F);
            }
            else
            {   
                rotate_card(0, 0.5F);
            }
        }
    }

    private void un_review_card()
    {
        if (card_visible)
        {
            tween.InterpolateProperty(this, "RectScale", RectScale, new Vector2(1, 1), 0.4F);
            rotate_card(locked_rotation, 0.5F);
        }
    }

    //==============================================================

    public void reveal_card()
    {
        if (!card_visible)
        {
            card_visible = true;
            anim.Queue("Card_Flip");
        }
    }

    public void hide_card()
    {
        if (card_visible)
        {
            anim.Queue("Card_UnFlip");
            un_review_card();
            card_visible = false;
            unload_card();
        }
    }

    //==============================================================

    private void _on_mouse_entered_card()
    {
        if (card_selectable && !card_selected)
        {
            Modulate = card_selected_color;
        }
    }

    private void _on_mouse_exited_card()
    {
        if (!card_selected)
        {
            Modulate = card_default_color;
        }
    }

    private void _on_input_event_card(Godot.Object viewport, InputEvent inputEvent, int shape_index)
    {
        if (card_selectable)
        {
            if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                _toggle_card_selcted();
            }
        }
    }

    public void select_card()
    {
        particle_1.Emitting = true;
        particle_2.Emitting = true;
        card_selected = true;
        EmitSignal(nameof(CardSelected), this);
    }

    public void deselect_card()
    {
        particle_1.Emitting = false;
        particle_2.Emitting = false;
        Modulate = card_default_color;
        card_selected = false;
        EmitSignal(nameof(CardUnselected), this);
    }

    private void _toggle_card_selcted()
    {
        if (card_selected)
        {
            deselect_card();
        }
        else
        {
            select_card();
        }
    }

    //==============================================================
}
