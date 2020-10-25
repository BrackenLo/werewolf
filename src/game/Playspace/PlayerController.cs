using Godot;
using System;
using System.Collections.Generic;

public class PlayerController : Node2D
{

    //=================================================================

    public List<Server.player_data> player_list {get; private set;} = new List<Server.player_data>();
    public Dictionary<int, Player> player_id_to_icon {get; private set;} = new Dictionary<int, Player>();
    public Dictionary<int, Server.player_data> player_data_dict {get; private set;} = new Dictionary<int, Server.player_data>();

    private Vector2 board_center = new Vector2();
    private float hor_rad;
    private float ver_rad;

    //=================================================================

    public int node_size {get; private set;} = 0;

    //=================================================================

    public override void _Ready()
    {
        hor_rad = GetViewportRect().Size.x * 0.9F;
        ver_rad = GetViewportRect().Size.y * 0.768F;
    }
    
    public void add_players(List<Server.player_data> players)
    {
        foreach(Server.player_data player in players)
        {
            PackedScene packed = (PackedScene)GD.Load("res://src/game/Playspace/Player.tscn");
            Player new_player = (Player)packed.Instance();

            AddChild(new_player);
            new_player.init(player.player_name, player.player_color);
        
            player_list.Add(player);
            player_id_to_icon.Add(player.player_id, new_player);
            player_data_dict.Add(player.player_id, player);

            player_list = sort_players(player_list);
            reset_positions(player_list, player_id_to_icon);
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

    public void reset_positions(List<Server.player_data> player_data, Dictionary<int, Player> player_nodes)
    {
        float player_index = 0;

        float angle_spacing = 360 / player_list.Count;

        foreach(Server.player_data data in player_data)
        {
            Player player_node = player_nodes[data.player_id];

            float angle_deg = angle_spacing * player_index;
            float angle = Mathf.Deg2Rad(angle_deg + 90);

            Vector2 oval_angle_vector = new Vector2(hor_rad * Mathf.Cos(angle), ver_rad * Mathf.Sin(angle));

            Vector2 player_pos = board_center + oval_angle_vector - player_node.RectSize / 2;

            player_node.RectPosition = player_pos;
            //player.RectRotation = angle_deg;

            player_index++;
        }
    }

    //======================================================================
    //======================================================================

    public List<int> player_position_to_id(List<int> positions)
    {
        List<int> to_return = new List<int>();

        foreach(int pos in positions)
        {
            to_return.Add(player_list[pos].player_id);
        }

        return to_return;
    }

    //======================================================================
    //======================================================================

    
}
