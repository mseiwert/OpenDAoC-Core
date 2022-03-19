using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest.Midgard
{
	public class CaptureKeepQuestMid : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Daily] Frontier Conquerer";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;

		// Capture Goal
		protected const int MAX_CAPTURED = 1;
		
		private static GameNPC Herou = null; // Start NPC

		private int _isCaptured = 0;

		// Constructors
		public CaptureKeepQuestMid() : base()
		{
		}

		public CaptureKeepQuestMid(GamePlayer questingPlayer) : base(questingPlayer, 1)
		{
		}

		public CaptureKeepQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public CaptureKeepQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}
		
		public override int Level
		{
			get
			{
				// Quest Level
				return minimumLevel;
			}
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Herou", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 766332 && npc.Y == 670275)
					{
						Herou = npc;
						break;
					}

			if (Herou == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Herou , creating it ...");
				Herou = new GameNPC();
				Herou.Model = 142;
				Herou.Name = "Herou";
				Herou.GuildName = "Atlas Quest";
				Herou.Realm = eRealm.Midgard;
				//Svasud Faste Location
				Herou.CurrentRegionID = 100;
				Herou.Size = 50;
				Herou.Level = 59;
				Herou.X = 766332;
				Herou.Y = 670275;
				Herou.Z = 5736;
				Herou.Heading = 2835;
				Herou.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Herou.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Herou, GameObjectEvent.Interact, new DOLEventHandler(TalkToHerou));
			GameEventMgr.AddHandler(Herou, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHerou));

			/* Now we bring to Herou the possibility to give this quest to players */
			Herou.AddQuestToGive(typeof (CaptureKeepQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Herou == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Herou, GameObjectEvent.Interact, new DOLEventHandler(TalkToHerou));
			GameEventMgr.RemoveHandler(Herou, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHerou));

			/* Now we remove to Herou the possibility to give this quest to players */
			Herou.RemoveQuestToGive(typeof (CaptureKeepQuestMid));
		}

		protected static void TalkToHerou(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Herou.CanGiveQuest(typeof (CaptureKeepQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			CaptureKeepQuestMid quest = player.IsDoingQuest(typeof (CaptureKeepQuestMid)) as CaptureKeepQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Herou.SayTo(player, "Find an enemy occupied keep and capture it. If you succeed come back for your reward.");
							break;
						case 2:
							Herou.SayTo(player, "Hello " + player.Name + ", did you [capture] a keep?");
							break;
					}
				}
				else
				{
					Herou.SayTo(player, "Hello "+ player.Name +", I am Herou. I help the king with logistics, and he's tasked me with getting things done around here. "+
					                    "I've seen you battling in our frontiers. Do you think you're strong enough to help me with some real estate matters? \n"+
					                    "\nThe king wants us to [reclaim a keep] that he's particularly fond of.");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "reclaim a keep":
							player.Out.SendQuestSubscribeCommand(Herou, QuestMgr.GetIDForQuestType(typeof(CaptureKeepQuestMid)), "Will you help Herou "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "capture":
							if (quest.Step == 2)
							{
								player.Out.SendMessage("Thank you for your contribution!", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
								quest.FinishQuest();
							}
							break;
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (CaptureKeepQuestMid)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			CaptureKeepQuestMid quest = player.IsDoingQuest(typeof (CaptureKeepQuestMid)) as CaptureKeepQuestMid;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and finish your work!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(CaptureKeepQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Herou.CanGiveQuest(typeof (CaptureKeepQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (CaptureKeepQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Herou.GiveQuest(typeof (CaptureKeepQuestMid), player, 1))
					return;

				Herou.SayTo(player, "Thank you "+player.Name+", be an enrichment for our realm!");

			}
		}

		//Set quest name
		public override string Name
		{
			get { return questTitle; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Go to the battlefield and conquer a keep. \nCaptured: Keep ("+ _isCaptured +" | 1)";
					case 2:
						return "Return to Herou for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(CaptureKeepQuestMid)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;
			
			if (Step == 1 && e == GamePlayerEvent.CapturedKeepsChanged)
			{
				_isCaptured = 1;
				player.Out.SendMessage("[Daily] Captured Keep: ("+_isCaptured+" | "+MAX_CAPTURED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
					
				if (_isCaptured >= MAX_CAPTURED)
				{
					// FinishQuest or go back to Dean
					Step = 2;
				}
				
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "CaptureKeepQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			
		}

		public override void SaveQuestParameters()
		{
			
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5, true);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level*2,0,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1000);
			_isCaptured = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
