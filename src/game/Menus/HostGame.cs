using Godot;
using System;
using System.Collections.Generic;

public class HostGame : PanelContainer
{
    public VBoxContainer role_selection_box;

    public Label role_count_node;
    public Label player_count_node;

    private LineEdit port_node;
    private LineEdit password_node;
    private CheckButton werewolf_node;

    private SpinBox discussion_time;
    private SpinBox role_time;
    private SpinBox diff_role_time;

    private Label warning_label_node;
    private Button start_button_node;

    //==========================================================

    private Dictionary<CardDatabase.roles, HostGameSelectionButton> selection_buttons = new Dictionary<CardDatabase.roles, HostGameSelectionButton>();

    //==========================================================

    private int center_cards_count = 3;

    private int role_count = 0;
    private int player_count = 0;
    private const int minimum_player_count = 1;

    //==========================================================

    private const int DEFAULT_PORT = 59543;

    private int port;

    //==========================================================

    //Function called on scene creation
    //Used for assigning all the various parts of the menu to variables as well as creating buttons for each of the roles
    public override void _Ready()
    {
        //Assign menu parts to variables ------------------------

        role_selection_box = (VBoxContainer)GetNode("HBoxContainer/VBoxContainer/PanelContainer2/ScrollContainer/RoleSelectionArea");
        role_count_node = (Label)GetNode("HBoxContainer/VBoxContainer/PanelContainer/GridContainer/RoleCountData");
        player_count_node = (Label)GetNode("HBoxContainer/VBoxContainer/PanelContainer/GridContainer/PlayerCountData");

        port_node = (LineEdit)GetNode("HBoxContainer/PanelContainer/NinePatchRect/VBoxContainer/GridContainer/PortData");
        password_node = (LineEdit)GetNode("HBoxContainer/PanelContainer/NinePatchRect/VBoxContainer/GridContainer/PasswordData");
        werewolf_node = (CheckButton)GetNode("HBoxContainer/PanelContainer/NinePatchRect/VBoxContainer/GridContainer/GuaranteedWerewolfData");

        discussion_time = (SpinBox)GetNode("HBoxContainer/PanelContainer/NinePatchRect/VBoxContainer/GridContainer/DiscussionTimeData");
        role_time = (SpinBox)GetNode("HBoxContainer/PanelContainer/NinePatchRect/VBoxContainer/GridContainer/RoleTimeData");
        diff_role_time = (SpinBox)GetNode("HBoxContainer/PanelContainer/NinePatchRect/VBoxContainer/GridContainer/DiffRoleTimeData");

        warning_label_node = (Label)GetNode("HBoxContainer/PanelContainer/NinePatchRect/WarningLabel");
        start_button_node = (Button)GetNode("HBoxContainer/PanelContainer/NinePatchRect/HSplitContainer/StartButton");
        //-------------------------------------------------------

        //Create an array of enums that will help to create each of the buttons
        CardDatabase.roles[] values = (CardDatabase.roles[])Enum.GetValues(typeof(CardDatabase.roles));

        foreach(CardDatabase.roles role in values)  //Go through each of the roles
        {
            if (role == CardDatabase.roles.none)
                continue;

            //create a new button
            PackedScene packed = (PackedScene)ResourceLoader.Load("res://src/game/Menus/HostGameSelectionButton.tscn");
            HostGameSelectionButton selectionButton = (HostGameSelectionButton)packed.Instance();

            //Add the button to the menu and assign it its role
            role_selection_box.AddChild(selectionButton);
            selectionButton.initialise(this, role);

            selection_buttons.Add(role, selectionButton);
        }

        if (GameData.role_list != null)
        {
            GD.PrintS("GameData is not null", GameData.role_list.Count);
            foreach(CardDatabase.roles role in GameData.role_list)
            {
                selection_buttons[role].iterate_amount();
                selection_buttons[role].set_button_pressed(true);
            }
        }

        //-----------------------------------------------------------------------------

        discussion_time.Value = GameData.discussion_time;
        role_time.Value = GameData.normal_role_time;
        diff_role_time.Value = GameData.difficult_role_time;

        if (GameData.port != -1 && GameData.port != DEFAULT_PORT)
        {
            port_node.Text = $"{GameData.port}";
        }

        //-----------------------------------------------------------------------------

        //update all the menu statistics
        update_stats();
    }


    //Function for updating the various statistics on the menu such as how many players required for the number of roles selected etc.
    public void update_stats()
    {
        role_count = 0;
        player_count = 0;

        //Count how many roles are currently activated
        foreach(Node node in role_selection_box.GetChildren())
        {
            if (node is HostGameSelectionButton button)
            {
                if (button.role_enabled)
                {
                    role_count += button.current_amount;
                }
            }
        }

        //Math for how many required players.
        player_count = role_count - center_cards_count;

        //Display the number or roles and number of players if valid. If invalid, tell the user
        role_count_node.Text = $"{role_count}";
        if (player_count < minimum_player_count)
        {
            player_count_node.Text = "n/a - Not enough roles";
        }
        else
        {
            player_count_node.Text = $"{player_count}";
        }

        //Function for enabling the start button if all good
        _check_stats_valid();
    }

    //Trigger for the port input area to check if its valid on each keystroke
    private void _on_PortData_text_changed(string new_val)
    {
        _check_stats_valid();
    }

    //Function for checking each aspect of the menu to see if its correct.
    //If everything is fine, it will enable the start game button
    private void _check_stats_valid()
    {
        port_node.Text.Trim();
        //If the user has left the port node empty, use the default values
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
                //GD.PrintS("Port must be between 1025 - 65534");
                warning_label_node.Text = "Error: Port must be between 1025 - 65534";
                start_button_node.Disabled = true;
                return; //Returning so that the function doesn't finish as so cant enable the start button
            }

            if (port < 1025 || port > 65534) //Also, make sure the port is between the following values. These are the only values I belive are open/available/unused usually
            {
                //GD.PrintS("Port must be between 1025 - 65534");
                warning_label_node.Text = "Error: Port must be between 1025 - 65534";
                start_button_node.Disabled = true;
                return; //Returning so that the function doesn't finish as so cant enable the start button
            }
        }

        if (player_count < minimum_player_count)
        {
            start_button_node.Disabled = true;
            //GD.PrintS("minimum number of roles not reached");
            warning_label_node.Text = "Error: Minimum number of players not reached";
            return; //Returning so that the function doesn't finish as so cant enable the start button
        }

        //If the function has reached here, all should be good and the start button can be enabled.
        start_button_node.Disabled = false;
        warning_label_node.Text = "";
    }

    //Trigger for when the user presses the back button
    //Returns them to the main menu
    private void _on_BackButton_pressed()
    {
        _set_gamedata_roles();
        GetTree().ChangeScene("res://src/game/Menus/MainMenu.tscn");
    }

    //Trigger for when the user presses the start button
    //Should only be made available when everything is correctly inputted
    private void _on_StartButton_pressed()
    {
        //Set all values in the global class
        GameData.state = GameData.multiplayer_state.Host;

        GameData.port = port;
        GameData.player_count = player_count;
        GameData.center_card_count = center_cards_count;
        GameData.guaranteed_werewolf = werewolf_node.Pressed;

        GameData.discussion_time = (int)discussion_time.Value;
        GameData.normal_role_time = (int)role_time.Value;
        GameData.difficult_role_time = (int)diff_role_time.Value;

        _set_gamedata_roles();

        //After all assigned, Go to the main game gameroom
        GetTree().ChangeScene("res://src/game/GameRoom.tscn");
    }


    private void _set_gamedata_roles()
    {
        //Create a new empty array for the roles below
        GameData.role_list = new List<CardDatabase.roles>();

        //Go through each role button node and chech if its enabled and add that roll x times where x is how many the user entered
        foreach(Node node in role_selection_box.GetChildren())
        {
            if (node is HostGameSelectionButton role_select)
            {
                if (role_select.role_enabled)
                {
                    for (int x = 0; x < role_select.current_amount; x++)
                    {
                        GameData.role_list.Add(role_select.role);
                    }
                }
            }
        }
    }
}
