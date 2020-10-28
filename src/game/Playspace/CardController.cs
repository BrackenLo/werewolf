using Godot;
using System;
using System.Collections.Generic;

public class CardController : Node2D
{

    [Signal]
    public delegate void SelectedCardsChanged(int[] cards);

    //========================================================================

    public List<CardBase> playspace_cards {get; private set;} = new List<CardBase>();
    public Vector2 board_center = new Vector2();

    //========================================================================

    [Export]
    public float center_card_spacing = 30;

    //========================================================================

    //========================================================================

    public int center_card_count {get; private set;} = 3;
    public int player_count {get; private set;} = 1;

    public Vector2 center_card_area {get; private set;} = new Vector2();

    //=========================================================================
    public List<int> selected_cards = new List<int>();

    //=========================================================================

    public void init(int center_card_count, int player_count)
    {
        this.center_card_count = center_card_count;
        this.player_count = player_count;
    }

    //=========================================================================
    //=========================================================================

    public void add_card()
    {
        Rpc("_add_card");
    }

    [RemoteSync]
    private void _add_card()
    {
        PackedScene packed = (PackedScene)ResourceLoader.Load("res://src/game/cards/CardBase.tscn");
        CardBase new_card = (CardBase)packed.Instance();

        playspace_cards.Add(new_card);
        AddChild(new_card);

        new_card.Connect("CardSelected", this, "_card_selected");
        new_card.Connect("CardUnselected", this, "_card_unselected");

        reset_positions();
    }

    //=========================================================================
    //=========================================================================

    public void reset_positions()
    {
        if (playspace_cards.Count <= center_card_count)
        {
            _reset_center_cards();
        }
        else
        {
            _reset_center_cards();
            _reset_player_cards();
        }
    }

    private void _reset_center_cards()
    {
        List<CardBase> center_cards = new List<CardBase>();

        float final_size = 0;
        float used_space = 0;

        for (int x = 0; x < Mathf.Min(center_card_count, playspace_cards.Count); x++)
        {
            center_cards.Add(playspace_cards[x]);

            final_size += playspace_cards[x].GetRect().Size.x * playspace_cards[x].RectScale.x;
            final_size += center_card_spacing;
        }

        final_size -= center_card_spacing;

        Vector2 card_offset = new Vector2(board_center.x - final_size / 2, board_center.y);

        float tallest_card = 0;

        foreach (CardBase card in center_cards)
        {
            float special_spacing = 0;
            if (card.RectScale.x != 1)
                special_spacing = ((card.GetRect().Size.x * card.RectScale.x) - card.GetRect().Size.x) / 2;

            float new_x = card_offset.x + used_space + special_spacing;
            float new_y = 0 - card.GetRect().Size.y / 2;

            if (card.GetRect().Size.y > tallest_card)
                tallest_card = card.GetRect().Size.y;

            card.move_card(new Vector2(new_x, new_y), 1);
            card.set_locked_position(new Vector2(new_x, new_y), 0);

            used_space += card.GetRect().Size.x * card.RectScale.x;
            used_space += center_card_spacing;
        }

        center_card_area = new Vector2(used_space + center_card_spacing, tallest_card + center_card_spacing);

    }

    //-----------------------------------------

    private void _reset_player_cards()
    {   
        float min_dist = Mathf.Min(center_card_area.x * 1.3F, center_card_area.y * 2F);

        float hor_rad = min_dist;
        float ver_rad = min_dist;

        List<CardBase> player_cards = new List<CardBase>();

        for (int x = center_card_count; x < playspace_cards.Count; x++)
        {
            player_cards.Add(playspace_cards[x]);
        }

        float angle_spacing = 360 / player_cards.Count;
        int card_index = 0;

        foreach(CardBase card in player_cards)
        {
            float angle_deg = angle_spacing * card_index;
            float angle = Mathf.Deg2Rad(angle_deg + 90);

            Vector2 oval_angle_vector = new Vector2(
                hor_rad * Mathf.Cos(angle),
                ver_rad * Mathf.Sin(angle)
            );
            Vector2 card_pos = board_center + oval_angle_vector - card.RectSize / 2;

            card.move_card(card_pos, 1);
            card.rotate_card(angle_deg, 1);
            card.set_locked_position(card_pos, angle_deg);

            card_index++;
        }
    }

    //=========================================================================
    //=========================================================================

    public void set_selecting_center_cards(bool val)
    {
        for (int x = 0; x < center_card_count; x++)
        {
            playspace_cards[x].card_selectable = val;
        }
    }

    public void set_selecting_player_cards(bool val)
    {
        set_selecting_player_cards(val, new int[]{});
    }

    public void set_selecting_player_cards(bool val, int[] exclude)
    {
        for (int x = center_card_count; x < playspace_cards.Count; x++)
        {
            bool found = false;
            foreach (int num in exclude)
            {
                if (x == num)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                playspace_cards[x].card_selectable = val;
            else
                playspace_cards[x].card_selectable = !val;
        }
    }

    public void set_selecting_cards(bool val)
    {
        foreach (CardBase card in playspace_cards)
        {
            card.card_selectable = val;
        }
    }

    public void clear_selected_cards()
    {
        selected_cards = new List<int>();
        foreach(CardBase card in playspace_cards)
        {
            card.deselect_card();
        }
    }

    private void _card_selected(CardBase card)
    {
        selected_cards.Add(playspace_cards.IndexOf(card));

        EmitSignal(nameof(SelectedCardsChanged), selected_cards.ToArray());
    }

    private void _card_unselected(CardBase card)
    {
        if (selected_cards.Contains(playspace_cards.IndexOf(card)))
        {
            GD.PrintS("Removing card", card);
            selected_cards.Remove(playspace_cards.IndexOf(card));
            EmitSignal(nameof(SelectedCardsChanged), selected_cards.ToArray());
        }
    }

    //=========================================================================
    //=========================================================================

    public void load_and_reveal_card(int position, CardDatabase.roles role)
    {
        CardBase to_reveal = playspace_cards[position];
        to_reveal.load_card(role);
        to_reveal.reveal_card();
    }

    public void hide_all_cards()
    {
        foreach(CardBase card in playspace_cards)
        {
            card.hide_card();
        }
    }

    //=========================================================================
    //=========================================================================

    public void swap_cards(int pos1, int pos2)
    {
        if (pos1 >= playspace_cards.Count || pos2 >= playspace_cards.Count)
        {
            GD.PrintS("Cardswap out of bounds");
            return;
        }

        GD.PrintS("Swapping", pos1,"and", pos2);

        CardBase temp = playspace_cards[pos1];

        playspace_cards[pos1] = playspace_cards[pos2];
        playspace_cards[pos2] = temp;

        _reset_player_cards();
        _reset_center_cards();
    }

    //=========================================================================
    //=========================================================================

    public void set_cards_light_masks(int mask)
    {
        foreach(CardBase card in playspace_cards)
        {
            card.set_light_mask(mask);
        }
    }

    //=========================================================================
    //=========================================================================
}
