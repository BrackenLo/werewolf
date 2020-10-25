using Godot;
using System;

public class PlayerCards : Node2D
{

    //==============================================================

    private Vector2 board_center = new Vector2();
    private float hor_rad;
    private float ver_rad;

    //==============================================================
    
    public int node_size {get; protected set;} = 0;
    public int count = 0;

    //==============================================================
    

    public override void _Ready()
    {
        hor_rad = GetViewportRect().Size.x * 0.75F;
        ver_rad = GetViewportRect().Size.y * 0.64F;
    }

    //================================================================

    public void add_card(CardBase card)
    {
        AddChild(card);

        count++;

        if (IsNetworkMaster())
            reset_positions();
    }

    public void reset_positions()
    {
        node_size = 0;

        float card_index = 0;

        foreach(Node node in GetChildren())
        {
            if (node is CardBase card)
            {
                node_size++;
            }
        }

        float angle_spacing = 360 / node_size;

        foreach(Node node in GetChildren())
        {
            if (node is CardBase card)
            {
                float angle_deg = angle_spacing * card_index;
                float angle = Mathf.Deg2Rad(angle_deg + 90);

                Vector2 oval_angle_vector = new Vector2(hor_rad * Mathf.Cos(angle), ver_rad * Mathf.Sin(angle));

                Vector2 card_pos = board_center + oval_angle_vector - card.RectSize / 2;

                card.move_card(card_pos, 1);
                card.rotate_card(angle_deg, 1);
                card.set_locked_position(card_pos, angle_deg);

                card_index++;
            }
        }

    }

    //===============================================================

    public CardBase get_card(int index)
    {

        if (index < count && index >= 0)
        {
            int found = 0;
            foreach(Node n in GetChildren())
            {
                if (n is CardBase card)
                {
                    if (index == found)
                        return card;
                    found++;
                }
            }
        }
        return null;
    }

    //============================================================
}
