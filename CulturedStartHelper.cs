﻿using Helpers;
using MountAndBlade.CampaignBehaviors;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace zCulturedStart
{
    public static class CulturedStartHelper
    {
        public static void ApplyStartOptions()
        {
            //Take away all the stuff to apply to each option
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, Hero.MainHero.Gold, true);
            PartyBase.MainParty.ItemRoster.Clear();
            SetCastleToAdd();
            SetCaptorToEscapeFrom();
            MobileParty.MainParty.Position2D = StartingPosition;
            if (GameStateManager.Current.ActiveState is MapState mapState)
            {
                mapState.Handler.ResetCamera();
                mapState.Handler.TeleportCameraToMainParty();
            }
            switch (StartOption)
            {
                case 0: //Default
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                    break;
                case 1: //Merchant
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1600, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                    PartyBase.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("mule"), 5);
                    AddTroops(1, 5);
                    AddTroops(2, 3);
                    break;
                case 2: //Exiled
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 3000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                    AddExiledHero();
                    SetRelationWithRuler();
                    break;
                case 3: //Mercenary
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 250, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 1);
                    AddTroops(1, 10);
                    AddTroops(2, 5);
                    AddTroops(3, 3);
                    AddTroops(4, 1);
                    MobileParty.MainParty.RecentEventsMorale -= 40;
                    Hero.MainHero.BattleEquipment.FillFrom((from character in CharacterObject.All
                                                            where character.Tier == 3 && character.Culture == Hero.MainHero.Culture && !character.IsHero && (character.Occupation == Occupation.Soldier || character.Occupation == Occupation.Mercenary)
                                                            select character).GetRandomElementInefficiently().Equipment);
                    break;
                case 4: //Looter
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 40, true);
                    AddLooters(7);
                    foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
                    {
                        ChangeCrimeRatingAction.Apply(kingdom.MapFaction, 50, false);
                    }
                    break;
                case 5: //Vassal
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 3000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                    SetMainHeroAsVassal();
                    SetEquipment(Hero.MainHero, 3);
                    AddTroops(1, 10);
                    AddTroops(2, 4);
                    break;
                case 6: //Kingdom
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 8000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 15);
                    AddTroops(1, 31);
                    AddTroops(2, 20);
                    AddTroops(3, 14);
                    AddTroops(4, 10);
                    AddTroops(5, 6);
                    CreateKingdom();
                    SetEquipment(Hero.MainHero, 5);
                    Hero.MainHero.Clan.Influence = 100;
                    AddCompanionParties(2);
                    AddCompanions(1);
                    break;
                case 7: //Holding
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 10000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 15);
                    AddCastle();
                    AddTroops(1, 31);
                    AddTroops(2, 20);
                    AddTroops(3, 14);
                    AddTroops(4, 10);
                    AddTroops(5, 6);
                    CreateKingdom();
                    SetEquipment(Hero.MainHero, 5);
                    AddCompanionParties(1);
                    break;
                case 8: //Landed Vassal
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 10000, true);
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                    SetMainHeroAsVassal();
                    AddCastle();
                    SetEquipment(Hero.MainHero, 3);
                    AddTroops(1, 10);
                    AddTroops(2, 4);
                    break;
                case 9: //Escaped Prisoner
                    PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 1);
                    EscapeFromCaptor();
                    break;
                default:
                    break;
            }
            //Culture swap
        }

        private static void SetEquipment(Hero hero, int tier)
        {
            CharacterObject idealTroop = (from character in CharacterObject.All
                                          where character.Tier == tier && character.Culture == hero.Culture && !character.IsHero && character.Equipment.GetHumanBodyArmorSum() > 0
                                          select character).GetRandomElementInefficiently();
            Equipment equipment = idealTroop.Equipment;
            hero.BattleEquipment.FillFrom(equipment);
        }

        private static void SetRelationWithRuler()
        {
            Hero mainHero = Hero.MainHero;
            Hero ruler = Hero.FindAll(hero => (hero.Culture == mainHero.Culture) && hero.IsAlive && hero.IsFactionLeader && !hero.MapFaction.IsMinorFaction).GetRandomElementInefficiently();
            CharacterRelationManager.SetHeroRelation(mainHero, ruler, -50);
            foreach (Hero lord in Hero.FindAll(hero => (hero.MapFaction == ruler.MapFaction) && hero.IsAlive))
            {
                CharacterRelationManager.SetHeroRelation(mainHero, lord, -5);
            }
            if (ruler != null)
            {
                CharacterRelationManager.SetHeroRelation(mainHero, ruler, -50);
                ChangeCrimeRatingAction.Apply(ruler.MapFaction, 49, false);
            }
        }

        private static void SetMainHeroAsVassal()
        {
            Hero mainHero = Hero.MainHero;
            //Find a clan that matches culture
            Hero lord = Hero.FindAll(hero => (hero.Culture == mainHero.Culture) && hero.IsAlive && hero.IsFactionLeader && !hero.MapFaction.IsMinorFaction).GetRandomElementInefficiently();
            if (lord != null)
            {
                //Adding to prevent crash on custom cultures with no kingdom
                Clan targetclan = lord.Clan;
                CharacterRelationManager.SetHeroRelation(mainHero, lord, 10);
                ChangeKingdomAction.ApplyByJoinToKingdom(mainHero.Clan, targetclan.Kingdom, false);
                Hero.MainHero.Clan.Influence = 10;
            }
        }

        private static void SetCastleToAdd()
        {
            Settlement castle;
            castle = (from settlement in Settlement.All
                      where settlement.Culture == Hero.MainHero.Culture && settlement.IsCastle
                      select settlement).GetRandomElementInefficiently();
            if (castle == null) //Adding this for custom cultures that don't have any land to start
            {
                castle = (from settlement in Settlement.All
                          where settlement.IsCastle
                          select settlement).GetRandomElementInefficiently();
            }
            CastleToAdd = castle;
        }

        private static void SetCaptorToEscapeFrom()
        {
            Hero mainHero = Hero.MainHero;
            Hero captor = Hero.FindAll(hero => (hero.Culture == mainHero.Culture) && hero.IsAlive && hero.MapFaction != null && !hero.MapFaction.IsMinorFaction && hero.IsPartyLeader && !hero.PartyBelongedTo.IsHolding).GetRandomElementInefficiently();
            CaptorToEscapeFrom = captor;
        }

        private static void AddTroops(int tier, int num)
        {
            CharacterObject troop = (from character in CharacterObject.All
                                     where character.Tier == tier && character.Culture == Hero.MainHero.Culture && !character.IsHero && (character.Occupation == Occupation.Soldier || character.Occupation == Occupation.Mercenary)
                                     select character).GetRandomElementInefficiently();
            PartyBase.MainParty.AddElementToMemberRoster(troop, num, false);
        }

        private static void AddLooters(int num) //Dual purpose cause lazy, adds looters and sets player's gear as looter
        {
            CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            PartyBase.MainParty.AddElementToMemberRoster(character, num, false);
            Hero.MainHero.BattleEquipment.FillFrom(character.Equipment);
            Hero.MainHero.CivilianEquipment.FillFrom(character.Equipment);
        }

        private static void AddCompanions(int num) => AddCompanion(num, 2000, false);

        private static void AddCompanionParties(int num) => AddCompanion(num, 200, true);

        private static void AddCompanion(int num, int gold, bool shouldCreateParty)
        {
            Hero mainHero = Hero.MainHero;
            CultureObject culture = StartingSettlement.Culture;
            for (int i = 0; i < num; i++)
            {
                CharacterObject wanderer = (from character in CharacterObject.All
                                            where character.Occupation == Occupation.Wanderer && (character.Culture == mainHero.Culture || character.Culture == culture)
                                            select character).GetRandomElementInefficiently();
                Settlement randomSettlement = (from settlement in Settlement.All
                                               where settlement.Culture == wanderer.Culture && settlement.IsTown
                                               select settlement).GetRandomElementInefficiently();
                Hero companion = HeroCreator.CreateSpecialHero(wanderer, randomSettlement, null, null, 33);
                Campaign.Current.GetCampaignBehavior<IHeroCreationCampaignBehavior>().DeriveSkillsFromTraits(companion, wanderer);
                SetEquipment(companion, 4);
                companion.HasMet = true;
                companion.Clan = randomSettlement.OwnerClan;
                companion.ChangeState(Hero.CharacterStates.Active);
                AddCompanionAction.Apply(Clan.PlayerClan, companion);
                AddHeroToPartyAction.Apply(companion, MobileParty.MainParty, true);
                GiveGoldAction.ApplyBetweenCharacters(null, companion, gold, true);
                if (shouldCreateParty)
                {
                    MobilePartyHelper.CreateNewClanMobileParty(companion, mainHero.Clan, out bool fromMainclan);
                }
            }
        }

        private static void AddExiledHero()
        {
            Hero mainhero = Hero.MainHero;
            CharacterObject wanderer = (from character in CharacterObject.All
                                        where character.Occupation == Occupation.Wanderer && character.Culture == mainhero.Culture
                                        select character).GetRandomElementInefficiently();
            Equipment exiledHeroEquipment = (from character in CharacterObject.All
                                             where character.Level > 20 && character.Culture == wanderer.Culture && !character.IsHero && character.Tier > 4
                                             select character).GetRandomElementInefficiently().Equipment;
            Equipment mainHeroEquipment = (from character in CharacterObject.All
                                           where character.Tier == 4 && character.Culture == wanderer.Culture && !character.IsHero
                                           select character).GetRandomElementInefficiently().Equipment;
            Settlement randomSettlement = (from settlement in Settlement.All
                                           where settlement.Culture == wanderer.Culture && settlement.IsTown
                                           select settlement).GetRandomElementInefficiently();
            Hero exiledHero = HeroCreator.CreateSpecialHero(wanderer, randomSettlement, null, null, 33);
            Campaign.Current.GetCampaignBehavior<IHeroCreationCampaignBehavior>().DeriveSkillsFromTraits(exiledHero, wanderer);
            GiveGoldAction.ApplyBetweenCharacters(null, exiledHero, 4000, true);
            exiledHero.BattleEquipment.FillFrom(exiledHeroEquipment);
            mainhero.BattleEquipment.FillFrom(mainHeroEquipment);
            exiledHero.HasMet = true;
            exiledHero.Clan = randomSettlement.OwnerClan;
            exiledHero.ChangeState(Hero.CharacterStates.Active);
            AddCompanionAction.Apply(Clan.PlayerClan, exiledHero);
            AddHeroToPartyAction.Apply(exiledHero, MobileParty.MainParty, true);
        }

        private static void AddCastle() => ChangeOwnerOfSettlementAction.ApplyByKingDecision(Hero.MainHero, CastleToAdd);

        private static void CreateKingdom()
        {
            //This is from cheat, works but not thoroughly tested
            Kingdom kingdom = MBObjectManager.Instance.CreateObject<Kingdom>("player_kingdom");
            TextObject textObject = new TextObject("{=yGaGlXgQ}Player Kingdom", null);
            kingdom.InitializeKingdom(textObject, textObject, Clan.PlayerClan.Culture, Clan.PlayerClan.Banner, Clan.PlayerClan.Color, Clan.PlayerClan.Color2, StartingSettlement, null, null, null);
            ChangeKingdomAction.ApplyByCreateKingdom(Clan.PlayerClan, kingdom, false);
            kingdom.RulingClan = Clan.PlayerClan;
        }

        private static void EscapeFromCaptor() //Escaped Prisoner start 
        {
            if (CaptorToEscapeFrom != null)
            {
                CharacterRelationManager.SetHeroRelation(Hero.MainHero, CaptorToEscapeFrom, -50);
            }
            //Using Looter gear as baseline
            CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Hero.MainHero.BattleEquipment.FillFrom(character.Equipment);
            Hero.MainHero.CivilianEquipment.FillFrom(character.Equipment);
        }

        public static void SetQuestOption(int questOption) => QuestOption = questOption;
        public static void SetStartOption(int startOption) => StartOption = startOption;
        public static void SetLocationOption(int locationOption) => LocationOption = locationOption;

        //0 = Default, 1 = Skip
        public static int QuestOption { get; set; }

        //0 = Default, 1 = Merchant, 2 = Exiled, 3 = Mercenary, 4 = Looter, 5 = Vassal, 6 = Kingdom, 7 = Holding, 8 = Landed Vassal, 9 = Escaped Prisoner
        public static int StartOption { get; set; }

        //0 = Hometown, 1 = Random, 2 - 7 = Specific Town, 8 = Castle, 9 = Escaping
        public static int LocationOption { get; set; }

        public static Settlement CastleToAdd { get; set; }

        public static Hero CaptorToEscapeFrom { get; set; }

        public static Settlement StartingSettlement
        {
            get
            {
                switch (LocationOption)
                {
                    case 0:
                        return Hero.MainHero.HomeSettlement;
                    case 1:
                        return Settlement.FindAll(settlement => settlement.IsTown).GetRandomElementInefficiently();
                    case 2:
                        return Settlement.Find("town_A8");
                    case 3:
                        return Settlement.Find("town_B2");
                    case 4:
                        return Settlement.Find("town_EW2");
                    case 5:
                        return Settlement.Find("town_S2");
                    case 6:
                        return Settlement.Find("town_K4");
                    case 7:
                        return Settlement.Find("town_V3");
                    case 8:
                        return CastleToAdd;
                    default:
                        return Settlement.Find("tutorial_training_field");
                }
            }
        }

        public static Vec2 StartingPosition
        {
            get
            {
                if (LocationOption != 9)
                {
                    return StartingSettlement.GatePosition;
                }
                else
                {
                    return CaptorToEscapeFrom.PartyBelongedTo.Position2D;
                }
            }
        }
    }
}