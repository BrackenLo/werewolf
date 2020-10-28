using Godot;
using System;
using System.Collections.Generic;

public class Server : Node
{

    //=======================================================================================

    public List<int> non_autenticated_users {get; private set;} = new List<int>();
    public List<int> authenticated_users {get; private set;} = new List<int>();
    public Dictionary<int, player_data> player_data_list = new Dictionary<int, player_data>();
    private List<player_data> final_player_list = new List<player_data>();

    public int player_count {get; private set;}

    private string password = "";

    //========================================================================================

    private GameRoom parent;
    private UsernameSelectScreen user_screen;
    private Client client;

    //========================================================================================

    public void init(GameRoom parent, UsernameSelectScreen personalize_screen, Client client)
    {
        this.parent = parent;
        this.user_screen = personalize_screen;
        this.client = client;
    }

    //========================================================================================

    public void start_server(int port, int player_count)
    {
        if (!GetTree().HasNetworkPeer())
        {
            this.player_count = player_count;

            NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
            peer.CreateServer(port, player_count);
            GetTree().NetworkPeer = peer;

            GetTree().Connect("network_peer_connected", this, "_server_player_connected");
            GetTree().Connect("network_peer_disconnected", this, "_server_player_disconnected");

            GD.PrintS($"Starting server on port {port}");
            GD.PrintS($"Connection status = {GetTree().NetworkPeer.GetConnectionStatus()}");

            authenticated_users.Add(GetTree().GetNetworkUniqueId());
            client.join_as_host();
        }
        else
        {
            GD.PrintS("You are already connected. Please restart your game if you are not already connected.");
        }
    }

    //========================================================================================

    private void _server_player_connected(int id)
    {
        GD.PrintS("A player is trying to join the server");
        non_autenticated_users.Add(id);
    }

    private void _server_player_disconnected(int id)
    {
        GD.PrintS("A player has left the server");

        non_autenticated_users.Remove(id);
        authenticated_users.Remove(id);
        player_data_list.Remove(id);
    }

    //=========================================================================================

    private NetworkedMultiplayerENet get_enet()
    {
        if (GetTree().NetworkPeer is NetworkedMultiplayerENet enet)
        {
            return enet;
        }
        else
        {
            return null;
        }
    }

    //=========================================================================================

    //client side function
    public void request_authentication(string password)
    {
        RpcId(1, "_authenticate_user", password);
    }

    [Remote]    //Server side function
    private void _authenticate_user(string password)
    {
        int sender = GetTree().GetRpcSenderId();

        if (authenticated_users.Contains(sender))
        {
            return;
        }
        else if (!non_autenticated_users.Contains(sender))
        {
            GD.PrintS("sender has not been added?");
            get_enet().DisconnectPeer(sender);
        }

        if (this.password == "" || this.password == password)
        {
            authenticated_users.Add(sender);
            non_autenticated_users.Remove(sender);
            RpcId(sender, "user_authenticated", player_count, GameData.center_card_count, GameData.guaranteed_werewolf, GameData.role_list);
            authenticated_users.Sort();
        }
    }

    [Remote]    //Client side function
    private void user_authenticated(int player_count, int center_count, bool guaranteed_ww, System.Collections.Generic.List<CardDatabase.roles> role_list)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GameData.player_count = player_count;
            GameData.center_card_count = center_count;
            GameData.guaranteed_werewolf = guaranteed_ww;
            GameData.role_list = role_list;

            parent.start_user_select();
        }
    }

    //===============================================================================

    //Client + serverside function
    public void check_user_details(string username, Color usercolor)
    {

        RpcId(1, "checking_user_details", username, usercolor);

    }


    [RemoteSync]    //Server side function
    private void checking_user_details(string username, Color usercolor)
    {
        int sender = GetTree().GetRpcSenderId();

        string lower_name = username.ToLower();

        bool color_fine = true;
        bool username_fine = true;

        foreach(player_data data in player_data_list.Values)
        {
            if (data.player_color == usercolor)
                color_fine = false;

            if (data.player_name.ToLower() == lower_name)
                username_fine = false;
        }

        if (color_fine == false)
        {
            RpcId(sender, "color_invalid");
        }

        if (username_fine == false)
        {
            RpcId(sender, "username_invalid");
        }

        if (color_fine && username_fine)
        {
            RpcId(sender, "user_details_valid");
            player_data_list.Add(sender, new player_data(sender, username, usercolor, player_data_list.Count));

            check_start_criteria();
        }
    }

    [RemoteSync]
    private void color_invalid()
    {
        user_screen.color_taken();
    }

    [RemoteSync]
    private void username_invalid()
    {
        user_screen.username_taken();
    }

    [RemoteSync]
    private void user_details_valid()
    {
        user_screen.details_validated();
    }

    //===============================================================================

    //Server side function
    private void check_start_criteria() 
    {
        if (player_count == authenticated_users.Count && player_count == player_data_list.Count)
        {
            get_enet().RefuseNewConnections = true;
            foreach(KeyValuePair<int, player_data> data in player_data_list)
            {
                Rpc("_recieve_player_data", data.Value.player_id, data.Value.player_name, data.Value.player_color, data.Value.player_position);
            }
            Rpc("_start_game");
        }
    }

    [RemoteSync]
    private void _recieve_player_data(int id, string name, Color color, int position)
    {
        final_player_list.Add(new Server.player_data(id, name, color, position));
    }

    [RemoteSync]
    private void _start_game()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            parent.start_game(final_player_list);
        }
    }

    //===============================================================================










    public class player_data
    {
        public int player_id {get; private set;} 
        public string player_name {get; private set;}
        public Color player_color {get; private set;}
        public int player_position {get; private set;}

        public player_data(int id, String name, Color color, int position)
        {
            player_id = id;
            player_name = name;
            player_color = color;
            player_position = position;
        }
    }


}
