using Godot;
using System;
using System.Collections.Generic;

public class GameRoom : Node2D
{

    //====================================================================

    private Server server;
    private Client client;
    private UsernameSelectScreen user_select_screen;
    private Playspace playspace;

    //====================================================================

    public override void _Ready()
    {
        server = (Server)GetNode("Server");
        client = (Client)GetNode("Client");
        user_select_screen = (UsernameSelectScreen)GetNode("Client/CanvasLayer/UsernameSelectScreen");
        playspace = (Playspace)GetNode("Playspace");


        //Initialise the server, client and playspace
        //server requires the user selcetion screen to allow for verification of username/colors. Also needs client for communication
        //Client requires server for communication, also playspace to play
        //playspace requires client for communication

        server.init(this, user_select_screen, client);
        client.init(this, server, playspace);
        playspace.init(this, client);



        //Start either client or server depending on what the user selected on main menu
        if (GameData.state == GameData.multiplayer_state.Host)
        {
            server.start_server(GameData.port, GameData.player_count);
        }
        else
        {
            client.join_server(GameData.ip_address, GameData.port);
        }
    }

    //Called by server if client and client if server
    //Creates and shows the user selection screen with the data both client and server should now share
    public void start_user_select()
    {
        user_select_screen.init(server, GameData.player_count);
    }

    //Called by the server when all connected players have selected their username and color
    public void start_game(List<Server.player_data> player_data_list)
    {
        GameData.player_data_list = player_data_list;

        user_select_screen.Visible = false;

        playspace.start_game();

    }

}
