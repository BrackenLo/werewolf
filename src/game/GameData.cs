using System;
using System.Collections.Generic;

public static class GameData
{
    public enum multiplayer_state 
    {
        Host,
        Client
    }

    public static multiplayer_state state;

    public static string player_name;
    public static string ip_address;    //Only used by clients
    public static int port;

    public static int player_count;
    public static int center_card_count;
    public static bool guaranteed_werewolf;

    public static List<CardDatabase.roles> role_list;

    public static List<Server.player_data> player_data_list;
}