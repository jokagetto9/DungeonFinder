// Project:         Dungeon Charter mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2024 GhostPutty
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          GhostPutty

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using Wenzil.Console;

namespace DungeonFinder
{
//--------------------------------------------- save data ---------------------------------------------
	[FullSerializer.fsObject("v1")]
	public class DungeonFinderSaveData{
		public bool QuestActive;
		public List<int> QuestStatus; 
		public List<int> DaedraStatus; 
	}
	
	public class DungeonFinder : MonoBehaviour, IHasModSaveData 
	{
		public Type SaveDataType
		{
			get { return typeof(DungeonFinderSaveData); }
		}

		public object NewSaveData()
		{
			return new DungeonFinderSaveData
			{
				QuestActive = false,
				QuestStatus = new List<int>(),
				DaedraStatus = new List<int>()
			};
		}

		public object GetSaveData()
		{
			Debug.Log(" Saving DungeonFinder ");
			return new DungeonFinderSaveData
			{
				QuestActive = activeQuest,
				QuestStatus = DRQuestStatus,
				DaedraStatus = DRDaedraStatus
				
			};			
		}

		public void RestoreSaveData(object saveData)
		{
			Debug.Log(" Loading DungeonFinder ");
			var dungeonFinderSaveData = (DungeonFinderSaveData)saveData;
			
			activeQuest = dungeonFinderSaveData.QuestActive;
			
			DRQuestStatus = dungeonFinderSaveData.QuestStatus;
			DRDaedraStatus = dungeonFinderSaveData.DaedraStatus;
            if (DRQuestStatus == null)
                DRQuestStatus = new List<int>();
            if (DRDaedraStatus == null)
                DRDaedraStatus = new List<int>();
			InitQuestStatus();
		}		
				
		public static void InitQuestStatus(){
			if (DRQuestStatus.Count == 0){
				Debug.Log(" Creating New DungeonFinder Quest Status Data ");
				for (int i = 0; i < 100; i++)
					DRQuestStatus.Add(0);				
			}
			if (DRDaedraStatus.Count == 0){
				for (int i = 0; i < 16; i++)
					DRDaedraStatus.Add(1);
			}		
		}		
				
//--------------------------------------------- structures ---------------------------------------------
				
		public struct DaedraQuestData
		{
			public readonly int index;
			public readonly string quest;
			public readonly string daedraName;
			public readonly int scoreReq;

			public DaedraQuestData(string questFile, int req, int id, string name)
			{
				this.index = id;
				this.quest = questFile;
				this.daedraName = name;
				this.scoreReq = req;
			}
		}
		
		public static DaedraQuestData[] daedraQuestData = {
			new DaedraQuestData("X0C0Y000", 20, (int) FactionFile.FactionIDs.Hircine, "Hircine"),
			new DaedraQuestData("V0C0Y000", 25, (int) FactionFile.FactionIDs.Clavicus_Vile, "Clavicus Vile"),
			new DaedraQuestData("Y0C0Y000", 21, (int) FactionFile.FactionIDs.Mehrunes_Dagon, "Mehrunes Dagon"),
			new DaedraQuestData("20C0Y000", 30, (int) FactionFile.FactionIDs.Molag_Bal, "Molag Bal"),
			new DaedraQuestData("70C0Y000", 20, (int) FactionFile.FactionIDs.Sanguine, "Sanguine"),
			new DaedraQuestData("50C0Y000", 29, (int) FactionFile.FactionIDs.Peryite, "Peryite"),
			new DaedraQuestData("80C0Y000", 30, (int) FactionFile.FactionIDs.Malacath, "Malacath"),
			new DaedraQuestData("W0C0Y000", 31, (int) FactionFile.FactionIDs.Hermaeus_Mora, "Hermaeus Mora"),
			new DaedraQuestData("60C0Y000", 20, (int) FactionFile.FactionIDs.Sheogorath, "Sheogorath"),
			new DaedraQuestData("U0C0Y000", 24, (int) FactionFile.FactionIDs.Boethiah, "Boethiah"),
			new DaedraQuestData("30C0Y000", 36, (int) FactionFile.FactionIDs.Namira, "Namira"),
			new DaedraQuestData("10C0Y000", 29, (int) FactionFile.FactionIDs.Meridia, "Meridia"),
			new DaedraQuestData("90C0Y000", 30, (int) FactionFile.FactionIDs.Vaernima, "Vaernima"),
			new DaedraQuestData("40C0Y000", 29, (int) FactionFile.FactionIDs.Nocturnal, "Nocturnal"),
			new DaedraQuestData("Z0C0Y000", 29, (int) FactionFile.FactionIDs.Mephala, "Mephala"),
			new DaedraQuestData("T0C0Y000", 27, (int) FactionFile.FactionIDs.Azura, "Azura"),
		};
		
		public static int[] DaedraFactionConversion = {0, 1, 2, 3, 0, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
			
		
		static Mod mod;
		static DungeonFinder instance;
		static int guildFactionId = 60;
		static bool activeQuest = false;
		static int QuestDataCount = 91;
		static List<int> DRQuestStatus = new List<int>();
		static List<int> DRDaedraStatus = new List<int>();
		static DungeonRegionQuestData questData;
		
		public struct DungeonRegionQuestData
		{
			public readonly int region;
			public readonly int dungeon;
			public readonly string quest;
			public readonly string dungeonName;
			public readonly int daedraId;
			public readonly int score;

			public DungeonRegionQuestData(string questFile, int regionId, int points, int dungeonId, string dungName, int prince)
			{
				this.region = regionId;
				this.dungeon = dungeonId;
				this.quest = questFile;
				this.dungeonName = dungName;
				this.daedraId = prince;
				this.score = points;
			}
		}
		
        public enum RegionQuests
        {
			Q0000 = 0,
			Q0004 = 1,
			Q0006 = 2,
			Q0011 = 3,
			Q0017 = 4,
			Q0100 = 5,
			Q0101 = 6,
			Q0102 = 7,
			Q0106 = 8,
			Q0116 = 9,
			Q0500 = 10,
			Q0503 = 11,
			Q0504 = 12,
			Q0508 = 13,
			Q0509 = 14,
			Q0511 = 15,
			Q1100 = 16,
			Q1106 = 17,
			Q1114 = 18,
			Q1117 = 19,
			Q1600 = 20,
			Q1601 = 21,
			Q1606 = 22,
			Q1608 = 23,
			Q1609 = 24,
			Q1613 = 25,
			Q1615 = 26,
			Q1616 = 27,
			Q1700 = 28,
			Q1703 = 29,
			Q1707 = 30,
			Q1708 = 31,
			Q1709 = 32,
			Q1711 = 33,
			Q1713 = 34,
			Q1714 = 35,
			Q1800 = 36,
			Q1804 = 37,
			Q1807 = 38,
			Q1808 = 39,
			Q1809 = 40,
			Q2006 = 41,
			Q2007 = 42,
			Q2011 = 43,
			Q2017 = 44,
			Q2100 = 45,
			Q2102 = 46,
			Q2104 = 47,
			Q2106 = 48,
			Q2107 = 49,
			Q2108 = 50,
			Q2111 = 51,
			Q2301 = 52,
			Q2304 = 53,
			Q2307 = 54,
			Q2308 = 55,
			Q2311 = 56,
			Q2601 = 57,
			Q3207 = 58,
			Q3210 = 59,
			Q4006 = 60,
			Q4009 = 61,
			Q4011 = 62,
			Q4012 = 63,
			Q4015 = 64,
			Q4310 = 65,
			Q4810 = 66,
			Q4817 = 67,
			Q5100 = 68,
			Q5101 = 69,
			Q5102 = 70,
			Q5104 = 71,
			Q5107 = 72,
			Q5108 = 73,
			Q5109 = 74,
			Q5111 = 75,
			Q5112 = 76,
			Q5113 = 77,
			Q5117 = 78,
			Q6000 = 79,
			Q6002 = 80,
			Q6003 = 81,
			Q6004 = 82,
			Q6006 = 83,
			Q6007 = 84,
			Q6008 = 85,
			Q6009 = 86,
			Q6011 = 87,
			Q6012 = 88,
			Q6013 = 89,
			Q6015 = 90,
        }

		public static DungeonRegionQuestData[] dungeonRegionQuests = {
//			new DungeonRegionQuestData("EXPL1114", 11, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.DragonsDen, "Dak'fron Dragon Dens", (int) FactionFile.FactionIDs.Peryite),
//			new DungeonRegionQuestData("EXPL1714", 17, 20, (int) DaggerfallConnect.DFRegion.DungeonTypes.DragonsDen, "Daggerfall Dragon Dens", (int) FactionFile.FactionIDs.Peryite),
//			new DungeonRegionQuestData("EXPL1808", 18, 29, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Glenpoint Vampire Haunts", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL0000", 0, 23, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Alik'r Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL0004", 0, 26, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Alik'r Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL0006", 0, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Alik'r Nature Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL0011", 0, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Alik'r Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL0017", 0, 21, (int) DaggerfallConnect.DFRegion.DungeonTypes.ScorpionNest,  "Alik'r Scorpion Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL0100", 1, 20, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Dragontail Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL0101", 1, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.OrcStronghold,  "Dragontail Orc Strongholds", (int) FactionFile.FactionIDs.Boethiah),
			new DungeonRegionQuestData("EXPL0102", 1, 13, (int) DaggerfallConnect.DFRegion.DungeonTypes.HumanStronghold,  "Dragontail Human Strongholds", (int) FactionFile.FactionIDs.Clavicus_Vile),
			new DungeonRegionQuestData("EXPL0106", 1, 29, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Dragontail Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL0116", 1, 29, (int) DaggerfallConnect.DFRegion.DungeonTypes.VolcanicCaves,  "Dragontail Volcanic Caves", (int) FactionFile.FactionIDs.Peryite),
			new DungeonRegionQuestData("EXPL0500", 5, 15, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Dwynnen Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL0503", 5, 13, (int) DaggerfallConnect.DFRegion.DungeonTypes.Prison,  "Dwynnen Prisons", (int) FactionFile.FactionIDs.Mehrunes_Dagon),
			new DungeonRegionQuestData("EXPL0504", 5, 19, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Dwynnen Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL0508", 5, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Dwynnen Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL0509", 5, 10, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Dwynnen Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL0511", 5, 23, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Dwynnen Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL1100", 11, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Dak'fron Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL1106", 11, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Dak'fron Natural Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL1114", 11, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.DragonsDen,  "Dak'fron Dragon Dens", (int) FactionFile.FactionIDs.Peryite),
			new DungeonRegionQuestData("EXPL1117", 11, 22, (int) DaggerfallConnect.DFRegion.DungeonTypes.ScorpionNest,  "Dak'fron Scorpion Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL1600", 16, 25, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Wrothgarian Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL1601", 16, 22, (int) DaggerfallConnect.DFRegion.DungeonTypes.OrcStronghold,  "Wrothgarian Orc Strongholds", (int) FactionFile.FactionIDs.Boethiah),
			new DungeonRegionQuestData("EXPL1606", 16, 29, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Wrothgarian Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL1608", 16, 25, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Wrothgarian Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL1609", 16, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Wrothgarian Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL1613", 16, 22, (int) DaggerfallConnect.DFRegion.DungeonTypes.GiantStronghold,  "Wrothgarian Giant StrongHolds", (int) FactionFile.FactionIDs.Molag_Bal),
			new DungeonRegionQuestData("EXPL1615", 16, 15, (int) DaggerfallConnect.DFRegion.DungeonTypes.BarbarianStronghold,  "Wrothgarian Barbarian Strongholds", (int) FactionFile.FactionIDs.Sanguine ),
			new DungeonRegionQuestData("EXPL1616", 16, 29, (int) DaggerfallConnect.DFRegion.DungeonTypes.VolcanicCaves,  "Wrothgarian Volcanic Caves", (int) FactionFile.FactionIDs.Peryite),
			new DungeonRegionQuestData("EXPL1700", 17, 25, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Daggerfall Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL1703", 17, 21, (int) DaggerfallConnect.DFRegion.DungeonTypes.Prison,  "Daggerfall Prisons", (int) FactionFile.FactionIDs.Mehrunes_Dagon),
			new DungeonRegionQuestData("EXPL1707", 17, 23, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Daggerfall Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL1708", 17, 21, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Daggerfall Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL1709", 17, 21, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Daggerfall Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL1711", 17, 24, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Daggerfall Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL1713", 17, 18, (int) DaggerfallConnect.DFRegion.DungeonTypes.GiantStronghold,  "Daggerfall Giant StrongHolds", (int) FactionFile.FactionIDs.Molag_Bal),
			new DungeonRegionQuestData("EXPL1714", 17, 20, (int) DaggerfallConnect.DFRegion.DungeonTypes.DragonsDen,  "Daggerfall Dragon Dens", (int) FactionFile.FactionIDs.Peryite),
			new DungeonRegionQuestData("EXPL1800", 18, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Glenpoint Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL1804", 18, 13, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Glenpoint Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL1807", 18, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Glenpoint Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL1808", 18, 29, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Glenpoint Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL1809", 18, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Glenpoint Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL2006", 20, 20, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Sentinel Natural Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL2007", 20, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Sentinel Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL2011", 20, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Sentinel Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL2017", 20, 10, (int) DaggerfallConnect.DFRegion.DungeonTypes.ScorpionNest,  "Sentinel Scorpion Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL2100", 21, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Anticlere Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL2102", 21, 13, (int) DaggerfallConnect.DFRegion.DungeonTypes.HumanStronghold,  "Anticlere Human Strongholds", (int) FactionFile.FactionIDs.Clavicus_Vile),
			new DungeonRegionQuestData("EXPL2104", 21, 13, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Anticlere Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL2106", 21, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Anticlere Natural Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL2107", 21, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Anticlere Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL2108", 21, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Anticlere Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL2111", 21, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Anticlere Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL2301", 23, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.OrcStronghold,  "Wayrest Orc Strongholds", (int) FactionFile.FactionIDs.Boethiah),
			new DungeonRegionQuestData("EXPL2304", 23, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Wayrest Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL2307", 23, 19, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Wayrest Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL2308", 23, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Wayrest Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL2311", 23, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Wayrest Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL2601", 26, 24, (int) DaggerfallConnect.DFRegion.DungeonTypes.OrcStronghold,  "Orsinium Orc Strongholds", (int) FactionFile.FactionIDs.Boethiah),
			new DungeonRegionQuestData("EXPL3207", 32, 10, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Northmoor Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL3210", 32, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.HarpyNest,  "Northmoor Harpy Nests", (int) FactionFile.FactionIDs.Hircine),
			new DungeonRegionQuestData("EXPL4006", 40, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Ykalon Natural Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL4009", 40, 10, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Ykalon Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL4011", 40, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Ykalon Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL4012", 40, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.SpiderNest,  "Ykalon Spider Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL4015", 40, 10, (int) DaggerfallConnect.DFRegion.DungeonTypes.BarbarianStronghold,  "Ykalon Barbarian Strongholds", (int) FactionFile.FactionIDs.Sanguine ),
			new DungeonRegionQuestData("EXPL4310", 43, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.HarpyNest,  "Abibon-Gora Harpy Nests", (int) FactionFile.FactionIDs.Hircine),
			new DungeonRegionQuestData("EXPL4810", 48, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.HarpyNest,  "Tigonus Harpy Nests", (int) FactionFile.FactionIDs.Hircine),
			new DungeonRegionQuestData("EXPL4817", 48, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.ScorpionNest,  "Tigonus Scorpion Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL5100", 51, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Totambu Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL5101", 51, 21, (int) DaggerfallConnect.DFRegion.DungeonTypes.OrcStronghold,  "Totambu Orc Strongholds", (int) FactionFile.FactionIDs.Boethiah),
			new DungeonRegionQuestData("EXPL5102", 51, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.HumanStronghold,  "Totambu Human Strongholds", (int) FactionFile.FactionIDs.Clavicus_Vile),
			new DungeonRegionQuestData("EXPL5104", 51, 24, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Totambu Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL5107", 51, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Totambu Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL5108", 51, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Totambu Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL5109", 51, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Totambu Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL5111", 51, 18, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Totambu Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL5112", 51, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.SpiderNest,  "Totambu Spider Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL5113", 51, 19, (int) DaggerfallConnect.DFRegion.DungeonTypes.GiantStronghold,  "Totambu Giant StrongHolds", (int) FactionFile.FactionIDs.Molag_Bal),
			new DungeonRegionQuestData("EXPL5117", 51, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.ScorpionNest,  "Totambu Scorpion Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL6000", 60, 16, (int) DaggerfallConnect.DFRegion.DungeonTypes.Crypt,  "Ilessan Hills Crypts", (int) FactionFile.FactionIDs.Vaernima),
			new DungeonRegionQuestData("EXPL6002", 60, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.HumanStronghold,  "Ilessan Hills Human Strongholds", (int) FactionFile.FactionIDs.Clavicus_Vile),
			new DungeonRegionQuestData("EXPL6003", 60, 13, (int) DaggerfallConnect.DFRegion.DungeonTypes.Prison,  "Ilessan Hills Prisons", (int) FactionFile.FactionIDs.Mehrunes_Dagon),
			new DungeonRegionQuestData("EXPL6004", 60, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.DesecratedTemple,  "Ilessan Hills Desecrated Temples", (int) FactionFile.FactionIDs.Azura),
			new DungeonRegionQuestData("EXPL6006", 60, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.NaturalCave,  "Ilessan Hills Natural Caves", (int) FactionFile.FactionIDs.Meridia),
			new DungeonRegionQuestData("EXPL6007", 60, 18, (int) DaggerfallConnect.DFRegion.DungeonTypes.Coven,  "Ilessan Hills Covens", (int) FactionFile.FactionIDs.Nocturnal),
			new DungeonRegionQuestData("EXPL6008", 60, 11, (int) DaggerfallConnect.DFRegion.DungeonTypes.VampireHaunt,  "Ilessan Hills Vampire Haunts", (int) FactionFile.FactionIDs.Mephala),
			new DungeonRegionQuestData("EXPL6009", 60, 10, (int) DaggerfallConnect.DFRegion.DungeonTypes.Laboratory,  "Ilessan Hills Laboratories", (int) FactionFile.FactionIDs.Hermaeus_Mora),
			new DungeonRegionQuestData("EXPL6011", 60, 18, (int) DaggerfallConnect.DFRegion.DungeonTypes.RuinedCastle,  "Ilessan Hills Ruined Castles", (int) FactionFile.FactionIDs.Namira),
			new DungeonRegionQuestData("EXPL6012", 60, 18, (int) DaggerfallConnect.DFRegion.DungeonTypes.SpiderNest,  "Ilessan Hills Spider Nests", (int) FactionFile.FactionIDs.Malacath),
			new DungeonRegionQuestData("EXPL6013", 60, 14, (int) DaggerfallConnect.DFRegion.DungeonTypes.GiantStronghold,  "Ilessan Hills Giant StrongHolds", (int) FactionFile.FactionIDs.Molag_Bal),
			new DungeonRegionQuestData("EXPL6015", 60, 12, (int) DaggerfallConnect.DFRegion.DungeonTypes.BarbarianStronghold,  "Ilessan Hills Barbarian Strongholds", (int) FactionFile.FactionIDs.Sanguine ),
		};
	
		
//--------------------------------------------- mod startup ---------------------------------------------
		[Invoke(StateManager.StateTypes.Start, 0)]
		public static void Init(InitParams initParams)
		{
			mod = initParams.Mod;
			var go = new GameObject(mod.Title);
			instance = go.AddComponent<DungeonFinder>();
			mod.SaveDataInterface = instance;

		}

		void Awake()
		{
			//Debug.Log("Begin mod init: DungeonFinder XX");
			InitMod();
			mod.IsReady = true;
			Debug.Log("Finished mod init: DungeonFinder");
		}

		public static void InitMod()
		{
			if (!QuestListsManager.RegisterQuestList("DungeonFinder"))
				throw new Exception("Quest list name is already in use, unable to register DungeonFinder quest list.");

			QuestMachine questMachine = GameManager.Instance.QuestMachine;
			QuestMachine.OnQuestEnded += QuestMachine_OnQuestEnded;
			PlayerGPS.OnEnterLocationRect += PlayerGPS_OnEnterLocationRect;
			//PlayerGPS.OnMapPixelChanged += PlayerGPS_OnMapPixelChanged;
            //ConsoleCommandsDatabase.RegisterCommand(ValidateQuests.name, ValidateQuests.description, ValidateQuests.usage, ValidateQuests.Execute);

			
			InitQuestStatus();
		}
		

//--------------------------------------------- on enter override ---------------------------------------------
		
		private static void PlayerGPS_OnEnterLocationRect(DFLocation location)
		{
            if (QuestMachine.Instance.QuestCount == 0)
            {
				DaggerfallUI.AddHUDText("No Active Quests");
            }
			if(!activeQuest){
				if (location.HasDungeon){
					bool newDung = false;					
						if (location.RegionIndex == 0){
							//Debug.Log(" Alik'r ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q0000)) newDung = true;			// ( x )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0004)) newDung = true;	// ( x )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0006)) newDung = true;	// (  )	
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0011)) newDung = true;	// (  )	
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0017)) newDung = true;	// (  )	17
						} else if (location.RegionIndex == 1){
							// Debug.Log(" Dragontail Mountains ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q0100)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0101)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0102)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0106)) newDung = true;	// (  )	
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0116)) newDung = true;	// ( x )
						} else if (location.RegionIndex == 5){
							// Debug.Log(" Dwynnen ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q0500)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0503)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0504)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0508)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0509)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q0511)) newDung = true;	// (  )
						} else if (location.RegionIndex == 11){
							// Debug.Log(" Dak'fron ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q1100)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1106)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1114)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1117)) newDung = true;	// (  )
						} else if (location.RegionIndex == 16){		
							// Debug.Log(" Wrothgarian ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q1600)) newDung = true;			// ( x )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1601)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1606)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1608)) newDung = true;	// ( x )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1609)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1613)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1615)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1616)) newDung = true;	// ( x )
						} else if (location.RegionIndex == 17){	
							//Debug.Log(" Daggerfall ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q1700)) newDung = true;			// ( x )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1703)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1707)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1708)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1709)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1711)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1713)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1714)) newDung = true;	// ( x )
						} else if (location.RegionIndex == 18){	
							// Debug.Log(" Glenpoint ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q1800)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1804)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1807)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1808)) newDung = true;	// ( x )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q1809)) newDung = true;	// (  )
						} else if (location.RegionIndex == 20){		
							// Debug.Log(" Sentinel ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q2006)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2007)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2011)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2017)) newDung = true;	// (  )
						} else if (location.RegionIndex == 21){		
							// Debug.Log(" Anticlere ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q2100)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2102)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2104)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2106)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2107)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2108)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2111)) newDung = true;	// (  )
						} else if (location.RegionIndex == 23){		
							// Debug.Log(" Wayrest ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q2301)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2304)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2307)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2308)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q2311)) newDung = true;	// (  )
						} else if (location.RegionIndex == 26){		
							// Debug.Log(" Orsinum ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q2601)) newDung = true;			// (  )
						} else if (location.RegionIndex == 32){							
							// Debug.Log(" Northmoor ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q3207)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q3207)) newDung = true;	// (  )
						} else if (location.RegionIndex == 40){					
							// Debug.Log(" Ykalon ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q4006)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q4009)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q4011)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q4012)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q4015)) newDung = true;	// (  )
						} else if (location.RegionIndex == 43){					
							// Debug.Log(" Abibon-Gora ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q4310)) newDung = true;			// (  )
						} else if (location.RegionIndex == 48){					
							// Debug.Log(" Tigonus ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q4810)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q4817)) newDung = true;	// (  )
						} else if (location.RegionIndex == 51){					
							// Debug.Log(" Totambu ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q5100)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5101)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5102)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5104)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5107)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5108)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5109)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5111)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5112)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5113)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q5117)) newDung = true;	// (  )
						} else if (location.RegionIndex == 60){		
							// Debug.Log(" Ilessan Hills ");
							if (CheckDungeonRegion(location, (int) RegionQuests.Q6000)) newDung = true;			// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6002)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6003)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6004)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6006)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6007)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6008)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6009)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6011)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6012)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6013)) newDung = true;	// (  )
							else if (CheckDungeonRegion(location, (int) RegionQuests.Q6015)) newDung = true;	// (  )
						}
					
				}
			}
			CheckDaedricShrine(location);		
		}	
		
		private static bool CheckDungeonRegion(DFLocation location, int drQuestId)
		{
			questData = dungeonRegionQuests[drQuestId];
			Debug.Log(" Checking " + questData.dungeonName + " ");
			//int dungeonType = (int) location.MapTableData.DungeonType;
			if (location.MapTableData.DungeonType == (DaggerfallConnect.DFRegion.DungeonTypes)questData.dungeon){				
				// Dungeon must not be involved in any other quests	
				SiteDetails[] activeQuestSites = QuestMachine.Instance.GetAllActiveQuestSites();
				if ( !IsDungeonAssigned(activeQuestSites, location.MapTableData.MapId)){					
				
					if (UpdateDungeonQuestStatus(drQuestId)){
						Debug.Log(" Starting " + questData.dungeonName + " ");
						StartDungeonMapperQuestline(location, questData.dungeonName, questData.quest, drQuestId, guildFactionId);
						return true;	//
					}
				}
			}
			return false;
		}
		
		public static void StartDungeonMapperQuestline(DFLocation location, String dungeontype, String quest, int drQuestId, int faction)
		{
			 try
            {
				Quest q1 = GameManager.Instance.QuestListsManager.GetQuest(quest, faction);
				QuestMachine.Instance.StartQuest(q1);
				DaggerfallUI.AddHUDText("Started " + dungeontype);
				activeQuest = true;	//
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat(" Parsing Quest " + quest + " FAILED! ", quest, ex.Message);
				SetQuestStatus(drQuestId, 0);
				activeQuest = false;	//
				
            }
		}
	
		
		private static bool UpdateDungeonQuestStatus(int drQuestId)
		{
			if (GetQuestStatus(drQuestId) == 0){
				SetQuestStatus(drQuestId, 1);
				return true;	//
			}			
			return false;
		}
		
		public static int GetQuestStatus(int drQuestId){
			if (drQuestId < DRQuestStatus.Count){
				return DRQuestStatus[drQuestId];
			}
			else return 0;
		}
		
		public static void SetQuestStatus(int drQuestId, int state){
			if (drQuestId < DRQuestStatus.Count){
				 DRQuestStatus[drQuestId] = state;
			}
		}

		public static void CheckDaedricShrine(DFLocation location)
		{
			if (location.MapTableData.LocationType == DaggerfallConnect.DFRegion.LocationTypes.ReligionCult ){
				Debug.Log(" Arrived at a cult ");
				for (int i = 0; i < 16; i++){
					if (DRDaedraStatus[i] == 3){
						DaedraQuestData daedricQuest = daedraQuestData[i];						
						Quest q1 = GameManager.Instance.QuestListsManager.GetQuest(daedricQuest.quest, guildFactionId);
						QuestMachine.Instance.StartQuest(q1);
						DaggerfallUI.AddHUDText("Started " + daedricQuest.daedraName + " Summoning");
						DRDaedraStatus[i] = 2;		
						activeQuest = true;	//			
					}	
				}
				CheckDaedraProgress();
			}
		}
		


        public static bool IsDungeonAssigned(SiteDetails[] activeQuestSites, int mapId)
        {
            // Check quest dungeon sites already active in quest machine
            if (activeQuestSites != null && activeQuestSites.Length > 0)
            {
                foreach (SiteDetails site in activeQuestSites)
                {
                    if (site.siteType == SiteTypes.Dungeon && site.mapId == mapId)
                        return true;
                }
            }
            return false;
        }
		
//--------------------------------------------- on quest ended override ---------------------------------------------
		
		public static void QuestMachine_OnQuestEnded(Quest quest)
		{	
			if (quest.QuestName.StartsWith("EXPL")){
				if(activeQuest){	// -------------- quest succeeded --------------------
					if (quest.QuestSuccess)	{
						QuestCompletion(quest, 4);		
						CheckQuestlineCompletion(quest);	
					} else {		// -------------- quest failed or canceled --------------------
						Debug.Log(" Failed " + quest.DisplayName + " with " + quest.FactionId + " ");	
						QuestCompletion(quest, 0);	
					}
				}
			}
			else if (quest.QuestName.EndsWith("Y000")){	
					if (quest.QuestSuccess)	{// -------------- quest succeeded --------------------
						ActivateSummoningQuest(quest);
					} else {		// -------------- quest failed or canceled --------------------
						Debug.Log(" Quit " + quest.DisplayName + " with " + quest.FactionId + " ");
						ResetSummoningQuest(quest);
					}
			}else {
					if (!quest.QuestSuccess)	{// -------------- quest succeeded --------------------
						Debug.Log("Failed " + quest.DisplayName + " with " + quest.FactionId);
					}				
			}
			
			
		}

		public static void QuestCompletion(Quest quest, int state){		
			int i = 0;
			bool match = false; 
			while (!match && i < QuestDataCount){
				match = SetQuestStatus(quest, i, state);
				if(match)								
					activeQuest = false;	
				i++;							
			}	
		}		
		
		public static bool SetQuestStatus(Quest quest, int drQuestId, int state){			
			if (quest.QuestName == dungeonRegionQuests[drQuestId].quest){
				Debug.Log(" Set quest(" + drQuestId + ") " + quest.DisplayName + " to " + state + " ");
				SetQuestStatus(drQuestId, state);
				return true;	//						
			} 			
			return false;	
		}		

		public static void CheckQuestlineCompletion(Quest quest){	
				CheckDaedraProgress();
		}
		
		public static void CheckDaedraProgress(){
			int[] deadraScore = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			for (int i = 0; i < QuestDataCount; i++){
				if (DRQuestStatus[i] == 4){
					questData = dungeonRegionQuests[i];
					int daedraIndex = DaedraFactionConversion[questData.daedraId];
					deadraScore[daedraIndex] += questData.score;
								
				}				
			}	
			for (int i = 0; i < 16; i++){				
				if (DRDaedraStatus[i] == 1 && deadraScore[i] >=  daedraQuestData[i].scoreReq){		
					DaedraQuestData daedricQuest = daedraQuestData[i];
					Quest q1 = GameManager.Instance.QuestListsManager.GetQuest(daedricQuest.quest, guildFactionId);
					QuestMachine.Instance.StartQuest(q1);
					DaggerfallUI.AddHUDText("Started " + daedricQuest.daedraName + " Summoning");
					DRDaedraStatus[i] = 2;			
				}				
			}	
			
		}
		
//--------------------------------------------- daedric prince summoning ---------------------------------------------
		public static void SummonDaedricPrince(int princeid)
		{
			
			DaggerfallQuestPopupWindow.DaedraData daedraToSummon;			
			daedraToSummon = DaggerfallQuestPopupWindow.daedraData[princeid];
			int finderFactionID = (int) FactionFile.FactionIDs.The_Academics;
			// Offer the quest to player.
			UserInterfaceManager uiManager = DaggerfallUI.Instance.UserInterfaceManager;
			Quest offeredQuest = GameManager.Instance.QuestListsManager.GetQuest(daedraToSummon.quest, finderFactionID);
			if (offeredQuest != null)
			{
				// Close menu and push DaggerfallDaedraSummoningWindow here for video and custom quest offer..
				//CloseWindow();
				uiManager.PushWindow(UIWindowFactory.GetInstanceWithArgs(UIWindowType.DaedraSummoned, new object[] { uiManager, daedraToSummon, offeredQuest }));
			}
		}	
		
		
		public static void ActivateSummoningQuest(Quest quest)
		{
			//new DaedraData((int) FactionFile.FactionIDs.Hircine, "X0C0Y00", 155, "HIRCINE.FLC", Weather.WeatherType.None),  // Restrict to only glenmoril witches?
				if (quest.QuestName == "X0C0Y000"){
					SummonDaedricPrince(0);	DRDaedraStatus[0] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Clavicus_Vile, "V0C0Y00", 1, "CLAVICUS.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "V0C0Y000"){
					SummonDaedricPrince(1);	DRDaedraStatus[1] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Mehrunes_Dagon, "Y0C0Y00", 320, "MEHRUNES.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "Y0C0Y000"){
					SummonDaedricPrince(2);	DRDaedraStatus[2] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Molag_Bal, "20C0Y00", 350, "MOLAGBAL.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "20C0Y000"){
					SummonDaedricPrince(3);	DRDaedraStatus[3] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Sanguine, "70C0Y00", 46, "SANGUINE.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "70C0Y000"){
					SummonDaedricPrince(4);	DRDaedraStatus[4] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Peryite, "50C0Y00", 99, "PERYITE.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "50C0Y000"){
					SummonDaedricPrince(5);	DRDaedraStatus[5] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Malacath, "80C0XY00", 278, "MALACATH.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "80C0Y000"){
					SummonDaedricPrince(6);	DRDaedraStatus[6] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Hermaeus_Mora, "W0C0Y00", 65, "HERMAEUS.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "W0C0Y000"){
					SummonDaedricPrince(7);	DRDaedraStatus[7] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Sheogorath, "60C0Y00", 32, "SHEOGRTH.FLC", Weather.WeatherType.Thunder),
				} else if (quest.QuestName == "60C0Y000"){
					SummonDaedricPrince(8);	DRDaedraStatus[8] = 4; activeQuest = false; 
			//new DaedraData((int) FactionFile.FactionIDs.Boethiah, "U0C0Y00", 302, "BOETHIAH.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "U0C0Y000"){
					SummonDaedricPrince(9);	DRDaedraStatus[9] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Namira, "30C0Y00", 129, "NAMIRA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "30C0Y000"){
					SummonDaedricPrince(10);	DRDaedraStatus[10] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Meridia, "10C0Y00", 13, "MERIDIA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "10C0Y000"){
					SummonDaedricPrince(11);	DRDaedraStatus[11] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Vaernima, "90C0Y00", 190, "VAERNIMA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "90C0Y000"){
					SummonDaedricPrince(12);	DRDaedraStatus[12] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Nocturnal, "40C0Y00", 248, "NOCTURNA.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "40C0Y000"){
					SummonDaedricPrince(13);	DRDaedraStatus[13] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Mephala, "Z0C0Y00", 283, "MEPHALA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "Z0C0Y000"){
					SummonDaedricPrince(14);	DRDaedraStatus[14] = 4; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Azura, "T0C0Y00", 81, "AZURA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "T0C0Y000"){
					SummonDaedricPrince(15);	DRDaedraStatus[15] = 4; activeQuest = false;
				}
			
		}
		

		public static void ResetSummoningQuest(Quest quest)
		{
			//new DaedraData((int) FactionFile.FactionIDs.Hircine, "X0C0Y00", 155, "HIRCINE.FLC", Weather.WeatherType.None),  // Restrict to only glenmoril witches?
				if (quest.QuestName == "X0C0Y000"){
					DRDaedraStatus[0] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Clavicus_Vile, "V0C0Y00", 1, "CLAVICUS.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "V0C0Y000"){
					DRDaedraStatus[1] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Mehrunes_Dagon, "Y0C0Y00", 320, "MEHRUNES.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "Y0C0Y000"){
					DRDaedraStatus[2] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Molag_Bal, "20C0Y00", 350, "MOLAGBAL.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "20C0Y000"){
					DRDaedraStatus[3] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Sanguine, "70C0Y00", 46, "SANGUINE.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "70C0Y000"){
					DRDaedraStatus[4] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Peryite, "50C0Y00", 99, "PERYITE.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "50C0Y000"){
					DRDaedraStatus[5] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Malacath, "80C0XY00", 278, "MALACATH.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "80C0Y000"){
					DRDaedraStatus[6] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Hermaeus_Mora, "W0C0Y00", 65, "HERMAEUS.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "W0C0Y000"){
					DRDaedraStatus[7] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Sheogorath, "60C0Y00", 32, "SHEOGRTH.FLC", Weather.WeatherType.Thunder),
				} else if (quest.QuestName == "60C0Y000"){
					DRDaedraStatus[8] = 3; activeQuest = false; 
			//new DaedraData((int) FactionFile.FactionIDs.Boethiah, "U0C0Y00", 302, "BOETHIAH.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "U0C0Y000"){
					DRDaedraStatus[9] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Namira, "30C0Y00", 129, "NAMIRA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "30C0Y000"){
					DRDaedraStatus[10] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Meridia, "10C0Y00", 13, "MERIDIA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "10C0Y000"){
					DRDaedraStatus[11] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Vaernima, "90C0Y00", 190, "VAERNIMA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "90C0Y000"){
					DRDaedraStatus[12] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Nocturnal, "40C0Y00", 248, "NOCTURNA.FLC", Weather.WeatherType.Rain),
				} else if (quest.QuestName == "40C0Y000"){
					DRDaedraStatus[13] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Mephala, "Z0C0Y00", 283, "MEPHALA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "Z0C0Y000"){
					DRDaedraStatus[14] = 3; activeQuest = false;
			//new DaedraData((int) FactionFile.FactionIDs.Azura, "T0C0Y00", 81, "AZURA.FLC", Weather.WeatherType.None),
				} else if (quest.QuestName == "T0C0Y000"){
					DRDaedraStatus[15] = 3; activeQuest = false;
				}
			
		}

//--------------------------------------------- misc ---------------------------------------------
		public static int CalculateMaxBankLoan()
		{
			int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
			int legalRep = GameManager.Instance.PlayerEntity.RegionData[regionIndex].LegalRep;
			if (legalRep < 0)
				return 0;
			else
				return legalRep * 10000;
			
			//return GameManager.Instance.PlayerEntity.Level * 10000;
		}
		
	     private static class ValidateQuests
        {
            public static readonly string name = "validatequests";
            public static readonly string error = "unable to validate";
            public static readonly string usage = "validatequests";
            public static readonly string description = "validate quests";

            public static string Execute(params string[] args)
            {
				ValidateQuestList();
                return "quests validated";
            }
        }
			
		public static void ValidateQuestList()
		{
				for (int i = 0; i < 91; i++){
					questData = dungeonRegionQuests[i];
					string quest = questData.quest;
					try 
					{
						Quest q1 = GameManager.Instance.QuestListsManager.GetQuest(quest, 60);
					}
					catch (Exception ex) 
					{
						Debug.LogErrorFormat(" Parsing Quest " + quest + " FAILED! ", quest, ex.Message);						
					}
				}

		}
	}
}