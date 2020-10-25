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

    //============================================================================

    public override void _Ready()
    {
        gui = (GUI)GetNode("GUI");

        card_controller = (CardController)GetNode("CardController");
        game_controller = (GameController)GetNode("GameController");
        player_controller = (PlayerController)GetNode("Players");

        blind = (CanvasModulate)GetNode("CanvasModulate");
        tween = (Tween)GetNode("Tween");

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
                CardDatabase.roles.robber,
                CardDatabase.roles.seer,
                CardDatabase.roles.werewolf,
                CardDatabase.roles.werewolf,
                CardDatabase.roles.troublemaker,
                CardDatabase.roles.werewolf
            };
            GameData.player_data_list = new List<Server.player_data>()
            {
                new Server.player_data(1, "Bleb", new Color(1, 0, 0), 0),
                new Server.player_data(3952, "Cannon", new Color(0, 1, 0), 1),
                new Server.player_data(592752, "Friend", new Color(0, 0, 1), 2)
            };

            GameData.center_card_count = 3;
            GameData.player_count = 3;
            GameData.guaranteed_werewolf = true;

            debugging = true;

            start_game();
            
        }
    }

    [Obsolete("Pending delete - not needed")]
    public void init(GameRoom parent, Client client)
    {

    }

    //============================================================================

    public void start_game()
    {
        game_started = true;

        card_controller.init(GameData.center_card_count, GameData.player_count);
        game_controller.init(this, card_controller, player_controller, GameData.role_list, GameData.player_data_list);

        //foreach(Server.player_data player in GameData.player_data_list)
        //{
        //    player_controller.add_player(player);
        //}
        player_controller.add_players(GameData.player_data_list);

        //----------------------------------------------

        if (IsNetworkMaster())
        {
            game_controller.start_game_controller();
        }
    }

    public override void _Process(float delta)
    {
        if (Input.IsActionJustPressed("ui_focus_next"))
        {
            if (blinded)
                _unblind_player();
            else
                _blind_player();
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

    //===============================================================================================
    //===============================================================================================
    //===============================================================================================
    //Methods used by GameController (Host)


    public void set_current_turn(string turn)
    {
        Rpc("_recieve_turn", turn);
    }

    [RemoteSync]
    private void _recieve_turn(string turn)
    {
        gui.set_current_turn(turn);
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
            original_role = role;

            CardDatabase cdb = new CardDatabase();

            gui.set_original_role(cdb.role_data[role].card_name);
            gui.set_original_team($"{cdb.role_data[role].card_team}");

            card_controller.load_and_reveal_player_card(player_position, role);

            gui.set_visible(true);
            _unblind_player();
        }
    }

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

    //===============================================================================================
    //===============================================================================================
    //===============================================================================================
    //Turn data (also used by GameController)

    public void blind_players(bool value)
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

    public void wake_up_player_ids(int[] ids)
    {
        foreach(int id in ids)
        {
            RpcId(id, "_wake_up", ids);
        }
    }

    [RemoteSync]
    private void _wake_up(int[] other_awake)
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            GD.PrintS("You have woken up");
            _unblind_player();
            if (other_awake.Length > 1)
            {
                GD.PrintS("Other players who have woken up: ");
                foreach (int id in other_awake)
                {   
                    Server.player_data player_data = player_controller.player_data_dict[id];

                    if (id != GetTree().GetNetworkUniqueId())
                    {
                        GD.PrintS(player_data.player_name);
                    }
                }
            }
            else
            {
                GD.PrintS("There is nobody else awake");
            }
        }
    }

    //=====================================================================================
    //=====================================================================================

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

    [RemoteSync]
    private void _start_troublemaker_action()
    {
        if (GetTree().GetRpcSenderId() == 1)
        {
            card_controller.set_selecting_player_cards(true, new int[]{player_position});
        }
    }

    //=====================================================================================
    //=====================================================================================


}
