using Godot;
using System;

public class UsernameSelectScreen : Control
{

    //This Control class is included with the GameRoom
    //It appears once the user is connected to the server / created a server 
    //It prompts them to create a username and select a color and checks if they are available with the server

    //===================================================================

    private LineEdit username;
    private GridContainer button_container;
    private ButtonGroup group;

    private Panel connection_panel;
    private Panel waiting_panel;

    private String[] color_list = 
    {
        "#FFFFFF", "#808080", "#000000", "#FF0000",
        "#800000", "#FFFF00", "#808000", "#00FF00",
        "#008000", "#00FFFF", "#008080", "#0000FF",
        "#000080", "#FF00FF", "#800080", "#d35400"
    };

    //===================================================================

    private Server server;

    //===================================================================

    public override void _Ready()
    {
        username = (LineEdit)GetNode("MarginContainer/PanelContainer/VBoxContainer/UsernameData");
        button_container = (GridContainer)GetNode("MarginContainer/PanelContainer/VBoxContainer/CenterContainer/ColorButtonGridData");
        group = new ButtonGroup();

        connection_panel = (Panel)GetNode("MarginContainer/PanelContainer/ConnectionPanel");
        waiting_panel = (Panel)GetNode("MarginContainer/PanelContainer/WaitingPanel");
    }

    //Initialisation function called by GameRoom
    //Gets the number of players so it can show a certain number of colors.
    //Also gets a reference to the server so that it can query username and color availability
    public void init(Server server, int total_players)
    {
        this.server = server;

        //Go through each of the players
        //Create a button with a color rect on top of it for each.
        //Only one button can be selected at a time 
        for(int x = 0; x < total_players; x++)
        {
            Button new_button = new Button();
            new_button.ToggleMode = true;
            new_button.RectMinSize = new Vector2(40, 40);
            new_button.Group = group;

            button_container.AddChild(new_button);

            ColorRect color = new ColorRect();
            color.RectSize = new Vector2(30, 30);
            color.RectPosition = new Vector2(5, 5);
            color.MouseFilter = MouseFilterEnum.Ignore;
            color.Color = new Color(color_list[x]);

            new_button.AddChild(color);
        }

        //Hides the overlaying 'connection pending' screen onces this has been setup
        connection_panel.Visible = false;
    }

    //Trigger for when the user presses start game
    //Checks if all data is correct/inputted before proceeding
    private void _on_StartButton_pressed()
    {
        username.Text.Trim();   //Remove excess space from username
        if (username.Text.Length <= 1)  //Make sure username is long enough to be considered
        {
            return;
        }

        if (group.GetPressedButton() == null)   //Make sure a color is selcted
        {
            return;
        }
        
        Color color;

        if (group.GetPressedButton().GetChild(0) is ColorRect cr)
        {
            color = cr.Color;   //Get the selected color
        }
        else    //I dont know how you would get here. Included just in case
        {
            return;
        }

        //Query the selected data with the server
        server.check_user_details(username.Text, color);
        
    }

    //Called in the server class
    //Tells the user that the username is taken
    public void username_taken()
    {
        GD.PrintS("Username is taken");             //=-=-=-=-=-=-=-=           Change me pls - need a label in the scene for telling the user
    }

    //Called in the server class
    //Tells the user that the color is taken
    public void color_taken()
    {
        GD.PrintS("Color has been taken");             //=-=-=-=-=-=-=-=           Change me pls - need a label in the scene for telling the user
    }


    //Called in the server class
    //Allows the user to procede with all data validated and unique
    public void details_validated()
    {
        //Visible = false;
        waiting_panel.Visible = true;
    }
}
