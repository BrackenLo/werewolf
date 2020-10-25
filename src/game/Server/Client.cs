using Godot;
using System;
using System.Collections.Generic;

public class Client : Node
{
    //=============================================================================================

    private GameRoom parent;
    private Server server;
    private Playspace playspace;

    private string password;

    private List<int> connected_ids = new List<int>();

    //=============================================================================================

    public void init(GameRoom parent, Server server, Playspace playspace)
    {
        this.parent = parent;
        this.server = server;
        this.playspace = playspace;
    }

    //=============================================================================================

    public void join_server(string ip_address, int port, string password = "")
    {
        if (!GetTree().HasNetworkPeer())
        {
            this.password = password;   //Still needs implementing

            NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
            peer.CreateClient(ip_address, port);
            GetTree().NetworkPeer = peer;

            GetTree().Connect("connected_to_server", this, "_connected_ok");
            GetTree().Connect("connection_failed", this, "_connected_fail");
            GetTree().Connect("server_disconnected", this, "_server_disconnected");

            GetTree().Connect("network_peer_connected", this, "_player_connected");
            GetTree().Connect("network_peer_disconnected", this, "_player_disconnected");

            GD.PrintS($"Attempting to connect to server with address {ip_address}:{port}");
        }
        else if (GetTree().GetNetworkUniqueId() != 1)
        {
            GD.PrintS("You are already connected. If you are experiencing problems, please restart your game.");
        }
    }

    //Called by the server to connect the neccecary client functions without creating a client peer
    public void join_as_host()
    {
        if (GetTree().HasNetworkPeer())
        {
            if (GetTree().GetNetworkUniqueId() == 1)
            {
                GetTree().Connect("network_peer_connected", this, "_player_connected");
                GetTree().Connect("network_peer_disconnected", this, "_player_disconnected");

                parent.start_user_select();
            }
            else
            {
                GD.PrintS("You are not the host");
            }
        }
    }

    //=============================================================================================

    private void _connected_ok()
    {
        GD.PrintS("You are now connected");
        server.request_authentication(password);

    }

    private void _connected_fail()
    {
        GD.PrintS("Connection Failed");
        disconnect_from_server();
        return_to_menu();
    }

    private void _server_disconnected()
    {
        GD.PrintS("Server has been closed");
        disconnect_from_server();
        return_to_menu();
    }

    [Remote]
    private void CloseServer()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            _server_disconnected();
        }
    }

    [Remote]
    private void DisconnectClient()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have been disconnected");
            disconnect_from_server();
            return_to_menu();
        }
    }

    private void disconnect_from_server()
    {
        if (GetTree().NetworkPeer is NetworkedMultiplayerENet enet)
        {
            enet.CloseConnection();
        }
        GetTree().NetworkPeer = null;
    }

    private void return_to_menu()
    {
        GetTree().ChangeScene("res://src/game/Menus/MainMenu.tscn");
    }

    //=============================================================================================

    private void _player_connected(int id)
    {
        connected_ids.Add(id);
    }

    private void _player_disconnected(int id)
    {
        connected_ids.Remove(id);
    }

    //=============================================================================================
}
