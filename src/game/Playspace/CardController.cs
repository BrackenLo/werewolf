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

    [Export]
    public float hor_rad_percentage = 0.75F;
    [Export]
    public float ver_rad_percentage = 0.64F;

    private float hor_rad;
    private float ver_rad;

    //========================================================================

    public int center_card_count {get; private set;} = 3;
    public int player_count {get; private set;} = 1;

    //=========================================================================
    public List<int> selected_cards = new List<int>();

    //=========================================================================

    public override void _Ready()
    {
        hor_rad = GetViewportRect().Size.x * hor_rad_percentage;
        ver_rad = GetViewportRect().Size.y * ver_rad_percentage;
    }

    public void init(int center_card_count, int player_count)
    {
        this.center_card_count = center_card_count;
        this.player_count = player_count;
    }

    public override void _Process(float delta)
    {
        //_reset_center_cards();
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
        if (player_count + center_card_count == playspace_cards.Count)
        {
            GD.PrintS("Resetting positions");
            _reset_center_cards(playspace_cards, center_card_count, center_card_spacing, board_center);
            _reset_player_cards(playspace_cards, center_card_count, hor_rad, ver_rad, board_center);
        }
        else if (playspace_cards.Count <= center_card_count)
        {
            _reset_center_cards(playspace_cards, playspace_cards.Count, center_card_spacing, board_center);
        }
        else
        {
            _reset_center_cards(playspace_cards, center_card_count, center_card_spacing, board_center);
            _reset_player_cards(playspace_cards, center_card_count, hor_rad, ver_rad, board_center);
        }
    }

    private void _reset_center_cards(List<CardBase> cards, int center_count, float spacing, Vector2 center)
    {
        List<CardBase> center_cards = new List<CardBase>();

        float final_size = 0;
        float used_space = 0;

        for (int x = 0; x < center_count; x++)
        {
            center_cards.Add(cards[x]);

            final_size += cards[x].GetRect().Size.x * cards[x].RectScale.x;
            final_size += spacing;
        }

        final_size -= spacing;

        Vector2 card_offset = new Vector2(center.x - final_size / 2, center.y);

        foreach (CardBase card in center_cards)
        {
            float special_spacing = 0;
            if (card.RectScale.x != 1)
                special_spacing = ((card.GetRect().Size.x * card.RectScale.x) - card.GetRect().Size.x) / 2;

            float new_x = card_offset.x + used_space + special_spacing;
            float new_y = 0 - card.GetRect().Size.y / 2;

            card.move_card(new Vector2(new_x, new_y), 1);
            card.set_locked_position(new Vector2(new_x, new_y), 0);

            used_space += card.GetRect().Size.x * card.RectScale.x;
            used_space += spacing;
        }
    }

    //-----------------------------------------

    private void _reset_player_cards(List<CardBase> cards, int center_count, float hor_radius, float ver_radius, Vector2 center)
    {
        List<CardBase> player_cards = new List<CardBase>();

        for (int x = center_count; x < cards.Count; x++)
        {
            player_cards.Add(cards[x]);
        }

        float angle_spacing = 360 / player_cards.Count;
        int card_index = 0;

        foreach(CardBase card in player_cards)
        {
            float angle_deg = angle_spacing * card_index;
            float angle = Mathf.Deg2Rad(angle_deg + 90);

            Vector2 oval_angle_vector = new Vector2(
                hor_radius * Mathf.Cos(angle),
                ver_radius * Mathf.Sin(angle)
            );
            Vector2 card_pos = center + oval_angle_vector - card.RectSize / 2;

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

        EmitSignal("SelectedCardsChanged", selected_cards.ToArray());
    }

    private void _card_unselected(CardBase card)
    {
        if (selected_cards.Contains(playspace_cards.IndexOf(card)))
        {
            GD.PrintS("Removing card", card);
            selected_cards.Remove(playspace_cards.IndexOf(card));
            EmitSignal("SelectedCardsChanged", selected_cards.ToArray());
        }
    }

    //=========================================================================
    //=========================================================================

    public void load_and_reveal_player_card(int player_pos, CardDatabase.roles role)
    {
        CardBase to_reveal = playspace_cards[player_pos + center_card_count];
        to_reveal.load_card(role);
        to_reveal.reveal_card();
    }

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
}
