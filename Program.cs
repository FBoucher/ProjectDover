using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ProjectDover
{
    class Program
    {
        static private void Main(string[] args)
        {
            var _config = GetConfig();

            GameType gameType = Introduction();
            string playerName = PlayerCreation();

            var parser = new CommandParser();
            var GameSession = new GameSession(gameType, playerName);

            if(gameType == GameType.LOADED_GAME){
                Console.WriteLine(GameSession.Summary() + Environment.NewLine);
                Console.WriteLine(GameSession.RoomManager.CurrentRoomDescription);
            }

            while (true)
            {
                // spit room desc
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(GameSession.RoomManager.CurrentRoomName);
                Console.ResetColor();
                if (!GameSession.RoomManager.CurrentRoomHasSeenDescription)
                {
                    Console.WriteLine(GameSession.RoomManager.CurrentRoomDescription);
                }

                Console.WriteLine(GameSession.RoomManager.CurrentRoomExits());
                Console.Write("> ");
                var inputString = Console.ReadLine();

                var command = parser.ProcessCommandText(inputString);
                switch (command)
                {
                    case Command.COMMAND_QUIT:
                        {
                            Console.Clear();
                            Console.WriteLine("Thanks for playing!");
                            Environment.Exit(0);
                        }
                        break;
                    case Command.COMMAND_NORTH:
                    case Command.COMMAND_SOUTH:
                    case Command.COMMAND_EAST:
                    case Command.COMMAND_WEST:
                    case Command.COMMAND_UP:
                    case Command.COMMAND_DOWN:
                        {
                        GameSession.RoomManager.Go(command);
                    }
                        break;
                    case Command.COMMAND_LOOK:
                        {
                            GameSession.RoomManager.Do(command);
                        }
                        break;
                    case Command.COMMAND_INVENTORY:
                        {
                            GameSession.Inventory.ListItems();
                        }
                        break;
                    case Command.COMMAND_TAKE:
                        {
                            Inventory roomInventory = GameSession.RoomManager.CurrentRoomInventory();
                            string itemName = inputString.Split(' ')[1];
                
                            if(roomInventory.Contains(itemName)){
                                Item currentItem = roomInventory.RemoveItem(itemName);

                                if(currentItem.Triggers.ContainsKey("take")){
                                    string keyEvent = GameSession.RoomManager.ProcessTrigger(currentItem,"take");
                                    if(!String.IsNullOrEmpty(keyEvent)){
                                        GameSession.KeyEvents.Add(keyEvent);
                                    }
                                }

                                GameSession.Inventory.AddItem(currentItem);
                            }
                        }
                        break;
                    case Command.COMMAND_SUMMARY:
                        {
                            Console.WriteLine(GameSession.Summary());
                        }
                        break;
                    case Command.COMMAND_SAVE:
                    {
                        var connStr = _config["Blind2021DatabaseSettings:ConnectionString"];
                        Console.WriteLine(GameSession.SaveGame(connStr));
                    }
                    break;
                    case Command.COMMAND_HANDLED: break;
                    default:
                        {
                            Console.WriteLine();
                            Console.WriteLine("I'm not sure what to do");
                        }
                        break;
                }
            }
        }

        static private GameType Introduction(){

            Console.Clear();
            Console.SetWindowSize(119, 50);
            Console.WriteLine("-=- Welcome to Blind2021 -=-");

            Console.Write(File.ReadAllText(@".\medias\blind2021-ascii.txt")); 
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("This is a text based adventure game...");

            Console.WriteLine("Do you want to:");
            Console.WriteLine("1 - Start a New Game.");

            var client = new MongoClient("mongodb://localhost:27017");
            if(client != null)
                Console.WriteLine("2 - Load a Saved Game.");
            
            Console.Write("> ");
            var inputString = Console.ReadLine();

            var selectedGameType = GameTypeParser.ProcessGameTypeText(inputString);
            Console.WriteLine(Environment.NewLine);

            return selectedGameType;
        }

        public static IConfigurationRoot GetConfig(){
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        public static string PlayerCreation(){

            bool validName = false;
            string inputString = string.Empty;

            while(!validName)
            {
                Console.WriteLine("How should I call you?");
                Console.WriteLine("More than 4 charcters, please.");
                Console.Write("> ");
                inputString = Console.ReadLine();

                if(inputString.Length >= 5){
                    validName = true;
                }
            }

            Console.WriteLine(String.Format("Happy to have you with us {0}, now let's get started.", inputString));
            Console.WriteLine(Environment.NewLine);

            return inputString;
        }

    }
}
