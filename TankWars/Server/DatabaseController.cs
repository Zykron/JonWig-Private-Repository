using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TankWars;

namespace Server
{
    /// <summary>
    /// This method contains all of the methods for interacting with the database
    /// </summary>
    public class DatabaseController
    {
        //Parameters for connecting to the database
        public const string connectionString = "server=atr.eng.utah.edu;" +
            "database=cs3500_u1190338;" +
            "uid=cs3500_u1190338;" +
            "password=AlanTuring";

        /// <summary>
        /// Method for saving the current game to the database. This goes through each world object and adds the info to the right
        /// tables in the database
        /// </summary>
        /// <param name="TheWorld">The world object that's being saved</param>
        public static void SaveGameToDatabase(World TheWorld)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    MySqlCommand command = conn.CreateCommand();

                    //This command inserts the the duration of the game since the game ID is auto incrementing
                    command.CommandText = "insert into Games(Duration) values (\"" + TheWorld.Duration.Elapsed.Seconds + "\");";
                    command.ExecuteNonQuery();

                    //This records the game ID so it can be used in later inserts
                    UInt64 gID = 0;
                    command.CommandText = "select LAST_INSERT_ID();";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();
                        gID = (UInt64)reader["LAST_INSERT_ID()"];
                    }

                    //For each tank in the 
                    foreach (Tank t in TheWorld.Players.Values)
                    {
                        //Inserts the name of the tank into the players table since pID is auto incrementing
                        command.CommandText = "insert into Players(Name) values (\"" + t.Name + "\");";
                        command.ExecuteNonQuery();

                        //Calculates the accuracy of each tank
                        int Accuracy;
                        if (t.ShotsFired != 0)
                            Accuracy = (int)(100 * (((float)t.ShotsHit) / ((float)t.ShotsFired)));
                        else
                            Accuracy = 100;

                        //Inserts the gID, Score, and Accuracy into the games played table since pID is auto incrementing
                        command.CommandText = "insert into GamesPlayed(gID,Score,Accuracy) values (\"" + gID + "\", \"" + t.Score + "\", \"" + Accuracy  + "\");";
                        command.ExecuteNonQuery();
                    }

                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        
        /// <summary>
        /// This method is for reading a dictionary of all of the games from the database
        /// </summary>
        /// <returns>The dictionary of all of the games</returns>
        public static Dictionary<uint, GameModel> GetAllGames()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    Dictionary<uint, GameModel> Games = new Dictionary<uint, GameModel>();

                    //Selects all of games to create the entries to the dictionary with the gID as the key
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "Select * from Games;";

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GameModel game = new GameModel((uint)reader["gID"], (uint)reader["Duration"]);
                            Games.Add((uint)reader["gID"], game);
                        }
                    }

                    //Joins two tables so it can record the names, scores, and accuracy of those players in the dictionary entries created earlier
                    command.CommandText = "Select * from GamesPlayed natural join Players";

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Games[(uint)reader["gID"]].AddPlayer((string)reader["Name"], (uint)reader["Score"], (uint)reader["Accuracy"]);
                        }
                    }
                    return Games;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Dictionary<uint, GameModel>();
                }
            }
        }


        /// <summary>
        /// This method is for reading a dictionary of all of the player games from the database
        /// </summary>
        /// <returns>The dictionary of all of the games</returns>
        public static Dictionary<uint, PlayerModel> GetAllPlayerGames(string PlayerName)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    Dictionary<uint, PlayerModel> Players = new Dictionary<uint, PlayerModel>();

                    //Joins two tables so it can receive the names, scores, and accuracy to add them to the dictionary
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "Select * from GamesPlayed natural join Players where Name = \'" + PlayerName + "\';";

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PlayerModel player = new PlayerModel((string)reader["Name"], (uint)reader["Score"], (uint)reader["Accuracy"]);
                            if(!Players.ContainsKey((uint)reader["gID"]))
                                Players.Add((uint)reader["gID"], player);
                        }
                    }
                    return Players;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return new Dictionary<uint, PlayerModel>();
                }
            }
        }


        /// <summary>
        /// This method reads the duration of a game by using the database
        /// </summary>
        /// <param name="gID">ID of the game</param>
        /// <returns>duration of the game</returns>
        public static uint GetGameDuration(uint gID)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    //reads the duration from the database and filters it so it gets one with the right gID
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText = "Select Duration from Games where gID = \'" + gID + "\';";

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();
                        return (uint)reader["Duration"];
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return 0;
                }
            }
        }
    }
}
