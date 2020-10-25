using Godot;
using System;

public class MainMenu : PanelContainer
{

    //Notifications are used for closing the server properly if the user quits the program prematurely
    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationWmQuitRequest)
        {
            if (GetTree().HasNetworkPeer())
            {
                if (GetTree().GetNetworkUniqueId() == 1)    //Is Server host
                {
                    Rpc("CloseServer");
                }
                else    //Is client
                {
                    RpcId(1, "player_disconnecting");
                }
            }
            GetTree().Quit();
        }
    }


    //Trigger for when the user presses the host game button
    //transfers the user to the host game page and sets the state in the global variable class
    private void _on_HostGameButton_pressed()
    {
        GameData.state = GameData.multiplayer_state.Host;
        GetTree().ChangeScene("res://src/game/Menus/HostGame.tscn");
    }

    //Trigger for when the user presses the join game button
    //transfers the user to the join game page and sets the state in the global variable class
    private void _on_JoinGameButton_pressed()
    {
        GameData.state = GameData.multiplayer_state.Client;
        GetTree().ChangeScene("res://src/game/Menus/JoinGame.tscn");
    }


    //Trigger for when the user presses the options button
    //Currently unimplemented
    private void _on_OptionsButton_pressed()
    {
        
    }

    //Trigger for closing the game
    //Has code for making sure there is no open server however all servers should be closed/disconnected if you get here.
    //Its just a precaution
    private void _on_ExitButton_pressed()
    {
        if (GetTree().HasNetworkPeer())
        {
            if (GetTree().GetNetworkUniqueId() == 1)    //Is Server host
            {
                Rpc("CloseServer");
            }
            else    //Is client
            {
                RpcId(1, "player_disconnecting");
            }
        }
        GetTree().Quit();
    }
}
