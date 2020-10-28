using Godot;
using System;

public class Player : Node2D
{

    //=======================================================================

    [Signal]
    public delegate void PlayerSelected(int id);
    [Signal]
    public delegate void PlayerUnselected(int id);

    //=======================================================================

    private ColorRect player_sprite;
    private Label player_name;

    private Area2D player_area;
    private CollisionPolygon2D player_area_collision;
    private Polygon2D visible_player_area;
    private Light2D light;

    private Tween tween;
    private Sprite mouse;
    private Label pointer_name;

    //=======================================================================

    public int player_id {get; private set;}

    private bool player_selectable = false;
    private bool player_selected = false;

    private Color player_color;

    private bool awake = false;

    //======================================================================

    [Puppet]
    private Vector2 mouse_pos;
    private bool mouse_enabled = false;

    //=======================================================================
    

    public override void _Ready()
    {
        player_sprite = (ColorRect)GetNode("Sprite");
        player_name = (Label)GetNode("Sprite/Label");

        player_area = (Area2D)GetNode("Area2D");
        player_area_collision = (CollisionPolygon2D)GetNode("Area2D/CollisionPolygon2D");
        visible_player_area = (Polygon2D)GetNode("Polygon2D");
        light = (Light2D)GetNode("Light2D");

        tween = (Tween)GetNode("Tween");
        mouse = (Sprite)GetNode("MousePointer");
        pointer_name = (Label)GetNode("MousePointer/PointerName");

        player_area.Connect("input_event", this, "_on_area_input_event");
        player_area.Connect("mouse_entered", this, "_on_area_mouse_entered");
        player_area.Connect("mouse_exited", this, "_on_area_mouse_exited");
    }

    public void init(int id, int position, string name, Color color)
    {
        Name = $"{id}";
        SetNetworkMaster(id);

        player_id = id;

        player_name.Text = name;
        player_sprite.Color = color;
        player_color = color;

        mouse.Modulate = color;
        pointer_name.Text = name;

        int mask = (int)Mathf.Pow(2, position % 18);
        visible_player_area.LightMask = mask;
        light.RangeItemCullMask = mask;
        player_sprite.LightMask = mask;
        player_name.LightMask = mask;

        set_default_color();
    }

    public override void _Process(float delta)
    {
        if (mouse_enabled && IsNetworkMaster())
        {
            Vector2 new_pos = GetGlobalMousePosition();
            if (mouse_pos != new_pos)
            {
                mouse_pos = new_pos;
                Rset("mouse_pos", new_pos);
            }
        }

        mouse.GlobalPosition = mouse_pos;
    }

    //=======================================================================

    public void set_position(Vector2 new_pos)
    {
        player_sprite.RectPosition = new_pos - (player_sprite.RectSize / 2);
        //light.Position = new_pos;
    }

    public Vector2 get_size()
    {
        return player_sprite.RectSize;
    }

    public void set_player_area(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        player_area_collision.Polygon = new Vector2[]{p1, p2, p3};
        visible_player_area.Polygon = new Vector2[]{p1, p2, p3};
    }

    public void set_player_selectable(bool value)
    {
        player_selectable = value;
    }

    //=======================================================================

    private void set_default_color()
    {
        Color darkened = player_color.Darkened(0.8F);
        set_color(player_color.Darkened(0.3F), darkened, darkened);
    }

    private void set_selected_color()
    {
        Color slight_darken = player_color.Darkened(0.1F);
        set_color(slight_darken, slight_darken, slight_darken);
    }

    private void set_color(Color c1, Color c2, Color c3)
    {
        visible_player_area.VertexColors = new Color[]{c1, c2, c3};
    }

    //=======================================================================

    private void _on_area_input_event(Godot.Object viewport, InputEvent inputEvent, int shape_index)
    {
        if (player_selectable)
        {
            if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                _toggle_player_selected();
            }
        }
    }

    private void _on_area_mouse_entered()
    {
        if (player_selectable && !player_selected)
        {
            set_selected_color();
        }
    }

    private void _on_area_mouse_exited()
    {
        if (!player_selected)
        {
            set_default_color();
        }
    }


    private void select_player()
    {
        player_selected = true;
        set_selected_color();
        EmitSignal(nameof(PlayerSelected), player_id);
    }

    public void deselect_player()
    {
        player_selected = false;
        set_default_color();
        EmitSignal(nameof(PlayerUnselected), player_id);
    }

    private void _toggle_player_selected()
    {
        if (player_selected)
            deselect_player();
        else
            select_player();
    }

    //===========================================================================

    public void set_player_spotlight(bool value)
    {
        if(value)
        {
            light.Energy = 0;
            tween.InterpolateProperty(light, "energy", light.Energy, 1.5, 0.5F);
            light.Enabled = true;
            tween.Start();
        }
        else
        {
            tween.InterpolateProperty(light, "energy", light.Energy, 0, 0.5f);
            tween.Start();
            tween.Connect("tween_completed", this, "_light_off");
        }
    }

    private void _light_off(Godot.Object obj, NodePath key)
    {
        tween.Disconnect("tween_completed", this, "_light_off");
        light.Enabled = false;
    }

    //===========================================================================

    public void set_player_mouse(bool value)
    {
        if (value)
        {
            mouse.Visible = true;
            mouse_enabled = true;
            Input.SetMouseMode(Input.MouseMode.Hidden);
        }
        else
        {
            mouse.Visible = false;
            mouse_enabled = false;
            Input.SetMouseMode(Input.MouseMode.Visible);
        }
    }

    //===========================================================================

}