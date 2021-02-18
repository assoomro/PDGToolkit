﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PDGToolkitCore.Domain.Models;
using PDGToolkitCore.Infrastructure;
using PDGToolkitCore.Infrastructure.DebugExtensions;

namespace PDGToolkitCore.Application
{
    internal class TeenyZonesGenerator : IGenerator
    {
        private readonly Settings settings;
        private readonly Random random = new Random();
        private readonly IRoomService roomService;
        private const int MinimumRoomSize = 3;
        private const int OneInXChanceToGenerateARoom = 600;

        private int Width { get; }
        private int Height { get; }
        
        public TeenyZonesGenerator(Settings settings, IRoomService roomService)
        {
            this.settings = settings;
            this.roomService = roomService;
            Width = settings.GridSettings.Width / settings.TileSettings.Size;
            Height = settings.GridSettings.Height / settings.TileSettings.Size;
        }

        /**
         * Generates a Grid asynchronously, using the RoomBuilder class.
         * It does so by generating a room in the size of the dungeon and fills it using <see cref="GenerateRooms"/> function,
         */
        public async Task<Grid> GenerateGridAsync()
        {
            var room = await RoomBuilder.Create()
                .WithHeight(Height)
                .WithWidth(Width)
                .WithInsideTiles(wallThickness => GenerateRooms(wallThickness))
                .WithOutsideWalls()
                .BuildAsync();
          
            return new Grid(settings.GridSettings.Height, settings.GridSettings.Width,
                new TileConfig(settings.TileSettings.Size), room.Tiles);
        }

        /**
         * Select random points in the room using the <see cref="OneInXChanceToGenerateARoom"/> function,
         * then create new, smaller rooms of varying size <see cref="SelectRoomWidth"/> & <see cref="SelectRoomHeight"/>
         * and lastly, use <see cref="ITileService"/> to remove merge overlapping rooms into one bigger, more naturally
         * looking room.
         */
        private async Task<List<Tile>> GenerateRooms(int wallThickness)
        {
            var allRooms = new List<Room>();
            
            for (var x = wallThickness; x < Width - wallThickness; x++)
            {
                for (var y = wallThickness; y < Height - wallThickness; y++)
                {
                    if (OneIn(OneInXChanceToGenerateARoom))
                    {
                        var room = await RoomBuilder.Create()
                            .WithWidth(SelectRoomWidth)
                            .WithHeight(SelectRoomHeight)
                            .WithStartingPosition(new Position(x, y))
                            .WithOutsideWalls()
                            .WithInsideTilesOfType(TileType.Floor)
                            .BuildAsync();

                        for (var i = 0; i < allRooms.Count; i++)
                        {
                            var existingRoom = allRooms[i];
                            if (roomService.AreRoomsOverlapping(room, existingRoom))
                            {
                                allRooms.RemoveAt(i);
                                room = roomService.MergeRooms(existingRoom, room);
                            }
                        }
                        
                        allRooms.Add(room);
                    }
                }
            }

            var tiles = allRooms.SelectMany(r => r.Tiles).ToList();
            allRooms.ListRooms();

            return tiles;
        }

        // TODO: Refactor magic number to a meaningful variable
        private int SelectRoomWidth => RandomlySelectWallLength(Width / 4);
        private int SelectRoomHeight => RandomlySelectWallLength(Height / 4);
        
        private int RandomlySelectWallLength(int maxLength)
        {
            return random.Next(MinimumRoomSize, maxLength);
        }

        /**
         * Perform a pseudo-random roll, having one in <paramref name="chance"/> to succeed,
         * by generating a number between 0 and <paramref name="chance"/> - 1.
         * <returns> true if roll is 0, false otherwise</returns>
         */
        private bool OneIn(int chance)
        {
            return random.Next(chance) < 1;
        }
    }
}