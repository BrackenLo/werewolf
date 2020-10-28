using System;
using System.Collections.Generic;

public class CardDatabase
{
    public enum roles 
    {
        none, werewolf, troublemaker, seer, robber, insomniac, 
        villager, tanner, hunter, drunk
    }

    public enum teams
    {
        werewolves, villagers, neutral,
    }


    public Dictionary<roles, card_data> role_data = new Dictionary<roles, card_data>()
    {
      {roles.werewolf, new card_data("Werewolf", teams.werewolves, -1, "res://assets/roles/Werewolf.png")},
      {roles.villager, new card_data("Villager", teams.villagers, -1, "res://assets/roles/Villager.png")},
      {roles.troublemaker, new card_data("Troublemaker", teams.villagers, -1, "res://assets/roles/Troublemaker.png")},
      {roles.seer, new card_data("Seer", teams.villagers, -1, "res://assets/roles/Seer.png")},
      {roles.robber, new card_data("Robber", teams.villagers, -1, "res://assets/roles/robber.png")},
      {roles.insomniac, new card_data("Insomniac", teams.villagers, -1, "res://assets/roles/Insomniac.png")},
      {roles.tanner, new card_data("Tanner", teams.neutral, -1, "res://assets/roles/Jester.png")},
      {roles.hunter, new card_data("Hunter", teams.villagers, -1, "res://assets/roles/Hunter.png")},
      {roles.drunk, new card_data("Drunk", teams.villagers, -1, "res://assets/roles/Drunk.png")},
    };

    public Dictionary<teams, string> team_data = new Dictionary<teams, string>()
    {
        {teams.werewolves, "[b][color=red]Werewolf[/color][/b]"},
        {teams.villagers, "[i][color=green]Villager[/color][/i]"},
        {teams.neutral, "[color=gray]Neutral[/color]"}
    };



    public class card_data
    {
        public string card_name {get; private set;}
        public teams card_team {get; private set;}
        public int role_max_amount {get; private set;}
        public string card_art {get; private set;}

        public card_data(string name, teams team, int max_amount, string art_path)
        {
            card_name = name;
            card_team = team;
            role_max_amount = max_amount;
            card_art = art_path;
        } 
    }

}
