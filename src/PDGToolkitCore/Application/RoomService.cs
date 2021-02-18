﻿using System;
using System.Collections.Generic;
using System.Linq;
using PDGToolkitCore.Application.Extensions;
using PDGToolkitCore.Domain.Models;

namespace PDGToolkitCore.Application
{
    internal class RoomService : IRoomService
    {
        public bool AreRoomsOverlapping(Room firstRoom, Room secondRoom)
        {
            var allRooms = new List<Room>{firstRoom, secondRoom};
            var allTiles = allRooms.SelectMany(r => r.Tiles).ToList();
                
            var allDupePositions = GetPositionsOfDuplicateTiles(allTiles).Keys.ToList();

            foreach (var dupePosition in allDupePositions)
            {
                var numRoomSharingDupePosition = GetRoomsByPosition(allRooms, dupePosition).ToList();
                if (numRoomSharingDupePosition.Count() > 1)
                    return true;
            }

            return false;
        }
        
        public Room MergeRooms(Room r1, Room r2)
        {
            var allTiles = r1.Tiles.Concat(r2.Tiles).ToList();

            if (r1.Equals(r2)) 
                throw new ArgumentException("Can't merge a room with itself!");
            
            var overlappingWalls = r2.Tiles.FindAll(t =>
            {
                var sharedPositionsBetweenRooms = GetRoomsByPosition(new List<Room> {r1, r2}, t.Position);
                
                return sharedPositionsBetweenRooms.Contains(r1) && t.Type.Equals(TileType.Wall) && HasTwoAdjacentFloorTiles(allTiles, t.Position);
            });

            foreach (var tile in overlappingWalls)
            {
                allTiles.ReplaceTilesWithOtherTile(new Tile(TileType.Floor, tile.Position));
            }
            
            return new Room(0, 0, new Position(0, 0), allTiles, r1.Id);
        }

        public IEnumerable<Room> GetRoomsByPosition(IEnumerable<Room> rooms, Position position)
        { 
            var foundRooms = new List<Room>(); 
            foreach (var room in rooms.ToList()) 
            {
               var roomTiles = room.Tiles;
               var foundTiles = roomTiles.FindAll(t => t.Position.Equals(position));
               if (foundTiles.Any())
                   foundRooms.Add(room);
            } 
            return foundRooms;
        }
        
        private Dictionary<Position, int> GetPositionsOfDuplicateTiles(List<Tile> tiles)
        {
            return tiles.GroupBy(t => t.Position)
                .Where(g => g.Count() > 1)
                .Select(y => new { Element = y.Key, Count = y.Count()})
                .ToDictionary(x => x.Element, y => y.Count);
        }
        
        private bool HasTwoAdjacentFloorTiles(List<Tile> allTiles, Position positionOfTileInQuestion)
        {
            var up = new Position(positionOfTileInQuestion.X, positionOfTileInQuestion.Y + 1);
            var down = new Position(positionOfTileInQuestion.X, positionOfTileInQuestion.Y - 1);
            var left = new Position(positionOfTileInQuestion.X - 1, positionOfTileInQuestion.Y);
            var right = new Position(positionOfTileInQuestion.X + 1, positionOfTileInQuestion.Y);
            var adjacentFloorTiles = allTiles.Where(t => t.Type.Equals(TileType.Floor)).ToList().FindAll(t =>
                t.Position.Equals(up) || t.Position.Equals(down) || t.Position.Equals(left) ||
                t.Position.Equals(right));

            var adjacentFloorPositions = adjacentFloorTiles.Select(a => a.Position).ToList(); 
            return adjacentFloorPositions.Contains(up) && adjacentFloorPositions.Contains(down) || adjacentFloorPositions.Contains(right) && adjacentFloorPositions.Contains(left);
        }
    }
}