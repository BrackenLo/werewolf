using Godot;
using System;
using System.Collections.Generic;

public class GameController : Node
{

    //=====================================================================

    private Playspace playspace;
    private CardController card_controller;
    private PlayerController player_controller;

    //=====================================================================

    private bool initialized = false;

    //=====================================================================
    
    private List<CardDatabase.roles> original_role_list;    //all the roles original at start (same as GameData?)
    private List<Server.player_data> player_list;   //List of players in order?
    private List<CardDatabase.roles> role_assignment;   //The list of roles representative of where they are in game
    private Dictionary<int, CardDatabase.roles> player_original_roles; //What players were originally where int is player_id

    //=====================================================================

    public CardDatabase.roles current_role {get;private set;} = CardDatabase.roles.werewolf;

    private List<int> pending_data_from = new List<int>();

    //=====================================================================

    public void init(Playspace playSpace, CardController cardController, PlayerController playerController, List<CardDatabase.roles> new_role_list, List<Server.player_data> players)
    {
        playspace = playSpace;
        card_controller = cardController;
        player_controller = playerController;
        original_role_list = new_role_list;
        player_list = players;
        initialized = true;
    }

    public async void start_game_controller()
    {
        if (IsNetworkMaster() && initialized)
        {
            CardDatabase cdb = new CardDatabase();
            RandomNumberGenerator rand = new RandomNumberGenerator();
            rand.Randomize();

            int center_count = GameData.center_card_count;

            //------------------------------------------
            //Sort out the role list

            //role_assignment = shuffle_role_list(rand, original_role_list);
            role_assignment = original_role_list;

            if (GameData.guaranteed_werewolf)
                role_assignment = add_guaranteed_werewolf(cdb, rand, role_assignment, center_count);
            
            player_original_roles = assign_original_roles(role_assignment, player_list, center_count);

            //------------------------------------------
            //Create all the cards

            create_all_cards(card_controller, GameData.player_count + GameData.center_card_count);

            //--------------------------------------------------------------------
            //Wait a while for game to start (players get ready)

            await ToSignal(GetTree().CreateTimer(2), "timeout");

            //------------------------------------------
            //Send players their role data

            GD.PrintS("Review your card turn");
            playspace.set_current_turn("Review your card");

            send_player_positions(player_list, center_count);
            send_original_roles(player_original_roles, player_list, center_count);

            //--------------------------------------------------------------------
            //Wait for players to read their cards and prepare

            await ToSignal(GetTree().CreateTimer(5), "timeout");
            playspace.blind_players(true);
            playspace.hide_all_cards();
            await ToSignal(GetTree().CreateTimer(3), "timeout");


            //--------------------------------------------------------------------
            //Start werewolves

            GD.PrintS("Werewolf turn");
            playspace.set_current_turn("Werewolves");
            current_role = CardDatabase.roles.werewolf;
            List<int> werewolves = find_role_in_players(CardDatabase.roles.werewolf, role_assignment, center_count);
            //werewolves.AddRange(find_role(CardDatabase.roles.alpha_wolf, role_assignment));       //Pending alpha wolf implementation

            List<int> werewolf_ids = player_controller.player_position_to_id(werewolves);
            
            playspace.wake_up_player_ids(werewolf_ids.ToArray());    
            if (werewolves.Count == 1)
            {
                pending_data_from.Add(werewolf_ids[0]);
                playspace.start_werewolf_solo(werewolf_ids[0]);


                await ToSignal(GetTree().CreateTimer(15), "timeout");
                playspace.hide_all_cards();
            }
            else
            {
                await ToSignal(GetTree().CreateTimer(5), "timeout");
            }
            playspace.blind_players(true);
            await ToSignal(GetTree().CreateTimer(3), "timeout");

            //--------------------------------------------------------------------




            //--------------------------------------------------------------------
        }
    }

    //===========================================================================
    //===========================================================================

    private List<CardDatabase.roles> shuffle_role_list(RandomNumberGenerator rand, List<CardDatabase.roles> unshuffled_list)
    {
        List<CardDatabase.roles> shuffled_list = new List<CardDatabase.roles>();

        int indexList = unshuffled_list.Count - 1;

        for (int z = 0; z <= indexList; z++)
        {
            int x = (int)rand.RandiRange(0, unshuffled_list.Count - 1);
            shuffled_list.Add(unshuffled_list[x]);
            unshuffled_list.RemoveAt(x);
        }
            
        return shuffled_list;
    }

    private void create_all_cards(CardController controller, int cards_to_create)
    {
        for (int x = 0; x < cards_to_create; x++)
        {
            controller.add_card();
        }
    }

    private List<CardDatabase.roles> add_guaranteed_werewolf(CardDatabase cdb, RandomNumberGenerator rand, List<CardDatabase.roles> role_list, int center_cards)
    {
        int werewolf_position = -1;

        for (int x = 0; x < role_list.Count; x++)
        {
            if (cdb.role_data[role_list[x]].card_team == CardDatabase.teams.werewolves)
            {
                if (x >= center_cards)
                {
                    return role_list;
                }
                else
                {
                    werewolf_position = x;
                }
            }
        }

        if (werewolf_position != -1)
        {
            int rand_player_pos = rand.RandiRange(center_cards, role_list.Count - 1);
            CardDatabase.roles temp = role_list[werewolf_position];

            role_list[werewolf_position] = role_list[rand_player_pos];
            role_list[rand_player_pos] = temp;

            return role_list;
        }
        else
        {
            return role_list;
        }
    }

    private Dictionary<int, CardDatabase.roles> assign_original_roles(List<CardDatabase.roles> roles, List<Server.player_data> players, int center_cards)
    {
        Dictionary<int, CardDatabase.roles> player_role_assignment = new Dictionary<int, CardDatabase.roles>();
        int card_index = center_cards;

        for (int x = 0; x < players.Count; x++)
        {
            player_role_assignment.Add(players[x].player_id, roles[card_index]);
            card_index++;
        }

        return player_role_assignment;
    }

    //===========================================================================
    //===========================================================================

    private void send_player_positions(List<Server.player_data> players, int center_count)
    {
        int player_index = 0;
        for (int x = center_count; x < players.Count; x++)
        {
            playspace.send_player_position(players[player_index].player_id, x);
            player_index++;
        }
    }

    private void send_original_roles(Dictionary<int, CardDatabase.roles> player_roles, List<Server.player_data> players, int center_count)
    {
        foreach(KeyValuePair<int, CardDatabase.roles> player_data in player_roles)
        {
            int index = -1;
            for (int x = 0; x < players.Count; x++)
            {
                if (players[x].player_id == player_data.Key)
                {
                    index = x;
                }
            }

            playspace.send_player_role(player_data.Key, player_data.Value);
            //playspace.reveal_card_at_position(player_data.Key, center_count + index, player_data.Value);
        }
    }

    private void reveal_all_roles()
    {
        for (int x = 0; x < role_assignment.Count; x++)
        {
            playspace.reveal_card_at_position(x, role_assignment[x]);
        }
    }

    //===========================================================================
    //===========================================================================
    //Card finding functions

    private List<int> find_role(CardDatabase.roles to_find, List<CardDatabase.roles> roles)
    {

        List<int> found_roles = new List<int>();

        for (int x = 0 ; x < roles.Count; x++)
        {
            if (roles[x] == to_find)
            {
                found_roles.Add(x);
            }
        }

        return found_roles;
    }

    //Returns a list of positions for the given role from the center
    private List<int> find_role_in_center(CardDatabase.roles to_find, List<CardDatabase.roles> roles, int center_count)
    {
        List<int> found_roles = new List<int>();

        for (int x = 0; x < center_count; x++)
        {
            if (roles[x] == to_find)
            {
                found_roles.Add(x - center_count);
            }
        }

        return found_roles;
    }

    //Returns a list of player positions for the given role
    private List<int> find_role_in_players(CardDatabase.roles to_find, List<CardDatabase.roles> roles, int center_count)
    {
        List<int> found_roles = new List<int>();

        for (int x = center_count; x < roles.Count; x++)
        {
            if (roles[x] == to_find)
            {
                found_roles.Add(x - center_count);
            }
        }

        return found_roles;
    }


    //===========================================================================
    //===========================================================================

    public void request_card_data(int card_position)
    {
        RpcId(1, "_request_card_data", card_position);
    }

    [RemoteSync]
    private void _request_card_data(int card_position)
    {
        if (pending_data_from.Contains(GetTree().GetRpcSenderId()))
        {
            GD.PrintS("Approved request from", GetTree().GetRpcSenderId());
            playspace.reveal_card_at_position(GetTree().GetRpcSenderId(), card_position, role_assignment[card_position]);

            if (current_role == CardDatabase.roles.werewolf)
            {
                pending_data_from.Clear();
            }
        }
        else
        {
            GD.PrintS("Denied request from", GetTree().GetRpcSenderId());
        }
    }

    //===========================================================================
    //===========================================================================

}
