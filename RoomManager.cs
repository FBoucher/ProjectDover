using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MongoDB.Driver;

namespace ProjectDover
{
    public class RoomManager
    {
        public List<Room> Rooms { get; set; }
        public string CurrentRoomId { get; set; }
        private Room CurrentRoom { get { return Rooms.First(p => p.Id == CurrentRoomId); } }

        private int visitedRoomCounter { get; set; }

        public RoomManager(GameType gametype)
        {
            CurrentRoomId = "0";

            Rooms = new List<Room>();
            // Rooms.Add(new Room()
            // {
            //     Id = "0",
            //     Name = "Outside Brady's House",
            //     Description = "Brady has an extremely loved home.  There are even trees and flowers.  On your left, there is a truck up on some blocks.",
            //     Exits = new List<Exit>() { new Exit() { Direction = Direction.North, TargetRoomId = "1" } }
            // });
            // Rooms.Add(new Room()
            // {
            //     Id = "1",
            //     Name = "Inside Brady's House",
            //     Description = "The inside is even nicer than the outside.  $4000 of electronic equipment sit on a table.",
            //     Exits = new List<Exit>() {  new Exit() { Direction = Direction.South, TargetRoomId = "999" },
            //                                 new Exit() { Direction = Direction.East, TargetRoomId = "2" },
            //                                 new Exit() { Direction = Direction.West, TargetRoomId = "3" } }
            // });
            // Rooms.Add(new Room()
            // {
            //     Id = "2",
            //     Name = "Living Room",
            //     Description = "Nice cozy living room. On the North wall there is a mirror. And a flashlight on the table in the middle of the room.",
            //     Exits = new List<Exit>() { new Exit() { Direction = Direction.West, TargetRoomId = "1" } },
            //     Inventory = new Inventory("Living Room")
            //     {
            //         Items = new List<Item>(){
            //                 new Item() { Name = "Mirror", Description = "Regular mirror, where you can see yourself."},
            //                 new Item() { Name = "flashlight",
            //                              Description = "Regular basic flashlight.",
            //                              Triggers = new Dictionary<string,string>() {{"take","noFlashlight"}},
            //                              KeyEvents = new Dictionary<string,string>() {{"take","You found the Flashlight."}}
            //                             }
            //         }
            //     },
            //     PotentialDescription = new Dictionary<string, string>() {
            //         {"noFlashlight", "Nice cozy living room. On the North wall there is a mirror. And a the table in the middle of the room."}
            //     }
            //     //TODO: trigger character creation event!
            // });
            // Rooms.Add(new Room()
            // {
            //     Id = "3",
            //     Name = "Master Bedroom",
            //     Description = "Nice luxurious bedroom. The perfect place for Brady to rest and retreive his voice.",
            //     Exits = new List<Exit>() { new Exit() { Direction = Direction.East, TargetRoomId = "1" } }
            // });

            if(gametype == GameType.LOADED_GAME){
                LoadMap();
            }
            else{
                //var jsonRooms = File.ReadAllText(@".\data\BradysHouse\rooms.json");
                var jsonRooms = File.ReadAllText(@".\data\CollinsHouse\rooms.json");
                
                Rooms = JsonSerializer.Deserialize<List<Room>>(jsonRooms);
            }
        }

        public string CurrentRoomName => CurrentRoom.Name;
        public bool CurrentRoomHasSeenDescription => CurrentRoom.HasSeenDescription;

        public string CurrentRoomDescription
        {
            get
            {
                if(! CurrentRoom.HasSeenDescription)
                {
                    CurrentRoom.HasSeenDescription = true;
                    visitedRoomCounter++;
                }
                return CurrentRoom.Description;
            }
        }

        public string CurrentRoomExits()
        {
            var exitList = new List<string>();
            foreach (var currentRoomExit in CurrentRoom.Exits)
            {
                exitList.Add(Exit.GetDirection(currentRoomExit.Direction));
            }

            return $"Exits: {string.Join(", ", exitList)}";
        }

        public void Go(Command command)
        {
            // north
            var indexOfExit = -1;
            switch (command)
            {
                case Command.COMMAND_NORTH:
                    indexOfExit = GetExitFromDirection(Direction.North);
                    break;
                case Command.COMMAND_SOUTH:
                    indexOfExit = GetExitFromDirection(Direction.South);
                    break;
                case Command.COMMAND_EAST:
                    indexOfExit = GetExitFromDirection(Direction.East);
                    break;
                case Command.COMMAND_WEST:
                    indexOfExit = GetExitFromDirection(Direction.West);
                    break;
                case Command.COMMAND_UP:
                    indexOfExit = GetExitFromDirection(Direction.Up);
                    break;
                case Command.COMMAND_DOWN:
                    indexOfExit = GetExitFromDirection(Direction.Down);
                    break;
            }

            if (indexOfExit == -1)
            {
                Console.WriteLine("You cannot go that way.");
                return;
            }

            CurrentRoomId = CurrentRoom.Exits[indexOfExit].TargetRoomId;
        }

        public void Do(Command command)
        {

            switch (command)
            {
                case Command.COMMAND_LOOK:
                    Console.WriteLine(CurrentRoomDescription);
                    break;
            }

        }

        private int GetExitFromDirection(Direction direction)
        {
            return CurrentRoom.Exits.FindIndex(p => p.Direction == direction);
        }

        public Inventory CurrentRoomInventory()
        {
            return CurrentRoom.Inventory;
        }

        public string ProcessTrigger(Item currentItem, string triggerName)
        {
            CurrentRoom.Description = CurrentRoom.PotentialDescription[currentItem.Triggers[triggerName]];

            if (currentItem.KeyEvents.ContainsKey(triggerName))
            {
                return currentItem.KeyEvents[triggerName];
            }
            return string.Empty;
        }

        public string MapCoverage(){
            if(Rooms.Count > 0 ){
                var result = ((double)visitedRoomCounter/Rooms.Count);
                var mapCoverage =  result * 100;
                return string. Format("You have seen {0}% of the map.", mapCoverage.ToString());
            }

            return "It looks like you just start. No map coverage available yet.";
        }


        public void SaveMap(){

            IMongoCollection<Room> _rooms;
            //var connStr = _config["Blind2021DatabaseSettings:ConnectionString"];
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("Blind2021Db");

            _rooms = database.GetCollection<Room>("Rooms");

            foreach(var room in Rooms){
                _rooms.InsertOne(room);
            }
        }

        public void LoadMap(){

            IMongoCollection<Room> _rooms;
            
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("Blind2021Db");

            _rooms = database.GetCollection<Room>("Rooms");

            Rooms =  _rooms.Find(room => true).ToList();
        }

    }
}