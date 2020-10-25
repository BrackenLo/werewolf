using Godot;
using System;

public class CenterCards : Node2D
{

    //=====================================================================

    //=====================================================================

    [Export]
    public float card_spacing = 30;

    public float node_size {get; protected set;} = 0;

    public int count = 0;


    //=====================================================================

    //=====================================================================

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
        float used_space = 0;


        foreach(Node node in GetChildren())
        {
            if (node is CardBase card)
            {

                node_size += card.GetRect().Size.x;
                node_size += card_spacing;
            }
        }

        node_size -= card_spacing;

        Vector2 cards_offset = new Vector2(0 - node_size / 2, 0);

        foreach(Node node in GetChildren())
        {
            if (node is CardBase card)
            {
                float new_x = cards_offset.x + used_space;
                float new_y = 0 - card.GetRect().Size.y / 2;

                card.move_card(new Vector2(new_x, new_y), 1);
                card.set_locked_position(new Vector2(new_x, new_y), 0);

                used_space += card.GetRect().Size.x;
                used_space += card_spacing;
            }
        }
    }

    //=====================================================================

    //=====================================================================

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

    //=====================================================================


}
