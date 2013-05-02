﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hooks;
using Terraria;
using TShockAPI;
using System.ComponentModel;

namespace NPCDisguise
{
	[APIVersion(1, 12)]
	public class NPCDisguise : TerrariaPlugin
    {
		public override string Name { get { return "NPCDisguise"; } }
		public override string Author { get { return "Scavenger"; } }
		public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

		dPlayer[] dPlayers { get; set; }
		DateTime LastUpdate { get; set; }

		public NPCDisguise(Main game) : base(game)
		{
			dPlayers = new dPlayer[256];
			LastUpdate = DateTime.UtcNow;
		}

		public override void Initialize()
		{
			GameHooks.Initialize += onInitialize;
			NetHooks.SendData += onSendData;
			NetHooks.GreetPlayer += onGreetPlayer;
			ServerHooks.Leave += onLeave;
			GameHooks.Update += onUpdate;
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GameHooks.Initialize -= onInitialize;
				NetHooks.SendData -= onSendData;
				NetHooks.GreetPlayer -= onGreetPlayer;
				ServerHooks.Leave -= onLeave;
				GameHooks.Update -= onUpdate;
			}
			base.Dispose(disposing);
		}

		#region Join/Leave
		public void onGreetPlayer(int who, HandledEventArgs e)
		{
			try
			{
				dPlayers[who] = new dPlayer(who);
			}
			catch { }
		}
		public void onLeave(int who)
		{
			try
			{
				dPlayers[who] = null;
			}
			catch { }
		}
		#endregion

		void onInitialize()
		{
			Commands.ChatCommands.Add(new Command("npcdisguise", CMDdisguise, "nd"));
		}
		
		void CMDdisguise(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendWarningMessage("Usage: /nd <remove/npc>");
				return;
			}
			var dPly = dPlayers[args.Player.Index];
			if (args.Parameters.Count == 1 && args.Parameters[0] == "remove")
			{
				dPly.Disguised = false;
				dPly.DisguiseNPC = -18;
				dPly.DisguiseNPCname = string.Empty;
				dPly.NPCIndex = -1;
				args.Player.SendInfoMessage("Removed disguise!");
				return;
			}
			var NPCs = TShock.Utils.GetNPCByIdOrName(string.Join(" ", args.Parameters));
			if (NPCs.Count != 1)
			{
				args.Player.SendWarningMessage(NPCs.Count < 1 ? "No NPCs matched!" : "More than one NPC matched!");
				return;
			}
			Main.player[args.Player.Index].position = new Vector2(Main.spawnTileX * 16F, Main.spawnTileY * 16F);
			NetMessage.SendData(13, -1, args.Player.Index, string.Empty, args.Player.Index);
			dPly.Disguised = true;
			dPly.DisguiseNPC = NPCs[0].netID;
			dPly.DisguiseNPCname = NPCs[0].name;
			args.Player.SendInfoMessage("Set disguise to: " + NPCs[0].name);

			int npcid = NPC.NewNPC(args.Player.TileX * 16, args.Player.TileY * 16, dPly.DisguiseNPC, 0);
			Main.npc[npcid].SetDefaults(dPly.DisguiseNPCname);
			dPly.NPCIndex = npcid;
		}
		void onSendData(SendDataEventArgs e)
		{
			try
			{
				if (e.MsgID == PacketTypes.NpcUpdate)
				{
					foreach (var dPly in dPlayers)
					{
						if (dPly != null && dPly.NPCIndex == e.number)
						{
							e.ignoreClient = dPly.Index;
						}
					}
				}
				if (e.MsgID == PacketTypes.PlayerUpdate)
				{
					if (dPlayers[e.ignoreClient].Disguised && dPlayers[e.ignoreClient].NPCIndex > -1)
					{
						var dPly = dPlayers[e.ignoreClient];
						int npcid = dPly.NPCIndex;
						if (Main.npc[npcid] == null || !Main.npc[npcid].active)
						{
							npcid = NPC.NewNPC(dPly.tsPlayer.TileX * 16, dPly.tsPlayer.TileY * 16, dPly.DisguiseNPC, 0);
							Main.npc[npcid].SetDefaults(dPly.DisguiseNPCname);
							dPly.NPCIndex = npcid;
						}
						Main.npc[npcid].position = Main.player[e.ignoreClient].position;
						Main.npc[npcid].velocity = Main.player[e.ignoreClient].velocity;
						Main.npc[npcid].target = -1;
						Main.npc[npcid].direction = Main.player[e.ignoreClient].direction;
						Main.npc[npcid].directionY = 0;
						Main.npc[npcid].life = Main.npc[npcid].lifeMax;
						Main.npc[npcid].netID = dPly.DisguiseNPC;
						e.MsgID = PacketTypes.NpcUpdate;
						e.number =  npcid;
					}
				}
			}
			catch { }
		}
		
		void onUpdate()
		{
			/*if ((DateTime.UtcNow - LastUpdate).TotalMilliseconds >= 250)
			{
				LastUpdate = DateTime.UtcNow;*/
                foreach (var dPly in dPlayers)
                {
                    if (dPly != null && dPly.Disguised && dPly.NPCIndex > -1)
                    {
                        int npcid = dPly.NPCIndex;
                        if (Main.npc[npcid] == null || !Main.npc[npcid].active)
                        {
                            npcid = NPC.NewNPC(dPly.tsPlayer.TileX * 16, dPly.tsPlayer.TileY * 16, dPly.DisguiseNPC, 0);
                            Main.npc[npcid].SetDefaults(dPly.DisguiseNPCname);
                            dPly.NPCIndex = npcid;
                        }
                        Main.npc[npcid].position = Main.player[dPly.Index].position;
                        Main.npc[npcid].velocity = Main.player[dPly.Index].velocity;
                        Main.npc[npcid].target = -1;
                        Main.npc[npcid].direction = Main.player[dPly.Index].direction;
                        Main.npc[npcid].directionY = 0;
                        Main.npc[npcid].life = Main.npc[npcid].lifeMax;
                        Main.npc[npcid].netID = dPly.DisguiseNPC;
                        NetMessage.SendData(23, -1, -1, string.Empty, npcid);
                    }
                }
			/*}*/
		}
    }
	public class dPlayer
	{
		public int Index { get; set; }
		public TSPlayer tsPlayer { get { return TShock.Players[Index]; } }
		public bool Disguised { get; set; }
		public int DisguiseNPC { get; set; }
		public string DisguiseNPCname { get; set; }
		public int NPCIndex { get; set; }

		public dPlayer(int index)
		{
			this.Index = index;
			this.Disguised = false;
			this.DisguiseNPC = -18;
			this.DisguiseNPCname = string.Empty;
			this.NPCIndex = -1;
		}
	}
}
