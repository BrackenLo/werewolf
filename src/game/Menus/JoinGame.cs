using Godot;
using System;

public class JoinGame : PanelContainer
{
    //===========================================================================

    private LineEdit ip_address_node;
    private LineEdit port_node;

    private Label error_node;

    private const string DEFAULT_IP_ADDRESS = "127.0.0.1";
    private const int DEFAULT_PORT = 59543;

    //===========================================================================

    private string ip_address;
    private int port;

    //===========================================================================

    public override void _Ready()
    {
        ip_address_node = (LineEdit)GetNode("PanelContainer/VBoxContainer/GridContainer/IPAddressData");
        port_node = (LineEdit)GetNode("PanelContainer/VBoxContainer/GridContainer/PortData");

        error_node = (Label)GetNode("PanelContainer/VBoxContainer/ErrorLabelNode");

        if (GameData.ip_address != null && GameData.ip_address != DEFAULT_IP_ADDRESS)
        {
            ip_address_node.Text = GameData.ip_address;
        }
        if (GameData.port != -1 && GameData.port != DEFAULT_PORT)
        {
            port_node.Text = $"{GameData.port}";
        }
    }


    //Trigger for when the user presses the join game button
    private void _on_JoinButton_pressed()
    {
        //Remove all excess empty space from the ip address and port
        ip_address_node.Text.Trim();
        port_node.Text.Trim();

        //If the user has left the IP address and/or port space empty, use the default values
        if (ip_address_node.Text == "") ip_address = DEFAULT_IP_ADDRESS;
        else                            ip_address = ip_address_node.Text;

        if (port_node.Text == "")
        {
            port = DEFAULT_PORT;
        }
        else
        {
            try
            {
                port = Int32.Parse(port_node.Text); //If the port has an inputted value, check to see if it is actually a number and not a string
            }
            catch (FormatException)
            {
                GD.PrintS("Port must be between 1025 - 65534");
                error_node.Text = "Port must be between 1025 - 65534";
                return;
            }

            if (port < 1025 || port > 65534)    //Also, make sure the port is between the following values. These are the only values I belive are open/available/unused usually
            {
                GD.PrintS("Port must be between 1025 - 65534");
                error_node.Text = "Port must be between 1025 - 65534";
                return;
            }
        }

        //If the code has reached here, everything should be good
        //Set the state to client if it isnt already and also set the ip address and port before changing scene to game room where we will connect
        error_node.Text = "";
        GameData.state = GameData.multiplayer_state.Client;

        GameData.ip_address = ip_address;
        GameData.port = port;

        GameData.role_list = null;

        GetTree().ChangeScene("res://src/game/GameRoom.tscn");
    }

    //Trigger for returning to the main menu
    private void _on_ExitButton_pressed()
    {
        GetTree().ChangeScene("res://src/game/Menus/MainMenu.tscn");
    }
}
