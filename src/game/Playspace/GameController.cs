using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    private List<int> pending_data_cat1 = new List<int>();
    private List<int> pending_data_cat2 = new List<int>();

    private bool voting_turn = false;
    private Dictionary<int, int> player_id_to_votes = new Dictionary<int, int>();
    private Dictionary<int, int> player_id_to_other_id = new Dictionary<int, int>();

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
            //--------------------------------------------------------------------
            //--------------------------------------------------------------------
            //--------------------------------------------------------------------

            CardDatabase cdb = new CardDatabase();
            RandomNumberGenerator rand = new RandomNumberGenerator();
            rand.Randomize();

            int center_count = GameData.center_card_count;

            //------------------------------------------
            //Sort out the role list

            role_assignment = shuffle_role_list(rand, original_role_list);

            if (GameData.guaranteed_werewolf)
                role_assignment = add_guaranteed_werewolf(cdb, rand, role_assignment, center_count);
            
            player_original_roles = assign_original_roles(role_assignment, player_list, center_count);

            //------------------------------------------
            //Create all the cards

            create_all_cards(card_controller, GameData.player_count + GameData.center_card_count);
            playspace.reset_player_positions();

            //--------------------------------------------------------------------
            //--------------------------------------------------------------------
            //--------------------------------------------------------------------
            //Wait a while for game to start (players get ready)

            await ToSignal(GetTree().CreateTimer(2), "timeout");

            //------------------------------------------
            //Send players their role data
            await start_role_review();

            //--------------------------------------------------------------------
            //Start werewolves

            if (role_assignment.Contains(CardDatabase.roles.werewolf))
            {
                await start_werewolves();
                await reset_board_state();
            }

            //--------------------------------------------------------------------
            //Start seer

            if (role_assignment.Contains(CardDatabase.roles.seer))
            {
                await start_seer();
                await reset_board_state();
            }

            //--------------------------------------------------------------------
            //Start robber
            
            if (role_assignment.Contains(CardDatabase.roles.robber))
            {
                await start_robber();
                await reset_board_state();
            }

            //--------------------------------------------------------------------

            if (role_assignment.Contains(CardDatabase.roles.troublemaker))
            {
                await start_troublemaker();
                await reset_board_state();
            }

            if (role_assignment.Contains(CardDatabase.roles.drunk))
            {
                await start_drunk();
                await reset_board_state();
            }

            if (role_assignment.Contains(CardDatabase.roles.insomniac))
            {
                await start_insomniac();
                await reset_board_state();
            }

            await start_discussion();
            await start_voting();
            await ToSignal(GetTree().CreateTimer(2), "timeout");
            await reset_board_state();


            await calculate_results(cdb);
            //----------------------------------------------------------

            GD.PrintS("End of currently working roles");


            playspace.set_player_messaging(true);
            playspace.set_current_turn("Game Ending");
            playspace.set_role_timer(60);
            await ToSignal(GetTree().CreateTimer(60), "timeout");

            if (GetTree().NetworkPeer is NetworkedMultiplayerENet enet)
            {
                enet.CloseConnection();
            }
            GetTree().ChangeScene("res://src/game/Menus/MainMenu.tscn");
        }
    }


    //=================================================================================================================
    //=================================================================================================================
    //=================================================================================================================
    //All the role functions are going in here apparently :D

    private async Task reset_board_state()
    {
        playspace.set_blind_players(true);
        pending_data_cat1.Clear();
        pending_data_cat2.Clear();
        playspace.reset_board_state();
        await ToSignal(GetTree().CreateTimer(3), "timeout");
    }

    private async Task start_role_review()
    {
        GD.PrintS("Review your card turn");
        playspace.set_current_turn("Review your card");

        send_player_positions(player_list, GameData.center_card_count);
        send_original_roles(player_original_roles, player_list, GameData.center_card_count);

        //--------------------------------------------------------------------
        //Wait for players to read their cards and prepare

        playspace.set_role_timer(5);
        await ToSignal(GetTree().CreateTimer(5), "timeout");
        playspace.set_blind_players(true);
        playspace.hide_all_cards();
        await ToSignal(GetTree().CreateTimer(3), "timeout");
    }

    private async Task start_werewolves()
    {
        GD.PrintS("Werewolf turn");
        playspace.set_current_turn("Werewolves");
        current_role = CardDatabase.roles.werewolf;

        List<int> werewolf_ids = find_original_role(CardDatabase.roles.werewolf, player_original_roles);

        if (werewolf_ids.Count == 1)
        {
            pending_data_cat1.Add(werewolf_ids[0]);
            playspace.start_werewolf_solo(werewolf_ids[0]);

            playspace.set_role_timer(GameData.normal_role_time);
            await ToSignal(GetTree().CreateTimer(GameData.normal_role_time), "timeout");
        }
        else
        {
            playspace.start_werewolf_team(werewolf_ids.ToArray());

            playspace.set_role_timer(GameData.normal_role_time);
            await ToSignal(GetTree().CreateTimer(GameData.normal_role_time), "timeout");
        }
    }

    private async Task start_seer()
    {
        GD.PrintS("Seer turn");
        playspace.set_current_turn("Seer");
        current_role = CardDatabase.roles.seer;

        List<int> seers = find_original_role(CardDatabase.roles.seer, player_original_roles);

        foreach(int seer in seers)
        {
            playspace.start_seer(seer);
            pending_data_cat1.Add(seer);
            pending_data_cat1.Add(seer);
        }

        playspace.set_role_timer(GameData.difficult_role_time);
        await ToSignal(GetTree().CreateTimer(GameData.difficult_role_time), "timeout");

    }

    private async Task start_robber()
    {
        GD.PrintS("Robber turn");
        playspace.set_current_turn("Robber");
        current_role = CardDatabase.roles.robber;

        List<int> robbers = find_original_role(CardDatabase.roles.robber, player_original_roles);

        foreach(int robber in robbers)
        {
            playspace.start_robber(robber);
            pending_data_cat1.Add(robber);
            pending_data_cat2.Add(robber);
        }

        playspace.set_role_timer(GameData.normal_role_time);
        await ToSignal(GetTree().CreateTimer(GameData.normal_role_time), "timeout");

    }

    private async Task start_troublemaker()
    {
        GD.PrintS("Troublemakers turn");
        playspace.set_current_turn("Troublemaker");
        current_role = CardDatabase.roles.troublemaker;

        List<int> troublemakers = find_original_role(CardDatabase.roles.troublemaker, player_original_roles);

        foreach(int troublemaker in troublemakers)
        {
            playspace.start_troublemaker(troublemaker);
            pending_data_cat1.Add(troublemaker);
        }

        playspace.set_role_timer(GameData.normal_role_time);
        await ToSignal(GetTree().CreateTimer(GameData.normal_role_time), "timeout");
    }

    private async Task start_drunk()
    {
        GD.PrintS("Drunks turn");
        playspace.set_current_turn("Drunk");
        current_role = CardDatabase.roles.drunk;

        List<int> drunks = find_original_role(CardDatabase.roles.drunk, player_original_roles);

        foreach(int drunk in drunks)
        {
            playspace.start_drunk(drunk);
            pending_data_cat1.Add(drunk);
        }

        playspace.set_role_timer(GameData.normal_role_time);
        await ToSignal(GetTree().CreateTimer(GameData.normal_role_time), "timeout");

    }

    private async Task start_insomniac()
    {
        GD.PrintS("Insomniacs Turn");
        playspace.set_current_turn("Insomniac");
        current_role = CardDatabase.roles.insomniac;

        List<int> insomniacs = find_original_role(CardDatabase.roles.insomniac, player_original_roles);

        foreach(int insomniac in insomniacs)
        {
            pending_data_cat1.Add(insomniac);
            playspace.start_insomniac(insomniac);
        }

        playspace.set_role_timer(GameData.normal_role_time);
        await ToSignal(GetTree().CreateTimer(GameData.normal_role_time), "timeout");
    }

    private async Task start_discussion()
    {
        GD.PrintS("Discussion time");
        playspace.set_current_turn("Discussion");
        playspace.set_blind_players(false);
        current_role = CardDatabase.roles.none;

        player_controller.set_all_player_mouse(true);
        playspace.set_player_messaging(true);
        
        playspace.set_role_timer(GameData.discussion_time);
        await ToSignal(GetTree().CreateTimer(GameData.discussion_time), "timeout");
        player_controller.set_all_player_mouse(false);
        playspace.set_player_messaging(false);
    }

    private async Task start_voting()
    {
        GD.PrintS("Voting time");
        playspace.set_current_turn("Voting");
        playspace.set_blind_players(false);
        player_id_to_votes.Clear();
        current_role = CardDatabase.roles.none;

        voting_turn = true;
        foreach(Server.player_data player in player_list)
        {
            GD.PrintS("Adding player", player.player_id, "to voting list");
            pending_data_cat1.Add(player.player_id);
        }

        playspace.start_player_voting();

        playspace.set_role_timer(15);
        await ToSignal(GetTree().CreateTimer(15), "timeout");
    }

    private async Task calculate_results(CardDatabase cdb)
    {
        List<int> highest_votes = new List<int>();
        int highest_vote = 0;

        foreach(KeyValuePair<int, int> votes in player_id_to_votes)
        {
            if (votes.Value > highest_vote)
            {
                highest_votes.Clear();
                highest_votes.Add(votes.Key);
                highest_vote = votes.Value;
            }
            else if (votes.Value == highest_vote)
            {
                highest_votes.Add(votes.Key);
            }
        }

        await ToSignal(GetTree().CreateTimer(3), "timeout");

        List<string> voted_players_names = new List<string>();

        //Reveal the voted players
        foreach(int player in highest_votes)
        {
            voted_players_names.Add(player_controller.player_data_dict[player].player_name);

            GD.PrintS("Player", player, "was voted for");
            int player_pos = player_controller.player_data_dict[player].player_position + GameData.center_card_count;
            playspace.set_spotlight_player(player, true);
            playspace.reveal_card_at_position(player_pos, role_assignment[player_pos]);
        }

        if (voted_players_names.Count <= 0)
        {
            playspace.set_game_help("Nobody was voted for.");
        }
        else if (voted_players_names.Count == player_list.Count)
        {
            playspace.set_game_help("Everyone was voted for.");
        }
        else if (voted_players_names.Count == 1)
        {
            playspace.set_game_help($"{voted_players_names[0]} was voted for.");
        }
        else
        {
            string name_string = voted_players_names[0];
            for (int x = 1; x < voted_players_names.Count - 1; x++)
            {
                name_string += $", {voted_players_names[x]}";
            }

            string exclaim = ".";
            if (voted_players_names.Count >= 3)
                exclaim = "!";

            playspace.set_game_help($"{name_string} and {voted_players_names[voted_players_names.Count - 1]} were voted for{exclaim}");
        }



        //Hunter voted for a player
        List<int> to_add = new List<int>();
        foreach(int player in highest_votes)
        {
            int player_pos = player_controller.player_data_dict[player].player_position + GameData.center_card_count;
            if (role_assignment[player_pos] == CardDatabase.roles.hunter)
            {
                if (player_id_to_other_id.ContainsKey(player))
                {
                    if (!highest_votes.Contains(player_id_to_other_id[player]))
                    {
                        await ToSignal(GetTree().CreateTimer(2.5F), "timeout");
                        int hunter_vote_pos = player_controller.player_data_dict[player_id_to_other_id[player]].player_position + GameData.center_card_count;

                        to_add.Add(player_id_to_other_id[player]);
                        
                        playspace.set_spotlight_player(player_id_to_other_id[player], true);
                        playspace.reveal_card_at_position(hunter_vote_pos, role_assignment[hunter_vote_pos]);

                        string player1 = player_controller.player_data_dict[player].player_name;
                        string player2 = player_controller.player_data_dict[player_id_to_other_id[player]].player_name;

                        playspace.add_game_help($"{player1} has hunted {player2}!");
                    }
                }
            }
        }
        highest_votes.AddRange(to_add);
        to_add = null;

        //----------------------------------------------------------

        //Turn off the spotlight for all voted players (prevents glare - kinda)
        await ToSignal(GetTree().CreateTimer(3), "timeout");
        foreach(int player in highest_votes)
        {
            playspace.set_spotlight_player(player, false);
        }

        reveal_all_roles();

        await ToSignal(GetTree().CreateTimer(4), "timeout");

        bool everyone_voted_for = false;

        bool werewolf_voted = false;
        bool tanner_voted = false;
        bool villager_voted = false;


        if (highest_votes.Count == player_list.Count)
        {
            everyone_voted_for = true;
        }

        foreach(int player in highest_votes)
        {
            int player_pos = player_controller.player_data_dict[player].player_position + GameData.center_card_count;
            CardDatabase.roles voted_role = role_assignment[player_pos];

            //role was a werewolf role
            if (cdb.role_data[voted_role].card_team == CardDatabase.teams.werewolves)
            {
                werewolf_voted = true;
            }
            //Role was a tanner
            else if (voted_role == CardDatabase.roles.tanner)
            {
                tanner_voted = true;
            }
            //Role was a villager role
            if (cdb.role_data[voted_role].card_team == CardDatabase.teams.villagers)
            {
                villager_voted = true;
            }
        }


        if (everyone_voted_for && tanner_voted)
        {
            GD.PrintS("The tanner wins");
            playspace.add_game_help("The tanner wins!");
        }
        else if (everyone_voted_for)
        {
            GD.PrintS("Nobody wins");
            playspace.add_game_help("Nobody wins.");
        }
        else if (tanner_voted && werewolf_voted)
        {
            GD.PrintS("The tanner wins");
            playspace.add_game_help("The tanner wins!");
        }
        else if (werewolf_voted)
        {
            GD.PrintS("The villagers win");
            playspace.add_game_help("The villagers win!");
        }
        else if (tanner_voted && villager_voted)
        {
            GD.PrintS("The tanner and werewolves win");
            playspace.add_game_help("The tanner and werewolves win!");
        }
        else if (tanner_voted)
        {
            GD.PrintS("The tanner and werewolves win");
            playspace.add_game_help("The tanner and werewolves win!");
        }
        else if (villager_voted)
        {
            GD.PrintS("The werewolves win");
            playspace.add_game_help("The werewolves win!");
        }
    }

    
    //=================================================================================================================
    //=================================================================================================================
    //=================================================================================================================

    private List<CardDatabase.roles> shuffle_role_list(RandomNumberGenerator rand, List<CardDatabase.roles> unshuffled)
    {
        List<CardDatabase.roles> unshuffled_list = new List<CardDatabase.roles>(unshuffled);
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
                    GD.PrintS("There was already a werewolf");
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
            GD.PrintS("There was no werewolf originally");
            int rand_player_pos = rand.RandiRange(center_cards, role_list.Count - 1);
            CardDatabase.roles temp = role_list[werewolf_position];

            role_list[werewolf_position] = role_list[rand_player_pos];
            role_list[rand_player_pos] = temp;

            return role_list;
        }
        else
        {
            GD.PrintS("There are no werewolves in this game");
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
        for (int x = center_count; x < players.Count + center_count; x++)
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
        playspace.set_blind_players(false);
        for (int x = 0; x < role_assignment.Count; x++)
        {
            playspace.reveal_card_at_position(x, role_assignment[x]);
        }
    }

    //===========================================================================
    //===========================================================================
    //Card finding functions

    private List<int> find_original_role(CardDatabase.roles to_find, Dictionary<int, CardDatabase.roles> orignal_roles)
    {
        List<int> found_ids = new List<int>();
        
        foreach(KeyValuePair<int, CardDatabase.roles> data in orignal_roles)
        {
            if (data.Value == to_find)
            {
                found_ids.Add(data.Key);
            }
        }

        return found_ids;
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
        int sender = GetTree().GetRpcSenderId();

        if (card_position < 0 || card_position >= role_assignment.Count)
        {
            GD.PrintS("Data request from", sender, "is out of bounds");
            return;
        }

        if (pending_data_cat1.Contains(sender) || pending_data_cat2.Contains(sender))
        {
            GD.PrintS("Approved reveal request from", sender);




            if (current_role == CardDatabase.roles.werewolf)
            {
                playspace.reveal_card_at_position(sender, card_position, role_assignment[card_position]);
                pending_data_cat1.Clear();
            }
            else if (current_role == CardDatabase.roles.seer)
            {
                playspace.reveal_card_at_position(sender, card_position, role_assignment[card_position]);
                if (card_position >= GameData.center_card_count)
                {
                    pending_data_cat1.Remove(sender);
                }
                pending_data_cat1.Remove(sender);
            }
            else if (current_role == CardDatabase.roles.robber)
            {
                if (pending_data_cat2.Contains(sender))
                {
                    playspace.reveal_card_at_position(sender, card_position, role_assignment[card_position]);
                    pending_data_cat2.Remove(sender);
                }
            }
            else if (current_role == CardDatabase.roles.insomniac)
            {
                playspace.reveal_card_at_position(sender ,card_position, role_assignment[card_position]);
                pending_data_cat1.Clear();
            }




        }
        else
        {
            GD.PrintS("Denied reveal request from", sender);
        }
    }

    public void request_card_swap(int pos1, int pos2)
    {
        RpcId(1, "_request_card_swap", pos1, pos2);
    }

    [RemoteSync]
    public void _request_card_swap(int pos1, int pos2)
    {
        int sender = GetTree().GetRpcSenderId();

        if (pos1 >= role_assignment.Count || pos2 >= role_assignment.Count)
        {
            GD.PrintS("Swap request from", sender, "is out of bounds");
            return;
        }

        if (pending_data_cat1.Contains(sender) || pending_data_cat2.Contains(sender))
        {
            GD.PrintS("Approved swap request from", sender);

            if (current_role == CardDatabase.roles.robber)
            {
                if (pending_data_cat1.Contains(sender))
                {
                    CardDatabase.roles temp = role_assignment[pos1];
                    role_assignment[pos1] = role_assignment[pos2];
                    role_assignment[pos2] = temp;

                    pending_data_cat1.Remove(sender);
                }
            }
            else if (current_role == CardDatabase.roles.troublemaker)
            {
                CardDatabase.roles temp = role_assignment[pos1];
                role_assignment[pos1] = role_assignment[pos2];
                role_assignment[pos2] = temp;

                pending_data_cat1.Remove(sender);
            }
            else if (current_role == CardDatabase.roles.drunk)
            {
                CardDatabase.roles temp = role_assignment[pos1];
                role_assignment[pos1] = role_assignment[pos2];
                role_assignment[pos2] = temp;

                pending_data_cat1.Remove(sender);
            }



        }
        else
        {
            GD.PrintS("Swap request denied from", sender);
        }
    }


    public void request_player_vote(int player)
    {
        RpcId(1, "_request_player_vote", player);
    }

    [RemoteSync]
    public void _request_player_vote(int player)
    {
        int sender = GetTree().GetRpcSenderId();

        //if (pos1 >= role_assignment.Count || pos2 >= role_assignment.Count)
        //{
        //    GD.PrintS("Swap request from", sender, "is out of bounds");
        //    return;
        //}

        if (pending_data_cat1.Contains(sender) && voting_turn)
        {
            GD.PrintS("Approved vote request from", sender);

            if (player_id_to_votes.ContainsKey(player))
            {
                player_id_to_votes[player]++;
            }
            else
            {
                player_id_to_votes.Add(player, 1);
            }

            player_id_to_other_id.Add(sender, player);

            pending_data_cat1.Remove(sender);

        }
        else
        {
            GD.PrintS("Denied player votes from", sender);
        }
    }

    //===========================================================================
    //===========================================================================

}
