using Godot;
using System;
using System.Collections.Generic;

public class Playspace : Node2D
{
    
    //============================================================================

    //============================================================================

    private GUI gui;

    private CardController card_controller;
    private GameController game_controller;
    private PlayerController player_controller;

    private CanvasModulate blind;
    private Tween tween;
    private Camera2D scene_camera;

    //============================================================================

    public bool debugging = false;

    private bool blinded = true;
    private Color blinded_color = new Color(0.2F, 0.2F, 0.2F, 1);
    private Color unblinded_color = new Color(1, 1, 1, 1);

    //============================================================================

    CardDatabase.roles original_role;
    int player_position;

    //============================================================================

    private bool game_started = false;  //variable to make sure game does not start prematurely
    private bool allow_messaging = false;

    //============================================================================

    public override void _Ready()
    {
        gui = (GUI)GetNode("GUI");
        gui.Connect("TextSubmitted", this, nameof(_send_message));

        card_controller = (CardController)GetNode("CardController");
        game_controller = (GameController)GetNode("GameController");
        player_controller = (PlayerController)GetNode("PlayerController");

        blind = (CanvasModulate)GetNode("CanvasModulate");
        tween = (Tween)GetNode("Tween");
        scene_camera = (Camera2D)GetNode("Camera2D");

        gui.set_visible(false);
        blind.Color = blinded_color;
        blinded = true;

        //Code to run for debugging purposes when this scene was run using f6
        if (GetParent() == GetTree().Root)
        {
            GD.PrintS("Entering debug mode");
            _debugging_mode();
        }
    }

    private void _debugging_mode()
    {
        if (GetParent() == GetTree().Root)
        {
            NetworkedMultiplayerENet peer = new NetworkedMultiplayerENet();
            peer.CreateServer(59543, 1);
            GetTree().NetworkPeer = peer;

            GameData.role_list = new List<CardDatabase.roles>()
            {
                CardDatabase.roles.werewolf,
                CardDatabase.roles.werewolf,
                CardDatabase.roles.seer,
                CardDatabase.roles.robber,
                CardDatabase.roles.troublemaker,
                CardDatabase.roles.insomniac,
                CardDatabase.roles.tanner,
                CardDatabase.roles.drunk,
            };
            GameData.player_data_list = new List<Server.player_data>()
            {
                new Server.player_data(1, "Bleb", new Color(1, 0, 0), 0),
                new Server.player_data(3952, "Cannon", new Color(0, 1, 0), 1),
                new Server.player_data(592752, "Friend", new Color(0, 0, 1), 2),
                new Server.player_data(1248907, "The Tester", new Color(1, 0, 1), 3),
                new Server.player_data(8912, "Gravy", new Color(1, 1, 0), 4),
            };

            GameData.center_card_count = 3;
            GameData.player_count = GameData.player_data_list.Count;
            GameData.guaranteed_werewolf = true;

            debugging = true;

            start_game();
            
        }
    }

    //============================================================================

    public void start_game()
    {
        game_started = true;

        card_controller.init(GameData.center_card_count, GameData.player_count);
        game_controller.init(this, card_controller, player_controller, GameData.role_list, GameData.player_data_list);

        player_controller.add_players(GameData.player_data_list);

        //----------------------------------------------

        if (IsNetworkMaster())
        {
            game_controller.start_game_controller();
        }
    }

    private void _blind_player()
    {
        blinded = true;
        tween.InterpolateProperty(blind, "color", blind.Color, blinded_color, 1);
        tween.Start();
    }

    private void _unblind_player()
    {
        blinded = false;
        tween.InterpolateProperty(blind, "color", blind.Color, unblinded_color, 1);
        tween.Start();
    }

    private void _send_message(string message)
    {
        if (allow_messaging)
        {
            Rpc("_recieve_player_message", message);
        }
    }

    [RemoteSync]
    private void _recieve_player_message(string message)
    {
        if (allow_messaging)
        {
            gui.add_message(player_controller.player_data_dict[GetTree().GetRpcSenderId()].player_name, message);
        }
    }

    public void send_server_message(string message)
    {
        if (IsNetworkMaster())
            Rpc("_recieve_server_message", message);
    }

    public void send_server_message(int target, string message)
    {
        if (IsNetworkMaster())
            RpcId(target, "_recieve_server_message", message);
    }

    [RemoteSync]
    private void _recieve_server_message(string message)
    {
        if (GetTree().GetRpcSenderId() == 1)
            gui.add_message(message);
    }

    public void set_player_messaging(bool value)
    {
        if (IsNetworkMaster())
            Rpc("_set_player_messaging", value);
    }

    [RemoteSync]
    private void _set_player_messaging(bool value)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            allow_messaging = value;
            gui.set_text_submission(value);
        }
    }

    //===============================================================================================
    //===============================================================================================
    //===============================================================================================
    //Gui methods used by GameController

    public void reset_player_positions()
    {
        if (IsNetworkMaster())
            Rpc("_reset_player_positions");
    }

    [RemoteSync]
    private void _reset_player_positions()
    {
        player_controller.set_center_area(card_controller.center_card_area);

        float cam_x = -card_controller.center_card_area.x * 0.5F;
        float cam_y = 0;
        scene_camera.Position = new Vector2(cam_x, cam_y);
    }


    public void set_role_timer(int time)
    {
        if (IsNetworkMaster())
            Rpc("_recieve_timer", time);
    }

    [RemoteSync]
    private void _recieve_timer(int time)
    {
        if (GetTree().GetRpcSenderId() == 1)
            gui.start_timer(time);
    }

    public void set_current_turn(string turn)
    {
        if (IsNetworkMaster())
            Rpc("_recieve_turn", turn);
    }

    [RemoteSync]
    private void _recieve_turn(string turn)
    {
        if (GetTree().GetRpcSenderId() == 1)
            gui.set_current_turn(turn);
    }

    public void add_game_help(string text)
    {
        if (IsNetworkMaster())
            Rpc("_add_game_help", text);
    }

    [RemoteSync]
    private void _add_game_help(string text)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            gui.add_game_help(text);
        }
    }

    public void set_game_help(string text)
    {
        if (IsNetworkMaster())
            Rpc("_set_game_help", text);
    }

    [RemoteSync]
    private void _set_game_help(string text)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            gui.set_game_help(text);
        }
    }

    public void send_player_position(int target, int position)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_recieve_position", position);
        }
    }

    [RemoteSync]
    private void _recieve_position(int position)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            this.player_position = position;
        }
    }

    public void send_player_role(int target, CardDatabase.roles role)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_recieve_role", role);
        }
    }

    [RemoteSync]
    private void _recieve_role(CardDatabase.roles role)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            gui.set_game_help("Take this time to review and remember your card");

            original_role = role;

            CardDatabase cdb = new CardDatabase();

            gui.set_original_role(cdb.role_data[role].card_name);
            gui.set_original_team($"{cdb.role_data[role].card_team}");

            card_controller.load_and_reveal_card(player_position, role);

            gui.set_visible(true);
            _unblind_player();
        }
    }

    //===============================================================================================
    //===============================================================================================
    //===============================================================================================
    //Card Manipulation used by GameController

    public void reveal_card_at_position(int target, int playspace_pos, CardDatabase.roles role)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_reveal_card", playspace_pos, role);
        }
    }

    public void reveal_card_at_position(int playspace_pos, CardDatabase.roles role)
    {
        if (IsNetworkMaster())
        {
            Rpc("_reveal_card", playspace_pos, role);
        }
    }

    [RemoteSync]
    private void _reveal_card(int playspace_pos, CardDatabase.roles role)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            card_controller.load_and_reveal_card(playspace_pos, role);
        }
    }

    public void hide_all_cards()
    {
        if (IsNetworkMaster())
            Rpc("_hide_all_cards");
    }

    [RemoteSync]
    private void _hide_all_cards()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            card_controller.hide_all_cards();
        }
    }

    public void reset_board_state()
    {
        if (IsNetworkMaster())
        {
            Rpc("_reset_board_state");
        }
    }

    [RemoteSync]
    private void _reset_board_state()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            card_controller.hide_all_cards();
            card_controller.set_selecting_cards(false);
            card_controller.clear_selected_cards();
            player_controller.set_players_spotlights(false);
            player_controller.clear_selected_players();
            player_controller.set_selecting_players(false);
            gui.set_game_help("");
        }
    }

    public void set_spotlight_player(int target, bool value)
    {
        if (IsNetworkMaster())
        {
            Rpc("_set_spotlight_player", target, value);
        }
    }

    [RemoteSync]
    private void _set_spotlight_player(int target, bool value)
    {
        player_controller.set_player_spotlight(target, value);
    }

    //===============================================================================================
    //===============================================================================================
    //===============================================================================================
    //Turn data (also used by GameController)

    public void set_blind_players(bool value)
    {
        if (IsNetworkMaster())
        {
            if (value)
            {
                Rpc("_blind_player_rpc");
            }
            else
            {
                Rpc("_unblind_player_rpc");
            }
        }
    }

    [RemoteSync]
    private void _blind_player_rpc()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            _blind_player();
        }
    }

    [RemoteSync]
    private void _unblind_player_rpc()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            _unblind_player();
        }
    }


    public void start_player_voting()
    {
        if (IsNetworkMaster())
        {
            Rpc("_start_player_voting");
        }
    }

    [RemoteSync]
    private void _start_player_voting()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            card_controller.set_cards_light_masks(524287);

            player_controller.set_selecting_players(true);
            player_controller.Connect("PlayerSelected", this, "_player_selected");
        }
    }

    private void _player_selected(int player_id)
    {
        player_controller.set_selecting_players(false);
        player_controller.Disconnect("PlayerSelected", this, "_player_selected");

        game_controller.request_player_vote(player_id);
    }

    //=====================================================================================
    //=============== The Werewolves ======================================================
    //=====================================================================================

    public void start_werewolf_team(int[] team)
    {
        if (IsNetworkMaster())
        {
            foreach(int id in team)
            {
                RpcId(id, "_start_werewolf_team", team);
            }
        }
    }

    [RemoteSync]
    private void _start_werewolf_team(int[] team)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            if (team.Length > 1)
            {
                List<Server.player_data> other_werewolves = new List<Server.player_data>();

                GD.PrintS("Your other werewolves are:");
                foreach (int id in team)
                {   
                    player_controller.set_player_spotlight(id, true);

                    Server.player_data player_data = player_controller.player_data_dict[id];

                    if (id != GetTree().GetNetworkUniqueId())
                    {
                        GD.PrintS(player_data.player_name);
                        other_werewolves.Add(player_data);
                    }
                }

                if (other_werewolves.Count == 1)
                {
                    gui.set_game_help($"You have woken up.\nYour fellow werewolf is {other_werewolves[0].player_name}");
                }
                else
                {
                    string name_string = other_werewolves[0].player_name;
                    for (int x = 1; x < other_werewolves.Count - 1; x++)
                    {
                        name_string += $", {other_werewolves[x]}";
                    }

                    gui.set_game_help($"You have woken up.\nYour fellow werewolves are {name_string} and {other_werewolves[other_werewolves.Count -1].player_name}.");
                }
            }
        }
    }

    public void start_werewolf_solo(int target)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_start_werewolf_solo");
        }
    }

    [RemoteSync]
    private void _start_werewolf_solo()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            gui.set_game_help("You have woken up.\nThere is nobody else awake.\nChoose one of the center cards to look at");

            //GD.PrintS("You have woken up");
            _unblind_player();
            //GD.PrintS("There is nobody else awake");
            //GD.PrintS("Choose one of the center cards to look at");

            card_controller.Connect("SelectedCardsChanged", this, "_werewolf_card_selected");
            card_controller.set_selecting_center_cards(true);
        }
    }

    private void _werewolf_card_selected(int[] selected_cards)
    {
        card_controller.set_selecting_cards(false);
        card_controller.Disconnect("SelectedCardsChanged", this, "_werewolf_card_selected");
        card_controller.clear_selected_cards();

        game_controller.request_card_data(selected_cards[0]);
    }

    //=====================================================================================
    //================ The Seer ===========================================================
    //=====================================================================================

    public void start_seer(int target)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_start_seer");
        }
    }

    [RemoteSync]
    private void _start_seer()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            _unblind_player();
            GD.PrintS("Choose two of the center cards or another players card to look at");

            gui.set_game_help("You have woken up.\nChoose two of the center cards or another players card to look at.");


            card_controller.Connect("SelectedCardsChanged", this, "_seer_card_selected");
            card_controller.set_selecting_center_cards(true);
            card_controller.set_selecting_player_cards(true, new int[]{player_position});
        }
    }

    private void _seer_card_selected(int[] selected_cards)
    {
        if (selected_cards.Length == 0)
        {
            card_controller.set_selecting_center_cards(true);
            card_controller.set_selecting_player_cards(true, new int[]{player_position});
        }
        else
        {
            if (selected_cards[0] >= GameData.center_card_count)    //Its a player card
            {
                card_controller.set_selecting_cards(false);
                card_controller.Disconnect("SelectedCardsChanged", this, "_seer_card_selected");
                card_controller.clear_selected_cards();

                game_controller.request_card_data(selected_cards[0]);
            }
            else if (selected_cards.Length == 2)
            {
                card_controller.set_selecting_cards(false);
                card_controller.Disconnect("SelectedCardsChanged", this, "_seer_card_selected");
                card_controller.clear_selected_cards();

                game_controller.request_card_data(selected_cards[0]);
                game_controller.request_card_data(selected_cards[1]);
            }
            else if (selected_cards[0] < GameData.center_card_count) //Its a center card
            {
                card_controller.set_selecting_player_cards(false, new int[]{});
            }
        }
    }

    //=====================================================================================
    //================ The Robber =========================================================
    //=====================================================================================

    public void start_robber(int target)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_start_robber");
        }
    }

    [RemoteSync]
    private void _start_robber()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            _unblind_player();
            GD.PrintS("Choose another players card and swap yours with it. View your new card");

            gui.set_game_help("You have woken up.\nChoose another players card and swap yours with it. View your new card.");

            card_controller.Connect("SelectedCardsChanged", this, "_robber_card_selected");
            card_controller.set_selecting_player_cards(true, new int[]{player_position});
        }
    }

    private void _robber_card_selected(int[] selected_cards)
    {
        if (selected_cards.Length > 0)
        {
            card_controller.set_selecting_cards(false);
            card_controller.Disconnect("SelectedCardsChanged", this, "_robber_card_selected");
            card_controller.clear_selected_cards();

            card_controller.swap_cards(player_position, selected_cards[0]);
            game_controller.request_card_swap(player_position, selected_cards[0]);
            game_controller.request_card_data(player_position);
        }
    }

    //=====================================================================================
    //================ The Seer ===========================================================
    //=====================================================================================

    public void start_troublemaker(int target)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_start_troublemaker");
        }
    }

    [RemoteSync]
    private void _start_troublemaker()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            _unblind_player();
            GD.PrintS("Choose two players cards and swap them");

            gui.set_game_help("You have woken up.\nChoose two players cards and swap them");

            card_controller.Connect("SelectedCardsChanged", this, "_troublemaker_card_selected");
            card_controller.set_selecting_player_cards(true, new int[]{player_position});
        }
    }

    private void _troublemaker_card_selected(int[] selected_cards)
    {
        if (selected_cards.Length == 2)
        {
            card_controller.set_selecting_cards(false);
            card_controller.Disconnect("SelectedCardsChanged", this, "_troublemaker_card_selected");
            card_controller.clear_selected_cards();

            card_controller.swap_cards(selected_cards[0], selected_cards[1]);
            game_controller.request_card_swap(selected_cards[0], selected_cards[1]);
        }
    }

    //=====================================================================================
    //================ The Drunk ======================================================
    //=====================================================================================

    public void start_drunk(int target)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_start_drunk");
        }
    }

    [RemoteSync]
    private void _start_drunk()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            _unblind_player();
            GD.PrintS("Choose a center card to swap yours with");

            gui.set_game_help("You have woken up.\nChoose a center card to swap yours with.");

            card_controller.Connect("SelectedCardsChanged", this, "_drunk_card_selected");
            card_controller.set_selecting_center_cards(true);
        }
    }

    private void _drunk_card_selected(int[] selected_cards)
    {
        card_controller.set_selecting_cards(false);
        card_controller.Disconnect("SelectedCardsChanged", this, "_drunk_card_selected");
        card_controller.clear_selected_cards();

        card_controller.swap_cards(player_position, selected_cards[0]);
        game_controller.request_card_swap(player_position, selected_cards[0]);
    }

    //=====================================================================================
    //================ The Insomniac ======================================================
    //=====================================================================================

    public void start_insomniac(int target)
    {
        if (IsNetworkMaster())
        {
            RpcId(target, "_start_insomniac");
        }
    }

    [RemoteSync]
    private void _start_insomniac()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            _unblind_player();
            GD.PrintS("You get to look at your card to see if it has changed");

            gui.set_game_help("You have woken up.\nYou get to look at your card to see if it has changed.");

            game_controller.request_card_data(player_position);
        }
    }

    //=====================================================================================
    //=====================================================================================


}
