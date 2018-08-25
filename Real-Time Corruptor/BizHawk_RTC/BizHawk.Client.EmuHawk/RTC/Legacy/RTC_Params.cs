﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;

namespace RTC
{
	[Serializable]
	public class RTC_Params
	{
		List<object> objectList = new List<object>();
		List<Type> typeList = new List<Type>();

		//This is an array of pointers for setting/getting variables upon parameter synchronization accross processes
		//The index of refs must match between processes otherwise it will break
		//(EmuHawk.exe and StandaloneRTC.exe must match versions)

		Ref[] refs = {
			new Ref(() => RTC_CorruptCore.SelectedEngine, x => { RTC_CorruptCore.SelectedEngine = (CorruptionEngine)x; }),
			new Ref(() => RTC_CorruptCore.CustomPrecision, x => { RTC_CorruptCore.CustomPrecision = (int)x; }),
			new Ref(() => RTC_CorruptCore.Intensity, x => { RTC_CorruptCore.Intensity = (int)x; }),
			new Ref(() => RTC_CorruptCore.ErrorDelay, x => { RTC_CorruptCore.ErrorDelay = (int)x; }),
			new Ref(() => RTC_CorruptCore.Radius, x => { RTC_CorruptCore.Radius = (BlastRadius)x; }),

			new Ref(() => RTC_Core.ClearStepActionsOnRewind, x => { RTC_Core.ClearStepActionsOnRewind = (bool)x; }),
			new Ref(() => RTC_StepActions.MaxInfiniteBlastUnits, x => { RTC_StepActions.MaxInfiniteBlastUnits   = (int)x; }),
			new Ref(() => RTC_StepActions.LockExecution, x => { RTC_StepActions.LockExecution = (bool)x; }),

			new Ref(() => RTC_Core.ExtractBlastLayer, x => { RTC_Core.ExtractBlastLayer = (bool)x; }),
			new Ref(() => RTC_Core.lastOpenRom, x => { RTC_Core.lastOpenRom = (string)x; }),
			new Ref(() => RTC_Core.lastLoaderRom, x => { RTC_Core.lastLoaderRom = (int)x; }),
			new Ref(() => RTC_CorruptCore.AutoCorrupt, x => { RTC_CorruptCore.AutoCorrupt = (bool)x; }),

			new Ref(() => RTC_Core.BizhawkOsdDisabled, x => { RTC_Core.BizhawkOsdDisabled = (bool)x; }),
			new Ref(() => RTC_Hooks.ShowConsole, x => { RTC_Hooks.ShowConsole = (bool)x; }),
			new Ref(() => RTC_Core.DontCleanSavestatesOnQuit, x => { RTC_Core.DontCleanSavestatesOnQuit = (bool)x; }),


			new Ref(() => RTC_NightmareEngine.MinValue8Bit, x => { RTC_NightmareEngine.MinValue8Bit = (long)x; }),
			new Ref(() => RTC_NightmareEngine.MaxValue8Bit, x => { RTC_NightmareEngine.MaxValue8Bit = (long)x; }),
			new Ref(() => RTC_HellgenieEngine.MinValue8Bit, x => { RTC_HellgenieEngine.MinValue8Bit = (long)x; }),
			new Ref(() => RTC_HellgenieEngine.MaxValue8Bit, x => { RTC_HellgenieEngine.MaxValue8Bit = (long)x; }),
			new Ref(() => RTC_NightmareEngine.MinValue8Bit, x => { RTC_NightmareEngine.MinValue8Bit = (long)x; }),

			new Ref(() => RTC_NightmareEngine.MaxValue16Bit, x => { RTC_NightmareEngine.MaxValue16Bit = (long)x; }),
			new Ref(() => RTC_HellgenieEngine.MinValue16Bit, x => { RTC_HellgenieEngine.MinValue16Bit = (long)x; }),
			new Ref(() => RTC_HellgenieEngine.MaxValue16Bit, x => { RTC_HellgenieEngine.MaxValue16Bit = (long)x; }),
			new Ref(() => RTC_NightmareEngine.MinValue16Bit, x => { RTC_NightmareEngine.MinValue16Bit = (long)x; }),

			new Ref(() => RTC_NightmareEngine.MaxValue32Bit, x => { RTC_NightmareEngine.MaxValue32Bit = (long)x; }),
			new Ref(() => RTC_HellgenieEngine.MinValue32Bit, x => { RTC_HellgenieEngine.MinValue32Bit = (long)x; }),
			new Ref(() => RTC_HellgenieEngine.MaxValue32Bit, x => { RTC_HellgenieEngine.MaxValue32Bit = (long)x; }),
			new Ref(() => RTC_NightmareEngine.MinValue32Bit, x => { RTC_NightmareEngine.MinValue32Bit = (long)x; }),


			new Ref(() => RTC_CustomEngine.Delay,           x => { RTC_CustomEngine.Delay           = (int)x; }),
			new Ref(() => RTC_CustomEngine.Lifetime,        x => { RTC_CustomEngine.Lifetime        = (int)x; }),
			new Ref(() => RTC_CustomEngine.LimiterListHash,     x => { RTC_CustomEngine.LimiterListHash     = (string)x; }),
			new Ref(() => RTC_CustomEngine.LimiterTime,     x => { RTC_CustomEngine.LimiterTime		= (ActionTime)x; }),
			new Ref(() => RTC_CustomEngine.LimiterInverted,     x => { RTC_CustomEngine.LimiterInverted    = (bool)x; }),
			new Ref(() => RTC_CustomEngine.Loop,			x => { RTC_CustomEngine.Loop			= (bool)x; }),
			new Ref(() => RTC_CustomEngine.MinValue8Bit,    x => { RTC_CustomEngine.MinValue8Bit    = (long)x; }),
			new Ref(() => RTC_CustomEngine.MinValue16Bit,   x => { RTC_CustomEngine.MinValue16Bit   = (long)x; }),
			new Ref(() => RTC_CustomEngine.MinValue32Bit,   x => { RTC_CustomEngine.MinValue32Bit   = (long)x; }),
			new Ref(() => RTC_CustomEngine.MaxValue8Bit,    x => { RTC_CustomEngine.MaxValue8Bit    = (long)x; }),
			new Ref(() => RTC_CustomEngine.MaxValue16Bit,   x => { RTC_CustomEngine.MaxValue16Bit   = (long)x; }),
			new Ref(() => RTC_CustomEngine.MaxValue32Bit,   x => { RTC_CustomEngine.MaxValue32Bit   = (long)x; }),
			new Ref(() => RTC_CustomEngine.Source,			x => { RTC_CustomEngine.Source			= (BlastUnitSource)x; }),
			new Ref(() => RTC_CustomEngine.StoreAddress,    x => { RTC_CustomEngine.StoreAddress    = (CustomStoreAddress)x; }),
			new Ref(() => RTC_CustomEngine.StoreTime,		x => { RTC_CustomEngine.StoreTime		= (ActionTime)x; }),
			new Ref(() => RTC_CustomEngine.StoreType,		x => { RTC_CustomEngine.StoreType		= (StoreType)x; }),
			new Ref(() => RTC_CustomEngine.TiltValue,		x => { RTC_CustomEngine.TiltValue		= (BigInteger)x; }),
			new Ref(() => RTC_CustomEngine.ValueListHash,       x => { RTC_CustomEngine.ValueListHash       = (string)x; }),
			new Ref(() => RTC_CustomEngine.ValueSource,     x => { RTC_CustomEngine.ValueSource     = (CustomValueSource)x; }),
			

			new Ref(() => RTC_Filtering.Hash2LimiterDico,   x => { RTC_Filtering.Hash2LimiterDico   = (SerializableDico<string, string[]>)x; }),
			new Ref(() => RTC_Filtering.Hash2ValueDico,     x => { RTC_Filtering.Hash2ValueDico     = (SerializableDico<string, string[]>)x; }),



			new Ref(() => RTC_VectorEngine.LimiterListHash, x => { RTC_VectorEngine.LimiterListHash = (string)x; }),
			new Ref(() => RTC_VectorEngine.ValueListHash, x => { RTC_VectorEngine.ValueListHash = (string)x; }),

			new Ref(() => RTC_StockpileManager.currentSavestateKey, x => { RTC_StockpileManager.currentSavestateKey = (string)x; }),
			new Ref(() => RTC_StockpileManager.currentGameSystem, x => { RTC_StockpileManager.currentGameSystem = (string)x; }),
			new Ref(() => RTC_StockpileManager.currentGameName, x => { RTC_StockpileManager.currentGameName = (string)x; }),
			new Ref(() => RTC_StockpileManager.backupedState, x => { RTC_StockpileManager.backupedState = (StashKey)x; }),

			new Ref(() => RTC_MemoryDomains.SelectedDomains, x => { RTC_MemoryDomains.SelectedDomains = (string[])x; }),
			new Ref(() => RTC_MemoryDomains.lastSelectedDomains, x => { RTC_MemoryDomains.lastSelectedDomains = (string[])x; }),
		};

		public RTC_Params()
		{
			//Fills the Params object upon creation
			GetSetLiveParams(true);
		}

		public void Deploy()
		{
			//Has to be manually deployed after received

			GetSetLiveParams(false);

			if (RTC_StockpileManager.backupedState != null)
				RTC_StockpileManager.backupedState.Run();
			else
			{
				RTC_CorruptCore.AutoCorrupt = false;
			}
		}

		private void GetSetLiveParams(bool buildObject)
		{
			//Builds the params object or unwraps the params object from/back to all monitored variables

			for (int i = 0; i < refs.Length; i++)
				if (buildObject)
					objectList.Add(refs[i].Value);
				else
					refs[i].Value = objectList[i];
		}

		public static void LoadRTCColor()
		{
			if (NetCoreImplementation.isStandalone || !NetCoreImplementation.isRemoteRTC)
			{
				if (IsParamSet("COLOR"))
				{
					string[] bytes = ReadParam("COLOR").Split(',');
					RTC_Core.generalColor = Color.FromArgb(Convert.ToByte(bytes[0]), Convert.ToByte(bytes[1]), Convert.ToByte(bytes[2]));
				}
				else
					RTC_Core.generalColor = Color.FromArgb(110, 150, 193);

				RTC_UICore.SetRTCColor(RTC_Core.generalColor);
			}
		}

		public static void SaveRTCColor(Color color)
		{
			SetParam("COLOR", color.R.ToString() + "," + color.G.ToString() + "," + color.B.ToString());
		}

		public static void LoadHotkeys()
		{
			if (IsParamSet("RTC_HOTKEYS"))
			{
				//		RTC_Hotkeys.LoadHotkeys(ReadParam("RTC_HOTKEYS"));
			}
		}

		public static void SaveHotkeys()
		{
			//	SetParam("RTC_HOTKEYS", RTC_Hotkeys.SaveHotkeys());
		}

		public static void LoadBizhawkWindowState()
		{
			if (IsParamSet("BIZHAWK_SIZE"))
			{
				string[] size = ReadParam("BIZHAWK_SIZE").Split(',');
				RTC_Hooks.BIZHAWK_GETSET_MAINFORMSIZE = new Size(Convert.ToInt32(size[0]), Convert.ToInt32(size[1]));
				string[] location = ReadParam("BIZHAWK_LOCATION").Split(',');
				RTC_Hooks.BIZHAWK_GETSET_MAINFORMLOCATION = new Point(Convert.ToInt32(location[0]), Convert.ToInt32(location[1]));
			}
		}

		public static void SaveBizhawkWindowState()
		{
			var size = RTC_Hooks.BIZHAWK_GETSET_MAINFORMSIZE;
			var location = RTC_Hooks.BIZHAWK_GETSET_MAINFORMLOCATION;

			SetParam("BIZHAWK_SIZE", $"{size.Width},{size.Height}");
			SetParam("BIZHAWK_LOCATION", $"{location.X},{location.Y}");
		}

		public static void AutoLoadVMDs()
		{
			string currentGame = (string)NetCoreImplementation.SendCommandToBizhawk(new RTC_Command(CommandType.REMOTE_KEY_GETGAMENAME), true);
			SetParam((currentGame.GetHashCode().ToString()));
		}

		public static void SetParam(string paramName, string data = null)
		{
			if (data == null)
			{
				if (!IsParamSet(paramName))
					SetParam(paramName, "");
			}
			else
				File.WriteAllText(RTC_Core.paramsDir + "\\" + paramName, data);
		}

		public static void RemoveParam(string paramName)
		{
			if (IsParamSet(paramName))
				File.Delete(RTC_Core.paramsDir + "\\" + paramName);
		}

		public static string ReadParam(string paramName)
		{
			if (IsParamSet(paramName))
				return File.ReadAllText(RTC_Core.paramsDir + "\\" + paramName);

			return null;
		}

		public static bool IsParamSet(string paramName)
		{
			return File.Exists(RTC_Core.paramsDir + "\\" + paramName);
		}
	}

	[Serializable]
	internal class Ref //Serializable pointer object
	{
		private Func<object> getter;
		private Action<object> setter;

		public Ref(Func<object> getter, Action<object> setter)
		{
			this.getter = getter;
			this.setter = setter;
		}

		public object Value
		{
			get => getter();
			set => setter(value);
		}
	}
}