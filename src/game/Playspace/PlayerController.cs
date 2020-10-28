using Godot;
using System;
using System.Collections.Generic;

public class PlayerController : Node2D
{

    //=================================================================

    [Signal]
    public delegate void PlayerSelected(int player_id);

    //=================================================================

    public List<Server.player_data> player_list {get; private set;} = new List<Server.player_data>();
    public Dictionary<int, Player> player_id_to_icon {get; private set;} = new Dictionary<int, Player>();
    public Dictionary<int, Server.player_data> player_data_dict {get; private set;} = new Dictionary<int, Server.player_data>();

    private Vector2 board_center = new Vector2();
    public Vector2 center_card_area {get; private set;} = new Vector2();

    //=================================================================

    public int node_size {get; private set;} = 0;

    public int selected_player;

    //=================================================================

    public void set_center_area(Vector2 area)
    {
        center_card_area = area;
        reset_positions();
    }

    //=================================================================

    public int get_id_by_position(int player_position)
    {
        foreach(Server.player_data player in player_list)
        {
            if (player.player_position == player_position)
            {
                return player.player_id;
            }
        }
        return -1;
    }

    //=================================================================
    
    public void add_players(List<Server.player_data> players)
    {
        foreach(Server.player_data player in players)
        {
            PackedScene packed = (PackedScene)GD.Load("res://src/game/Playspace/Player.tscn");
            Player new_player = (Player)packed.Instance();

            AddChild(new_player);
            new_player.init(player.player_id, player.player_position, player.player_name, player.player_color);
        
            player_list.Add(player);
            player_id_to_icon.Add(player.player_id, new_player);
            player_data_dict.Add(player.player_id, player);

            new_player.Connect("PlayerSelected", this, nameof(_player_selected));
            new_player.Connect("PlayerUnselected", this, nameof(_player_unselected));

            player_list = sort_players(player_list);
            reset_positions();
        }
    }

    private List<Server.player_data> sort_players(List<Server.player_data> old_list)
    {
        List<Server.player_data> temp_list = old_list;
        List<Server.player_data> sorted_list = new List<Server.player_data>();
        int original_size = temp_list.Count;

        for (int x = 0; x < original_size; x++)
        {   
            int lowest = 99999;
            Server.player_data lowest_player = null;

            foreach(Server.player_data data in temp_list)
            {
                if (data.player_position < lowest)
                {
                    lowest_player = data;
                    lowest = data.player_position;
                }
            }
            sorted_list.Add(lowest_player);
            temp_list.Remove(lowest_player);
        }

        return sorted_list;
    }

    public void reset_positions()
    {
        float player_index = 0;

        float angle_spacing = 360 / player_list.Count;

        foreach(Server.player_data data in player_list)
        {
            float min_dist = Mathf.Min(center_card_area.x * 1.70F, center_card_area.y * 2.65F);

            float hor_rad = min_dist;
            float ver_rad = min_dist;


            //float hor_rad = center_card_area.x * 1.3F;
            //float ver_rad = center_card_area.y * 2F;


            Player player_node = player_id_to_icon[data.player_id];

            float angle_deg = angle_spacing * player_index;
            float angle = Mathf.Deg2Rad(angle_deg + 90);

            Vector2 oval_angle_vector = new Vector2(hor_rad * Mathf.Cos(angle), ver_rad * Mathf.Sin(angle));

            Vector2 player_pos = board_center + oval_angle_vector;

            //Triangle math and shit

            float player_area_length = new Vector2().DistanceTo(player_pos) * 1.2F;
            float player_area_angle = Mathf.Min(angle_spacing, 180);

            Vector2 pos_1 = new Vector2();

            float hyp = player_area_length / Mathf.Cos(Mathf.Deg2Rad(player_area_angle / 2));
            float adj1 = Mathf.Cos(angle + Mathf.Deg2Rad(player_area_angle / 2)) * hyp;
            float opp1 = Mathf.Sin(angle + Mathf.Deg2Rad(player_area_angle / 2)) * hyp;
            Vector2 pos_2 = new Vector2(adj1, opp1);

            float adj2 = Mathf.Cos(angle - Mathf.Deg2Rad(player_area_angle / 2)) * hyp;
            float opp2 = Mathf.Sin(angle - Mathf.Deg2Rad(player_area_angle / 2)) * hyp;

            Vector2 pos_3 = new Vector2(adj2, opp2);

            // done (thank goodness)

            player_node.set_player_area(pos_1, pos_2, pos_3);

            player_node.set_position(player_pos);

            player_index++;
        }
    }

    //======================================================================
    //======================================================================

    public void set_selecting_players(bool value)
    {
        set_selecting_players(value, new int[]{});
    }

    public void set_selecting_players(bool value, int[] exclude)
    {
        foreach (KeyValuePair<int, Player> player in player_id_to_icon)
        {
            bool found = false;
            foreach(int x in exclude)
            {
                if (get_id_by_position(x) == player.Key)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                player.Value.set_player_selectable(value);
            else
                player.Value.set_player_selectable(!value);
        }
    }

    public void clear_selected_players()
    {
        foreach(KeyValuePair<int, Player> data in player_id_to_icon)
        {
            data.Value.deselect_player();
        }
        selected_player = -1;
    }

    //--------------------------------------------

    //Function connected to signal
    private void _player_selected(int player_id)
    {
        set_selecting_players(false);
        selected_player = player_id;

        EmitSignal(nameof(PlayerSelected), player_id);
    }

    private void _player_unselected(int player_id)
    {
        //set_selecting_players(true);
    }

    //======================================================================
    //======================================================================

    public void set_player_spotlight(int player_id, bool value)
    {
        player_id_to_icon[player_id].set_player_spotlight(value);
    }

    public void set_players_spotlights(bool value)
    {
        foreach(KeyValuePair<int, Player> player in player_id_to_icon)
        {
            player.Value.set_player_spotlight(value);
        }
    }

    //======================================================================
    //======================================================================

    public void set_all_player_mouse(bool value)
    {
        if (IsNetworkMaster())
            Rpc("_set_all_player_mouse", value);
    }

    [RemoteSync]
    private void _set_all_player_mouse(bool value)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            foreach(KeyValuePair<int, Player> data in player_id_to_icon)
            {
                data.Value.set_player_mouse(value);
            }
        }
    }

    //======================================================================
    //======================================================================
}
