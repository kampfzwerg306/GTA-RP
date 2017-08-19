﻿using System.Collections.Generic;
using System.Linq;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using GrandTheftMultiplayer.Shared;
using GrandTheftMultiplayer.Shared.Math;
using GTA_RP.Factions;
using GTA_RP.Misc;
using System;
using System.Text;

namespace GTA_RP.Vehicles
{
    public delegate void OnVehicleDestroyedDelegate(NetHandle vehicleHandle);
    public delegate void OnVehicleExitedDelegate(Client c, NetHandle vehicleHandle);
    public delegate void OnVehicleEnteredDelegate(Client c, NetHandle vehicleHandle);

    /// <summary>
    /// Class responsible for handling vehicles
    /// </summary>
    class VehicleManager : Singleton<VehicleManager>
    {
        List<RPVehicle> vehicles = new List<RPVehicle>();
        Dictionary<int, VehicleShop> vehicleShops = new Dictionary<int, VehicleShop>();
        private event OnVehicleDestroyedDelegate OnVehicleDestroyedEvent;
        private event OnVehicleExitedDelegate OnVehicleExitedEvent;
        private event OnVehicleEnteredDelegate OnVehicleEnterEvent;

        private const int buyParkPrice = 10000;
        private const float doorLockDistance = 2.0f;
        private const float parkDistance = 3.0f;
        private int vehicleInsertId = 0;

        public VehicleManager()
        {
            this.InitializeVehicleInsertId();
            this.InitializeVehicleShops();
        }

        /// <summary>
        /// Announce destruction of vehicle to subscribers
        /// </summary>
        /// <param name="vehicle">Vehicle handle</param>
        public void VehicleDestroyedEvent(NetHandle vehicle)
        {
            RunVehicleDestroyedEvents(vehicle);
        }

        /// <summary>
        /// Announce player exiting vehicle to subscribers
        /// </summary>
        /// <param name="c">Client who exited vehicle</param>
        /// <param name="vehicle">Vehicle from which client exited</param>
        public void VehicleExitEvent(Client c, NetHandle vehicle)
        {
            RunVehicleExitEvents(c, vehicle);
        }

        /// <summary>
        /// Announce player entering vehicle to subscribers
        /// </summary>
        /// <param name="c">Client who entered vehicle</param>
        /// <param name="vehicle">Vehicle that client entered into</param>
        public void VehicleEnterEvent(Client c, NetHandle vehicle)
        {
            RunVehicleEnterEvents(c, vehicle);
        }


        /// <summary>
        /// Subscribes a delegate to vehicle destroyed event
        /// </summary>
        /// <param name="d">Delegate</param>
        public void SubscribeToVehicleDestroyedEvent(OnVehicleDestroyedDelegate d)
        {
            this.OnVehicleDestroyedEvent += d;
        }

        /// <summary>
        /// Subscribes a delegate to enter vehicle event
        /// </summary>
        /// <param name="d">Delegate</param>
        public void SubscribeToVehicleEnterEvent(OnVehicleEnteredDelegate d)
        {
            this.OnVehicleEnterEvent += d;
        }

        /// <summary>
        /// Subscribes a delegate to exit vehicle event
        /// </summary>
        /// <param name="d">Delegate</param>
        public void SubscribeToVehicleExitEvent(OnVehicleExitedDelegate d)
        {
            this.OnVehicleExitedEvent += d;
        }

        /// <summary>
        /// Unsubscribes from vehicle destroyed event
        /// </summary>
        /// <param name="d">Delegate</param>
        public void UnsubscribeFromVehicleDestroyedEvent(OnVehicleDestroyedDelegate d)
        {
            this.OnVehicleDestroyedEvent -= d;
        }

        /// <summary>
        /// Unsubscribes from enter vehicle event
        /// </summary>
        /// <param name="d">Delegate</param>
        public void UnsubscribeFromVehicleEnterEvent(OnVehicleEnteredDelegate d)
        {
            this.OnVehicleEnterEvent -= d;
        }

        /// <summary>
        /// Unsubscribes from exit vehicle event
        /// </summary>
        /// <param name="d">Delegate</param>
        public void UnsubscribeFromVehicleExitEvent(OnVehicleExitedDelegate d)
        {
            this.OnVehicleExitedEvent -= d;
        }

        /// <summary>
        /// Runs all subscriber methods for vehicle destroyed event
        /// </summary>
        /// <param name="vehicle">Vehicle handle</param>
        private void RunVehicleDestroyedEvents(NetHandle vehicle)
        {
            if (this.OnVehicleDestroyedEvent != null)
                this.OnVehicleDestroyedEvent.Invoke(vehicle);
        }

        /// <summary>
        /// Runs all subscriber methods for enter vehicle event
        /// </summary>
        /// <param name="vehicle">Vehicle handle</param>
        private void RunVehicleEnterEvents(Client c, NetHandle vehicle)
        {
            if (this.OnVehicleEnterEvent != null)
                this.OnVehicleEnterEvent.Invoke(c, vehicle);
        }

        /// <summary>
        /// Initializes vehicle shops
        /// </summary>
        private void InitializeVehicleShops()
        {
            CreateVehicleShop(0, new Vector3(-177.2077, -1158.487, 23.8137), new Vector3(-177.0255, -1153.632, 23.11556), new Vector3(0, 0, -2.621614), new Vector3(205.331, -1004.533, -98.0), new Vector3(-18, 0, 50.22344), new Vector3(207.1324, -1007.67, -98.99998), new Vector3(201.9024, -1001.854, -99.00001), new Vector3(0, 0, 176.9532));
        }

        /// <summary>
        /// Generates a random string with given length
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Random string</returns>
        private String GenerateRandomString(int length)
        {
            string values = "QWERTYUIOPASDFGHJKLZXCVBNM1234567890";
            Random rnd = new Random();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < length; i++)
                builder.Append(values[rnd.Next(0, values.Length)]);
            return builder.ToString();
        }

        /// <summary>
        /// Informs player whether vehicles was locked or unlocked
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="locked">Locked</param>
        private void SendVehicleLockedMessage(Client client, Boolean locked)
        {
            if (locked) API.shared.sendNotificationToPlayer(client, "Vehicle locked");
            else API.shared.sendNotificationToPlayer(client, "Vehicle unlocked");
        }

        /// <summary>
        /// Checks if vehicle exists with the given license plate
        /// </summary>
        /// <param name="licensePlate">Text of the license plate</param>
        /// <returns>True if vehicle exists, otherwise false</returns>
        private Boolean DoesVehicleExistWithLicensePlate(String licensePlate)
        {
            foreach(RPVehicle v in vehicles)
            {
                if (v.licensePlateText.Equals(licensePlate))
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Checks whether any vehicles exist
        /// </summary>
        /// <returns>True if vehicles exist, otherwise false</returns>
        private Boolean HasVehicles()
        {
            return !DBManager.IsTableEmpty("vehicles");
        }

        /// <summary>
        /// Initializes the vehicle insert ID
        /// </summary>
        private void InitializeVehicleInsertId()
        {
            if (HasVehicles())
            {
                var cmd = DBManager.SimpleQuery("SELECT MAX(id) FROM vehicles");
                var reader = cmd.ExecuteReader();
                reader.Read();
                this.vehicleInsertId = reader.GetInt32(0) + 1;
                reader.Close();
            }
            else
                this.vehicleInsertId = 0;
        }

        /// <summary>
        /// Runs all subscriber methods for exit vehicle event
        /// </summary>
        /// <param name="vehicle">Vehicle handle</param>
        private void RunVehicleExitEvents(Client c, NetHandle vehicle)
        {
            if (this.OnVehicleExitedEvent != null)
                this.OnVehicleExitedEvent.Invoke(c, vehicle);
        }

        /// <summary>
        /// Creates a new vehicle shop
        /// </summary>
        /// <param name="id">Shop id</param>
        /// <param name="entrance">Entrance position</param>
        /// <param name="exit">Exit position</param>
        /// <param name="exitRot">Exit rotation</param>
        private void CreateVehicleShop(int id, Vector3 entrance, Vector3 exit, Vector3 exitRot, Vector3 cameraPos, Vector3 cameraRot, Vector3 charPos, Vector3 vehiclePos, Vector3 vehicleRot)
        {
            VehicleShop shop = new VehicleShop(id, entrance, exit, exitRot, cameraPos, cameraRot, charPos, vehiclePos, vehicleRot);
            if (!vehicleShops.ContainsKey(id)) vehicleShops[id] = shop;
        }

        /// <summary>
        /// Gets vehicle shop with certain id
        /// </summary>
        /// <param name="id">Vehicle shop</param>
        private VehicleShop GetVehicleShopWithId(int id)
        {
            if (vehicleShops.ContainsKey(id)) return vehicleShops[id];
            return null;
        }

        /// <summary>
        /// Checks if character is in given vehicle
        /// </summary>
        /// <param name="character">Character</param>
        /// <param name="veh">Vehicle</param>
        /// <returns>True if character is in vehicle, otherwise false</returns>
        private bool IsCharacterInVehicle(Character character, RPVehicle veh)
        {
            if (API.shared.getPlayerVehicle(character.owner.client) == veh.handle) return true;
            return false;
        }

        /// <summary>
        ///  Adds a vehicle into the vehicle manager
        /// </summary>
        /// <param name="id">Vehicle id</param>
        /// <param name="ownerId">Vehicle owner id</param>
        /// <param name="faction">Faction id</param>
        /// <param name="model">Vehicle model</param>
        /// <param name="parkX">Vehicle parking X position</param>
        /// <param name="parkY">Vehicle parking Y position</param>
        /// <param name="parkZ">Vehicle parking Z position</param>
        /// <param name="parkRotX">Vehicle parking X rotation</param>
        /// <param name="parkRotY">Vehicle parking Y rotation</param>
        /// <param name="parkRotZ">Vehicle parking Z rotation</param>
        /// <param name="plateText">Vehicle license plate text</param>
        /// <param name="color1">Vehicle primary color</param>
        /// <param name="color2">Vehicle secondary color</param>
        private void AddVehicleToManager(int id, int ownerId, FactionI faction, string model, float parkX, float parkY, float parkZ, float parkRotX, float parkRotY, float parkRotZ, string plateText, int color1, int color2)
        {
            RPVehicle v = new RPVehicle(id, ownerId, faction, API.shared.vehicleNameToModel(model), parkX, parkY, parkZ, parkRotX, parkRotY, parkRotZ, color1, color2, plateText, false);
            vehicles.Add(v);
        }

        /// <summary>
        /// Set's new parking spot for vehicle
        /// </summary>
        /// <param name="vehicle">Vehicle</param>
        /// <param name="spot">New spot</param>
        /// <param name="spotRot">New rotation</param>
        private void SetParkingSpotForVehicle(RPVehicle vehicle, Vector3 spot, Vector3 spotRot)
        {
            DBManager.UpdateQuery("UPDATE vehicles SET park_x=@park_x, park_y=@park_y, park_z=@park_z, park_rot_x=@park_rot_x, park_rot_y=@park_rot_y, park_rot_z=@park_rot_z WHERE id=@id")
                .AddValue("@park_x", spot.X)
                .AddValue("@park_y", spot.Y)
                .AddValue("@park_z", spot.Z)
                .AddValue("@park_rot_x", spotRot.X)
                .AddValue("@park_rot_y", spotRot.Y)
                .AddValue("@park_rot_z", spotRot.Z)
                .AddValue("@id", vehicle.id)
                .Execute();
        }

        /// <summary>
        /// Gets nearest vehicle
        /// </summary>
        /// <param name="position">Position for which to get the nearest vehicle</param>
        /// <returns>The vehicle</returns>
        public RPVehicle GetNearestVehicle(Vector3 position)
        {
            RPVehicle nearest = vehicles.First(); // fix
            float nearestDist = position.DistanceTo(nearest.position);

            foreach(RPVehicle v in vehicles)
            {
                if(position.DistanceTo(v.position) < nearestDist)
                    nearest = v;
            }

            return nearest;
        }

        /// <summary>
        /// Creates a new vehicle into the database
        /// </summary>
        /// <param name="ownerId">Id of the owning character</param>
        /// <param name="faction">Id of the faction if exists, 0 if civilian</param>
        /// <param name="model">Vehicle model</param>
        /// <param name="parkX">Vehicle park X position</param>
        /// <param name="parkY">Vehicle park Y position</param>
        /// <param name="parkZ">Vehicle park Z position</param>
        /// <param name="parkRotX">Vehicle park X rotation</param>
        /// <param name="parkRotY">Vehicle park Y rotation</param>
        /// <param name="parkRotZ">Vehicle park Z rotation</param>
        /// <param name="plateText">Text of the license plate</param>
        /// <param name="plateStyle">Style of the license plate</param>
        /// <param name="color1">Primary color of the vehicle</param>
        /// <param name="color2">Secondary color of the vehicle</param>
        /// <returns></returns>
        public int AddVehicleToDatabase(int ownerId, FactionI faction, string model, float parkX, float parkY, float parkZ, float parkRotX, float parkRotY, float parkRotZ, string plateText, int plateStyle, int color1, int color2)
        {
            var cmd = DBManager.SimpleQuery("INSERT INTO vehicles VALUES (@id, @ownerId, @faction, @model, @park_x, @park_y, @park_z, @park_rot_x, @park_rot_y, @park_rot_z, @platetxt, @style, @color1, @color2)");
            cmd.Parameters.AddWithValue("@id", this.vehicleInsertId);
            cmd.Parameters.AddWithValue("@ownerId", ownerId);
            cmd.Parameters.AddWithValue("@faction", (int)faction);
            cmd.Parameters.AddWithValue("@model", API.shared.vehicleNameToModel(model));
            cmd.Parameters.AddWithValue("@park_x", parkX);
            cmd.Parameters.AddWithValue("@park_y", parkY);
            cmd.Parameters.AddWithValue("@park_z", parkZ);
            cmd.Parameters.AddWithValue("@park_rot_x", parkRotX);
            cmd.Parameters.AddWithValue("@park_rot_y", parkRotY);
            cmd.Parameters.AddWithValue("@park_rot_z", parkRotZ);
            cmd.Parameters.AddWithValue("@platetxt", plateText);
            cmd.Parameters.AddWithValue("@style", 0);
            cmd.Parameters.AddWithValue("@color1", color1);
            cmd.Parameters.AddWithValue("@color2", color2);
            cmd.ExecuteNonQuery();
            this.AddVehicleToManager(this.vehicleInsertId, ownerId, faction, model, parkX, parkY, parkZ, parkRotX, parkRotY, parkRotZ, plateText, color1, color2);
            this.vehicleInsertId++;
            return this.vehicleInsertId - 1;
        }

        /// <summary>
        /// Generates an unused license plate for civilian vehicle
        /// </summary>
        /// <returns>License plate text</returns>
        public string GenerateUnusedLicensePlate()
        {
            StringBuilder nameBuilder = new StringBuilder();
            do
            {
                nameBuilder.Append("LS");
                nameBuilder.Append(this.GenerateRandomString(6));
            } while (this.DoesVehicleExistWithLicensePlate(nameBuilder.ToString()));

            return nameBuilder.ToString();
        }

        /// <summary>
        /// Return the vehicle that the character is currently using
        /// </summary>
        /// <param name="c">Character</param>
        /// <returns>Vehicle that the character is using, if not using vehicle then null</returns>
        public RPVehicle GetVehicleForCharacter(Character c)
        {
            NetHandle veh = API.shared.getPlayerVehicle(c.owner.client);
            foreach (RPVehicle v in vehicles)
            {
                if (v.handle == veh && v.spawned)
                    return v;
            }
            return null;
        }

        /// <summary>
        /// Sends a message to client that updates his HUD to include a new vehicle
        /// </summary>
        /// <param name="character">Character to whose client to send</param>
        /// <param name="vehicleId">Id of the new vehicle</param>
        /// <param name="licensePlateText">Text of the vehicle's license plate</param>
        /// <param name="spawned">Is vehicle spawned</param>
        public void SendUpdatedVehicleToClient(Character character, int vehicleId, string licensePlateText, bool spawned)
        {
            API.shared.triggerClientEvent(character.owner.client, "EVENT_NEW_VEHICLE_ADDED", vehicleId, licensePlateText, spawned);
        }

        /// <summary>
        /// Tries to purchase a vehicle for client
        /// Is called from event handler
        /// </summary>
        /// <param name="c">Client</param>
        /// <param name="model">Vehicle model</param>
        /// <param name="color1">Vehicle color 1</param>
        /// <param name="color2">Vehicle color 2</param>
        public void TryPurchaseVehicle(Client c, int id, string model, int color1, int color2)
        {
            Character character = PlayerManager.Instance().GetActiveCharacterForClient(c);
            VehicleShop shop = GetVehicleShopWithId(id);
            if (shop != null) shop.PurchaseVehicle(character, model, color1, color2);
        }

        

        /// <summary>
        /// Purchases a parking spot
        /// </summary>
        /// <param name="character">Character who is buying</param>
        /// <param name="vehicle">Vehicle for which the parking spot is being bought for</param>
        public void PurchasePark(Character character, RPVehicle vehicle)
        {
            if (character.ID == vehicle.ownerId)
            {
                if (VehicleManager.Instance().IsCharacterInVehicle(character, vehicle))
                {
                    if (character.money >= buyParkPrice)
                    {
                        character.SetMoney(character.money - buyParkPrice);
                        VehicleManager.Instance().SetParkingSpotForVehicle(vehicle, vehicle.handle.position, vehicle.handle.rotation);
                        vehicle.UpdateParkPosition(vehicle.handle.position, vehicle.handle.rotation);
                        API.shared.sendNotificationToPlayer(character.owner.client, "Parking spot updated!");
                    }
                    else
                        API.shared.sendNotificationToPlayer(character.owner.client, "You don't have enought money to buy a parking spot!");
                }
                else
                    API.shared.sendNotificationToPlayer(character.owner.client, "You have to be inside the vehicle to buy a parking spot!");

            }
            else
                API.shared.sendNotificationToPlayer(character.owner.client, "You are not the owner of the vehicle!");
        }

        /// <summary>
        /// Attempts to purchase a parking spot for a vehicle
        /// </summary>
        /// <param name="c">Client</param>
        public void TryPurchasePark(Client c, int vehicleId)
        {
            Character character = PlayerManager.Instance().GetActiveCharacterForClient(c);
            RPVehicle vehicle = VehicleManager.Instance().GetVehicleWithId(vehicleId);
            if (vehicle != null) VehicleManager.Instance().PurchasePark(character, vehicle);

        }

        /// <summary>
        /// Gets all vehicles that the player owns
        /// </summary>
        /// <param name="character">Character</param>
        /// <returns>List of vehicles</returns>
        public List<RPVehicle> GetVehiclesForCharacter(Character character)
        {
            return vehicles.Where(x => x.ownerId == character.ID).ToList();
        }

        /// <summary>
        /// Returns a vehicle with id
        /// </summary>
        /// <param name="id">Vehicle id</param>
        /// <returns>Vehicle if exists with the id, otherwise null</returns>
        public RPVehicle GetVehicleWithId(int id)
        {
            foreach (RPVehicle v in vehicles)
            {
                if (v.id == id)
                    return v;
            }

            return null;
        }
        

        /// <summary>
        /// Spawns a vehicle for player with certain id
        /// </summary>
        /// <param name="c">Character</param>
        /// <param name="vehicleId">Vehicle id</param>
        public void SpawnVehicleForCharacter(Client c, int vehicleId)
        {
            if (PlayerManager.Instance().IsClientUsingCharacter(c))
            {
                Character character = PlayerManager.Instance().GetActiveCharacterForClient(c);
                RPVehicle vehicle = VehicleManager.Instance().GetVehicleWithId(vehicleId);
                if (vehicle != null && character.ID == vehicle.ownerId)
                {
                    if (!vehicle.spawned) vehicle.Spawn();
                    else API.shared.sendNotificationToPlayer(c, "This vehicle is already active!");
                }
            }

        }

        /// <summary>
        /// Parks a vehicle with selected id
        /// </summary>
        /// <param name="c">Client</param>
        /// <param name="vehicleId">Vehicle id</param>
        public void ParkVehicle(Client c, int vehicleId)
        {
            if (PlayerManager.Instance().IsClientUsingCharacter(c))
            {
                Character character = PlayerManager.Instance().GetActiveCharacterForClient(c);
                RPVehicle vehicle = VehicleManager.Instance().GetVehicleWithId(vehicleId);
                if (vehicle != null && character.ID == vehicle.ownerId && vehicle.spawned)
                {
                    if (vehicle.handle == API.shared.getPlayerVehicle(c))
                    {
                        if (vehicle.position.DistanceTo(vehicle.parkPosition) <= parkDistance) vehicle.Park();
                        else API.shared.sendNotificationToPlayer(c, "You have to close to the parking spot in order to park the vehicle!");
                    }
                    else
                        API.shared.sendNotificationToPlayer(c, "You need to be in the vehicle you want to park!");
                }
            }
        }

        public void LockVehicleWithId(Client client, int id)
        {
            RPVehicle vehicle = GetVehicleWithId(id);

            if (vehicle != null && vehicle.spawned)
            {
                if (PlayerManager.Instance().IsClientUsingCharacter(client))
                {
                    Character character = PlayerManager.Instance().GetActiveCharacterForClient(client);
                    if (vehicle.ownerId == character.ID)
                    {
                        if (vehicle.position.DistanceTo(character.position) < doorLockDistance)
                        {
                            vehicle.ToggleDoorLock();
                            this.SendVehicleLockedMessage(client, vehicle.locked);
                        }
                        else
                            API.shared.sendNotificationToPlayer(client, "You need to be closer to the vehicle");
                    }
                }
            }
            else
            {
                API.shared.sendNotificationToPlayer(client, "Vehicle needs to be active!");
            }
        }

        /// <summary>
        /// Locks/Unlocks the most nearby vehicle if possible
        /// </summary>
        /// <param name="client"></param>
        public void LockVehicle(Client client)
        {
            RPVehicle nearest = VehicleManager.Instance().GetNearestVehicle(client.position);
            if (!PlayerManager.Instance().IsClientUsingCharacter(client)) return;

            if (nearest != null && client.position.DistanceTo(nearest.position) < doorLockDistance)
            {
                Character c = PlayerManager.Instance().GetPlayerByClient(client).activeCharacter;
                Boolean locked = false;

                if (c.factionID == nearest.factionID && c.onDuty) locked = true;
                else if (c.owner.id == nearest.ownerId) locked = true;

                // check if player has keys to the car 

                if (locked)
                {
                    nearest.ToggleDoorLock();
                    this.SendVehicleLockedMessage(client, locked);
                }
            }

        }

        /// <summary>
        /// Character attempts to exit a vehicle shop
        /// </summary>
        /// <param name="c">Client</param>
        /// <param name="id">Shop id</param>
        public void TryExitVehicleShop(Client c, int id)
        {
            if (PlayerManager.Instance().IsClientUsingCharacter(c))
            {
                Character character = PlayerManager.Instance().GetActiveCharacterForClient(c);
                VehicleShop shop = GetVehicleShopWithId(id);
                if (shop != null) shop.ExitShop(character);
            }
        }

        /// <summary>
        /// Loads all vehicles from the database and adds them to the array
        /// </summary>
        public void LoadVehiclesFromDB()
        {
            DBManager.SelectQuery("SELECT * FROM vehicles", (MySql.Data.MySqlClient.MySqlDataReader reader) =>
            {
                API.shared.consoleOutput("Vehicle loaded!");
                RPVehicle v = new RPVehicle(reader.GetInt32(0), reader.GetInt32(1), (FactionI)reader.GetInt32(2), (VehicleHash)reader.GetInt32(3), reader.GetFloat(4), reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7), reader.GetFloat(8), reader.GetFloat(9), reader.GetInt32(12), reader.GetInt32(13), reader.GetString(10), true);
                vehicles.Add(v);
            }).Execute();
        }
    }
}
