using System;
using System.Collections.Specialized;
using System.Threading;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
	public class PerfectRecoveryAbility : TimedRealmAbility
	{
		public PerfectRecoveryAbility(DbAbility dba, int level) : base(dba, level) { }
        private Int32 m_resurrectValue = 5;
		private const String RESURRECT_CASTER_PROPERTY = "RESURRECT_CASTER";
        protected readonly ListDictionary m_resTimersByLiving = new ListDictionary();
        private readonly Lock _lock = new();

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) 
				return;
			GamePlayer player = living as GamePlayer;

			if (player == null) 
				return;

			GamePlayer targetPlayer = null;
			bool isGoodTarget = true;

            m_resurrectValue = GetResurrectValue();

            if (player.TargetObject == null)
			{
				isGoodTarget = false;
			}
			else
			{
				targetPlayer = player.TargetObject as GamePlayer;

				if (targetPlayer == null ||
					targetPlayer.IsAlive ||
					GameServer.ServerRules.IsSameRealm(living, player.TargetObject as GameLiving, true) == false)
				{
					isGoodTarget = false;
				}
			}

			if (isGoodTarget == false)
			{
				player.Out.SendMessage("You have to target a dead member of your realm!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return;
			}
			
			GameLiving resurrectionCaster = targetPlayer.TempProperties.GetProperty<GameLiving>(RESURRECT_CASTER_PROPERTY);
			if (resurrectionCaster != null)
			{
				player.Out.SendMessage("Your target is already considering a resurrection!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return;
			}
            if( !player.IsWithinRadius( targetPlayer, (int)( 1500 * player.GetModified(eProperty.SpellRange) * 0.01 ) ) )

			{
				player.Out.SendMessage("You are too far away from your target to use this ability!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return;
			}
			if (targetPlayer != null)
			{
				SendCasterSpellEffectAndCastMessage(targetPlayer, 7019, true);
				DisableSkill(living);
                //Lifeflight:
                //don't rez just yet
				//ResurrectLiving(targetPlayer, player);
                //we need to add a dialogue response to the rez, copying from the rez spellhandler

				targetPlayer.TempProperties.SetProperty(RESURRECT_CASTER_PROPERTY, living);
				ECSGameTimer resurrectExpiredTimer = new ECSGameTimer(targetPlayer);
				resurrectExpiredTimer.Callback = new ECSGameTimer.ECSTimerCallback(ResurrectExpiredCallback);
				resurrectExpiredTimer.Properties.SetProperty("targetPlayer", targetPlayer);
				resurrectExpiredTimer.Start(15000);
				lock (_lock)
				{
                    m_resTimersByLiving.Add(player.TargetObject, resurrectExpiredTimer);
				}

				//send resurrect dialog
                targetPlayer.Out.SendCustomDialog("Do you allow " + living.GetName(0, true) + " to resurrect you\n with " + m_resurrectValue + " percent hits/power (PR)?", new CustomDialogResponse(ResurrectResponceHandler));

			}
		}

        //Lifeflight add
        /// <summary>
        /// Resurrects target if it accepts
        /// </summary>
        /// <param name="player"></param>
        /// <param name="response"></param>
        protected virtual void ResurrectResponceHandler(GamePlayer player, byte response)
        {
            ECSGameTimer resurrectExpiredTimer = null;
            lock (_lock)
            {
                resurrectExpiredTimer = (ECSGameTimer)m_resTimersByLiving[player];
                m_resTimersByLiving.Remove(player);
            }
            if (resurrectExpiredTimer != null)
            {
                resurrectExpiredTimer.Stop();
            }

            GameLiving rezzer = player.TempProperties.GetProperty<GameLiving>(RESURRECT_CASTER_PROPERTY);
            if (!player.IsAlive)
            {
                if (rezzer == null)
                {
                    player.Out.SendMessage("No one is currently trying to resurrect you.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    if (response == 1)
                    {
                        ResurrectLiving(player, rezzer); //accepted
         
                    }
                    else
                    {
                        player.Out.SendMessage("You decline to be resurrected.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        //Dont need to refund anything with PR
                        //m_caster.Mana += CalculateNeededPower(player);
                        //but we do need to give them PR back
                        //Lifeflight: Seems like the best way to do this is to send a 0 duration to DisableSkill, which will enable to ability
                        (rezzer as GameLiving).DisableSkill(this, 0);
                        
                    }
                }
            }
            player.TempProperties.RemoveProperty(RESURRECT_CASTER_PROPERTY);
        }

        //Lifeflight add
        /// <summary>
        /// Cancels resurrection after some time
        /// </summary>
        /// <param name="callingTimer"></param>
        /// <returns></returns>
        protected virtual int ResurrectExpiredCallback(ECSGameTimer callingTimer)
        {
            GamePlayer player = callingTimer.Properties.GetProperty<GamePlayer>("targetPlayer");
            if (player == null) return 0;
            player.TempProperties.RemoveProperty(RESURRECT_CASTER_PROPERTY);
            player.Out.SendMessage("Your resurrection spell has expired.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return 0;
        }


		public void ResurrectLiving(GamePlayer resurrectedPlayer, GameLiving rezzer)
		{
			if (rezzer.ObjectState != GameObject.eObjectState.Active)
				return;
			if (rezzer.CurrentRegionID != resurrectedPlayer.CurrentRegionID)
				return;
			resurrectedPlayer.Health = (int)(resurrectedPlayer.MaxHealth * m_resurrectValue / 100);
			resurrectedPlayer.Mana = (int)(resurrectedPlayer.MaxMana * m_resurrectValue / 100);
			resurrectedPlayer.Endurance = (int)(resurrectedPlayer.MaxEndurance * m_resurrectValue / 100); //no endurance after any rez
			resurrectedPlayer.MoveTo(rezzer.CurrentRegionID, rezzer.X, rezzer.Y, rezzer.Z, rezzer.Heading);

            GameLiving living = resurrectedPlayer as GameLiving;
            ECSGameTimer resurrectExpiredTimer = null;
            lock (_lock)
            {
                resurrectExpiredTimer = (ECSGameTimer)m_resTimersByLiving[living];
                m_resTimersByLiving.Remove(living);
            }
            if (resurrectExpiredTimer != null)
            {
                resurrectExpiredTimer.Stop();
            }

            resurrectedPlayer.StopReleaseTimer();
			resurrectedPlayer.Out.SendPlayerRevive(resurrectedPlayer);
			resurrectedPlayer.UpdatePlayerStatus();

			GameSpellEffect effect = SpellHandler.FindEffectOnTarget(resurrectedPlayer, "PveResurrectionIllness");
			if (effect != null)
				effect.Cancel(false);
			GameSpellEffect effecttwo = SpellHandler.FindEffectOnTarget(resurrectedPlayer, "RvrResurrectionIllness");
			if (effecttwo != null)
				effecttwo.Cancel(false);
			resurrectedPlayer.Out.SendMessage("You have been resurrected by " + rezzer.GetName(0, false) + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //Lifeflight: this should make it so players who have been ressurected don't take damage for 5 seconds
            RezDmgImmunityEffect rezImmune = new RezDmgImmunityEffect();
            rezImmune.Start(resurrectedPlayer);

            //Lifeflight: We need to reward rez RPs
            GamePlayer casterPlayer = rezzer as GamePlayer;
            if (casterPlayer != null)
            {
                long rezRps = resurrectedPlayer.LastDeathRealmPoints * (m_resurrectValue + 50) / 1000;
                if (rezRps > 0)
                {
                    casterPlayer.GainRealmPoints(rezRps);
                }
                else
                {
                    casterPlayer.Out.SendMessage("The player you resurrected was not worth realm points on death.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    casterPlayer.Out.SendMessage("You thus get no realm points for the resurrect.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
            }

        }
		public override int GetReUseDelay(int level)
		{
			return 300;
		}

        protected virtual Int32 GetResurrectValue()
        {
            if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1: return 10;
                    case 2: return 25;
                    case 3: return 50;
                    case 4: return 75;
                    case 5: return 100;
                }
            }
            else
            {
                switch (Level)
                {
                    case 2: return 50;
                    case 3: return 100;
                }
            }
            return 5;
        }
	}
}
