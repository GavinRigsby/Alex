using System;
using System.Linq.Expressions;
using System.Numerics;
using log4net;
using MiNET.Entities;
using MiNET.Entities.Passive;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using MiNET.Worlds;

namespace MiNET.AlexDebug
{
    public class CommandHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandHandler));
        
        private PluginCore Core { get; }
        public CommandHandler(PluginCore core)
        {
            Core = core;
        }

        [Command(Name = "testentity", Aliases = new[]{"testentity"})]
        public void SpawnTestEntity(Player player)
        {
            var position = player.KnownPosition;
         //   position.Y = player.Level.GetHeight(position);

         int count = 0;
         Vector3 offset = Vector3.Zero;
         foreach (var i in Enum.GetValues(typeof(EntityType)))
         {
             try
             {
                 TestEntity villager = new TestEntity(player.Level, (EntityType) i);
                 villager.KnownPosition = position + (offset);
                 villager.NoAi = true;

                 var boundingBox = villager.GetBoundingBox();
                 offset += new Vector3((float) boundingBox.Width + 2, 0,0);
                 count++;
                 
                 player.Level.AddEntity(villager);
                 villager.SpawnEntity();
             }
             catch(Exception ex)
             {
                Log.Warn($"Could not spawn entity: {ex.ToString()}"); 
             }
         }

            player.SendMessage($"Spawned {count} entities");
        }

        [Command(Name = "wt", Aliases = new[]{"wt"})]
        public void TestWorldTransfers(Player player)
        {
            var targetLevel = player.Level;
            if (player.Level.LevelId != "w2")
            {
                targetLevel = Core.LevelManager.GetLevel(player, "w2");
            }
            else
            {
                targetLevel = Core.LevelManager.GetLevel(player, Dimension.Overworld.ToString());
            }

            if (targetLevel == player.Level)
            {
                player.SendMessage($"Could not transfer.");
                return;
            }
            
            player.ChangeDimension(targetLevel, player.SpawnPosition, targetLevel.Dimension);
        }

        [Command(Name = "transfer", Aliases = new[] {"transfer"})]
        public void ServerTransferTest(Player player)
        {
            McpeTransfer transfer = McpeTransfer.CreateObject();
            transfer.serverAddress = "test.pmmp.io";
            transfer.port = 19132;
            
            player.SendPacket(transfer);
        }
    }
}