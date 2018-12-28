﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

namespace RTC
{
	public partial class RTC_NetCore
	{
		static volatile Dictionary<string, bool> TransferedRomFilenames = new Dictionary<string, bool>();

		public RTC_Command Process_RTCExtensions(RTC_Command cmd)
		{
			RTC_Command cmdBack = null;

			switch (cmd.Type)
			{
				case CommandType.ASYNCBLAST:
					{
						BlastLayer bl = RTC_Core.Blast(null, RTC_MemoryDomains.SelectedDomains);
						if (bl != null)
							bl.Apply();
					}
					break;

				case CommandType.BLAST:
				{
					BlastLayer bl = null;
					string[] _domains = (string[])cmd.objectValue;

					if (_domains == null)
						_domains = RTC_MemoryDomains.SelectedDomains;

					if (cmd.blastlayer != null)
					{
						cmd.blastlayer.Apply(cmd.isReplay);
					}
					else
					{
						bl = RTC_Core.Blast(null, _domains);
					}

					if (cmd.requestGuid != null)
					{
							cmdBack = new RTC_Command(CommandType.RETURNVALUE)
							{
								objectValue = bl
							};
						}
					}

				break;

				case CommandType.STASHKEY:

					if (!File.Exists(RTC_Core.rtcDir + "\\SKS\\" + cmd.romFilename))
						File.WriteAllBytes(RTC_Core.rtcDir + "\\SKS\\" + cmd.romFilename, cmd.romData);

					cmd.stashkey.RomFilename = RTC_Core.rtcDir + "\\SKS\\" + Path.GetFileName(cmd.romFilename);

					cmd.stashkey.DeployState();

					cmd.stashkey.Run();

					break;

				case CommandType.PULLROM:
					cmdBack = new RTC_Command(CommandType.PUSHROM)
					{
						romFilename = Path.GetFileName(RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM())
					};

					if (!PeerHasRom(cmdBack.romFilename))
						cmdBack.romData = File.ReadAllBytes(RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM());

					break;

				case CommandType.PUSHROM:
					if (cmd.romData != null)
					{
						cmd.romFilename = Path.GetFileName(cmd.romFilename);
						if (!File.Exists(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename))
							File.WriteAllBytes(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename, cmd.romData);
					}

					RTC_Core.LoadRom(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename);
					break;

				case CommandType.PULLSTATE:
					cmdBack = new RTC_Command(CommandType.PUSHSTATE);
					StashKey sk_PULLSTATE = RTC_StockpileManager.SaveState(false);
					cmdBack.stashkey = sk_PULLSTATE;
					sk_PULLSTATE.EmbedState();

					break;

				case CommandType.PUSHSTATE:
					cmd.stashkey.DeployState();
					RTC_StockpileManager.LoadState(cmd.stashkey, true);

					if (S.GET<RTC_Multiplayer_Form>().cbPullStateToGlitchHarvester.Checked)
					{
						StashKey sk_PUSHSTATE = RTC_StockpileManager.SaveState(true, cmd.stashkey);
						sk_PUSHSTATE.RomFilename = RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM();
					}

					break;

				case CommandType.PULLSWAPSTATE:

					cmdBack = new RTC_Command(CommandType.PUSHSWAPSTATE)
					{
						romFilename = Path.GetFileName(RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM())
					};

					if (!PeerHasRom(cmdBack.romFilename))
						cmdBack.romData = File.ReadAllBytes(RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM());

					StashKey sk_PULLSWAPSTATE = RTC_StockpileManager.SaveState(false);
					cmdBack.stashkey = sk_PULLSWAPSTATE;
					sk_PULLSWAPSTATE.EmbedState();

					cmd.romFilename = Path.GetFileName(cmd.romFilename);

					if (!File.Exists(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename))
						File.WriteAllBytes(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename, cmd.romData);
					RTC_Core.LoadRom(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename);

					cmd.stashkey.DeployState();
					RTC_StockpileManager.LoadState(cmd.stashkey, false);

					if (S.GET<RTC_Multiplayer_Form>().GameOfSwapTimer != null)
						S.GET<RTC_Multiplayer_Form>().GameOfSwapCounter = 64;

					break;

				case CommandType.PUSHSWAPSTATE:

					cmd.romFilename = Path.GetFileName(cmd.romFilename);

					if (cmd.romData != null)
						if (!File.Exists(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename))
							File.WriteAllBytes(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename, cmd.romData);

					RTC_Core.LoadRom(RTC_Core.rtcDir + "\\MP\\" + cmd.romFilename);

					cmd.stashkey.DeployState();
					RTC_StockpileManager.LoadState(cmd.stashkey, false);

					if (S.GET<RTC_Multiplayer_Form>().GameOfSwapTimer != null)
						S.GET<RTC_Multiplayer_Form>().GameOfSwapCounter = 64;

					break;
				case CommandType.PULLSCREEN:
					cmdBack = new RTC_Command(CommandType.PUSHSCREEN)
					{
						screen = RTC_Hooks.BIZHAWK_GET_SCREENSHOT()
					};
					break;

				case CommandType.REQUESTSTREAM:
					S.GET<RTC_Multiplayer_Form>().cbStreamScreenToPeer.Checked = true;
					break;

				case CommandType.PUSHSCREEN:
					UpdatePeerScreen(cmd.screen);
					break;

				case CommandType.GAMEOFSWAPSTART:
					S.GET<RTC_Multiplayer_Form>().StartGameOfSwap(false);
					break;

				case CommandType.GAMEOFSWAPSTOP:
					S.GET<RTC_Multiplayer_Form>().StopGameOfSwap(true);
					break;

				case CommandType.REMOTE_PUSHPARAMS:
					(cmd.objectValue as RTC_Params).Deploy();
					break;

				case CommandType.REMOTE_PUSHVMDS:
					RTC_MemoryDomains.VmdPool.Clear();
					foreach (var proto in (cmd.objectValue as VmdPrototype[]))
						RTC_MemoryDomains.AddVMD(proto);
					break;

				case CommandType.BLASTGENERATOR_BLAST:
				{
					List<BlastGeneratorProto> returnList;

					List<BlastGeneratorProto> blastGeneratorProtos =
						(List<BlastGeneratorProto>)(cmd.objectValue as object);
					StashKey _sk = (StashKey)(cmd.stashkey as object);

					returnList = RTC_BlastTools.GenerateBlastLayersFromBlastGeneratorProtos(blastGeneratorProtos, _sk);

					if (cmd.requestGuid != null)
					{
							cmdBack = new RTC_Command(CommandType.RETURNVALUE)
							{
								objectValue = returnList
							};
						}

						break;
				}

				case CommandType.REMOTE_LOADROM:
					RTC_Core.LoadRom_NET(cmd.romFilename);
					break;
				case CommandType.REMOTE_LOADSTATE:
					{
						StashKey sk = (StashKey)(cmd.objectValue as object[])[0];
						bool reloadRom = (bool)(cmd.objectValue as object[])[1];
						bool runBlastLayer = (bool)(cmd.objectValue as object[])[2];

						bool returnValue = RTC_StockpileManager.LoadState_NET(sk, reloadRom);

						RTC_MemoryDomains.RefreshDomains(false);

						if (runBlastLayer)
							RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.BLAST) { blastlayer = sk.BlastLayer, isReplay = true });

						cmdBack = new RTC_Command(CommandType.RETURNVALUE)
						{
							objectValue = returnValue
						};
					}
					break;

				case CommandType.REMOTE_MERGECONFIG:
					Stockpile.MergeBizhawkConfig_NET();
					break;

				case CommandType.REMOTE_IMPORTKEYBINDS:
					Stockpile.ImportBizhawkKeybinds_NET();
					break;

				case CommandType.REMOTE_SAVESTATE:
					{
						StashKey sk = RTC_StockpileManager.SaveState_NET((bool)(cmd.objectValue as object[])[0], (StashKey)(cmd.objectValue as object[])[1]);
						if (cmd.requestGuid != null)
						{
							cmdBack = new RTC_Command(CommandType.RETURNVALUE)
							{
								objectValue = sk
							};
						}
					}
					break;

				case CommandType.REMOTE_BACKUPKEY_REQUEST:
					{
						if (!RTC_Hooks.isNormalAdvance)
							break;

						cmdBack = new RTC_Command(CommandType.REMOTE_BACKUPKEY_STASH);

						bool multiThread = false;

						// apparently multithread savestates doesn't work well right now.
						// We can try again in a future version of bizhawk
						/*
                        if (new string[] {
                             "SNES", "GB", "GBC", "GBA",
                        }.Contains(Global.Game.System.ToString().ToUpper()))
                            multiThread = false;
                        */

						cmdBack.objectValue = RTC_StockpileManager.SaveState_NET(false, null, multiThread);
						break;
					}

				case CommandType.REMOTE_BACKUPKEY_STASH:
					RTC_StockpileManager.BackupedState = (StashKey)cmd.objectValue;
					RTC_StockpileManager.AllBackupStates.Push((StashKey)cmd.objectValue);
					S.GET<RTC_Core_Form>().btnGpJumpBack.Visible = true;
					S.GET<RTC_Core_Form>().btnGpJumpNow.Visible = true;
					break;

				case CommandType.REMOTE_DOMAIN_PEEKBYTE:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_MemoryDomains.GetInterface((string)(cmd.objectValue as object[])[0]).PeekByte((long)(cmd.objectValue as object[])[1])
					};
					break;

				case CommandType.REMOTE_DOMAIN_POKEBYTE:
					RTC_MemoryDomains.GetInterface((string)(cmd.objectValue as object[])[0]).PokeByte((long)(cmd.objectValue as object[])[1], (byte)(cmd.objectValue as object[])[2]);
					break;

				case CommandType.REMOTE_DOMAIN_GETDOMAINS:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_MemoryDomains.GetInterfaces()
					};

					break;

				case CommandType.REMOTE_DOMAIN_VMD_ADD:
					RTC_MemoryDomains.AddVMD((cmd.objectValue as VmdPrototype));
					break;

				case CommandType.REMOTE_DOMAIN_VMD_REMOVE:
					RTC_MemoryDomains.RemoveVMD((cmd.objectValue as string));
					break;

				case CommandType.REMOTE_DOMAIN_ACTIVETABLE_MAKEDUMP:
					RTC_MemoryDomains.GenerateActiveTableDump((string)(cmd.objectValue as object[])[0], (string)(cmd.objectValue as object[])[1]);
					break;

				case CommandType.REMOTE_DOMAIN_SETSELECTEDDOMAINS:
					RTC_MemoryDomains.UpdateSelectedDomains((string[])cmd.objectValue);
					break;

				case CommandType.REMOTE_DOMAIN_SYSTEM:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GET_CURRENTLYLOADEDSYSTEMNAME().ToUpper()
					};
					break;

				case CommandType.REMOTE_DOMAIN_SYSTEMPREFIX:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GET_SAVESTATEPREFIX()
					};
					break;

				case CommandType.REMOTE_KEY_PUSHSAVESTATEDICO:
					{
						var key = (string)(cmd.objectValue as object[])[1];
						var sk = (StashKey)((cmd.objectValue as object[])[0]);
						RTC_StockpileManager.SavestateStashkeyDico[key] = sk;
						S.GET<RTC_GlitchHarvester_Form>().RefreshSavestateTextboxes();
					}
					break;

				case CommandType.REMOTE_KEY_GETSYSTEMNAME:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GET_FILESYSTEMCORENAME()
					};
					break;

				case CommandType.REMOTE_KEY_GETSYSTEMCORE:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GET_SYSTEMCORENAME((string)cmd.objectValue)
					};
					break;

				case CommandType.REMOTE_KEY_GETGAMENAME:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GET_FILESYSTEMGAMENAME()
					};
					break;

				case CommandType.REMOTE_KEY_GETSYNCSETTINGS:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GETSET_SYNCSETTINGS
					};
					break;

				case CommandType.REMOTE_KEY_PUTSYNCSETTINGS:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE);
					break;

				case CommandType.REMOTE_KEY_GETOPENROMFILENAME:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM()
					};
					break;

				case CommandType.REMOTE_KEY_GETRAWBLASTLAYER:
					cmdBack = new RTC_Command(CommandType.RETURNVALUE)
					{
						objectValue = RTC_StockpileManager.GetRawBlastlayer()
					};
					break;
				case CommandType.REMOTE_KEY_GETBAKEDLAYER:
				{
					//We need a stashkey to load the game 
					BlastLayer _bl = cmd.blastlayer;
					var sk = cmd.stashkey;
						cmdBack = new RTC_Command(CommandType.RETURNVALUE)
						{
							objectValue = RTC_BlastTools.GetAppliedBackupLayer(_bl, sk)
						};
						break;
				}

				case CommandType.BIZHAWK_SET_OSDDISABLED:
					RTC_Core.BizhawkOsdDisabled = (bool)cmd.objectValue;
					break;

				case CommandType.BIZHAWK_SET_DONT_CLEAN_SAVESTATES_AT_QUIT:
					RTC_Core.DontCleanSavestatesOnQuit = (bool)cmd.objectValue;
					break;

				case CommandType.ENABLE_CONSOLE:
					RTC_Hooks.ShowConsole = (bool)cmd.objectValue;
					break;

				case CommandType.BIZHAWK_OPEN_HEXEDITOR_ADDRESS:
					{
						string domain = (string)(cmd.objectValue as object[])[0];
						long address = (long)(cmd.objectValue as object[])[1];
						
						MemoryDomainProxy mdp = RTC_MemoryDomains.GetProxy(domain, address);
						long realAddress = RTC_MemoryDomains.GetRealAddress(domain, address);

						RTC_Hooks.BIZHAWK_OPEN_HEXEDITOR_ADDRESS(mdp, realAddress);

						break;
					}

				case CommandType.REMOTE_SET_SAVESTATEBOX:
					RTC_StockpileManager.CurrentSavestateKey = (string)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_AUTOCORRUPT:
					RTC_Core.AutoCorrupt = (bool)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_CUSTOMPRECISION:
					RTC_Core.CustomPrecision = (int)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_INTENSITY:
					RTC_Core.Intensity = (int)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_ERRORDELAY:
					RTC_Core.ErrorDelay = (int)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_BLASTRADIUS:
					RTC_Core.Radius = (BlastRadius)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_RESTOREBLASTLAYERBACKUP:
					if (RTC_StockpileManager.LastBlastLayerBackup != null)
						RTC_StockpileManager.LastBlastLayerBackup.Apply(true);
					break;

				case CommandType.REMOTE_SET_NIGHTMARE_TYPE:
					RTC_NightmareEngine.Algo = (NightmareAlgo)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_NIGHTMARE_MINVALUE:
				{
					int precision = (int)(cmd.objectValue as object[])[0];
					long value = (long)(cmd.objectValue as object[])[1];

					switch (precision)
					{
						case 1:
							RTC_NightmareEngine.MinValue8Bit = value;
							break;
						case 2:
							RTC_NightmareEngine.MinValue16Bit = value;
							break;
						case 4:
							RTC_NightmareEngine.MinValue32Bit = value;
							break;
					}
					break;
				}
				case CommandType.REMOTE_SET_NIGHTMARE_MAXVALUE:
				{
					int precision = (int)(cmd.objectValue as object[])[0];
					long value = (long)(cmd.objectValue as object[])[1];

					switch (precision)
					{
						case 1:
							RTC_NightmareEngine.MaxValue8Bit = value;
							break;
						case 2:
							RTC_NightmareEngine.MaxValue16Bit = value;
							break;
						case 4:
							RTC_NightmareEngine.MaxValue32Bit = value;
							break;
					}
					break;
				}

				case CommandType.REMOTE_SET_HELLGENIE_MINVALUE:
				{
					int precision = (int)(cmd.objectValue as object[])[0];
					long value = (long)(cmd.objectValue as object[])[1];

					switch (precision)
					{
						case 1:
							RTC_HellgenieEngine.MinValue8Bit = value;
							break;
						case 2:
							RTC_HellgenieEngine.MinValue16Bit = value;
							break;
						case 4:
							RTC_HellgenieEngine.MinValue32Bit = value;
							break;
					}
					break;
				}
				case CommandType.REMOTE_SET_HELLGENIE_MAXVALUE:
				{
					int precision = (int)(cmd.objectValue as object[])[0];
					long value = (long)(cmd.objectValue as object[])[1];

					switch (precision)
					{
						case 1:
							RTC_HellgenieEngine.MaxValue8Bit = value;
							break;
						case 2:
							RTC_HellgenieEngine.MaxValue16Bit = value;
							break;
						case 4:
							RTC_HellgenieEngine.MaxValue32Bit = value;
							break;
					}
					break;
				}

				case CommandType.REMOTE_SET_STEPACTIONS_CLEARREWIND:
					RTC_Core.ClearStepActionsOnRewind = (bool)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_STEPACTIONS_CLEARALLBLASTUNITS:
					RTC_StepActions.ClearStepBlastUnits();
					break;
				case CommandType.REMOTE_SET_STEPACTIONS_REMOVEEXCESSINFINITEUNITS:
					RTC_StepActions.RemoveExcessInfiniteStepUnits();
					break;
				case CommandType.REMOTE_SET_STEPACTIONS_MAXLIFETIMEUNITS:
					RTC_StepActions.MaxInfiniteBlastUnits = (int)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_STEPACTIONS_LOCKEXECUTION:
					RTC_StepActions.LockExecution = (bool)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_STEPACTIONS_RUNBEFORE:
					RTC_StepActions.RunBefore = (bool)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_PIPE_LOCKPIPES:
					RTC_StepActions.LockExecution = (bool)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_ENGINE:
					RTC_Core.SelectedEngine = (CorruptionEngine)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_DISTORTION_DELAY:
					RTC_DistortionEngine.Delay = (int)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_DISTORTION_RESYNC:
					//TODO
					break;

				case CommandType.REMOTE_SET_VECTOR_LIMITER:
					RTC_VectorEngine.LimiterListHash = (string)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_VECTOR_VALUES:
					RTC_VectorEngine.ValueListHash = (string)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_CUSTOM_UNIT_SOURCE:
					RTC_CustomEngine.Source = (BlastUnitSource)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_CUSTOM_VALUE_SOURCE:
					RTC_CustomEngine.ValueSource = (CustomValueSource)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_CUSTOM_STORE_TIME:
					RTC_CustomEngine.StoreTime = (ActionTime)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_CUSTOM_STORE_ADDRESS:
					RTC_CustomEngine.StoreAddress = (CustomStoreAddress)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_CUSTOM_STORE_TYPE:
					RTC_CustomEngine.StoreType = (StoreType)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_CUSTOM_LIFETIME:
					RTC_CustomEngine.Lifetime = (int)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_CUSTOM_DELAY:
					RTC_CustomEngine.Delay = (int)cmd.objectValue;
					break;
					
				case CommandType.REMOTE_SET_CUSTOM_LOOP:
					RTC_CustomEngine.Loop = (bool)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_CUSTOM_LIMITERLIST:
					RTC_CustomEngine.LimiterListHash = (string)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_CUSTOM_LIMITERTIME:
					RTC_CustomEngine.LimiterTime = (ActionTime)cmd.objectValue;
					break;
				case CommandType.REMOTE_SET_CUSTOM_VALUELIST:
					RTC_CustomEngine.ValueListHash = (string)cmd.objectValue;
					break;

				case CommandType.REMOTE_SET_CUSTOM_LIMITERINVERTED:
					RTC_CustomEngine.LimiterInverted = (bool)cmd.objectValue;
					break;


				case CommandType.REMOTE_UPDATE_FILTERING_DICTIONARIES:
					{
						RTC_Filtering.Hash2LimiterDico = (Dictionary<string, HashSet<Byte[]>>)(cmd.objectValue as object[])[0];
						RTC_Filtering.Hash2ValueDico = (Dictionary<string, List<Byte[]>>)(cmd.objectValue as object[])[1];
						break;
					}
				case CommandType.REMOTE_SET_CUSTOM_RANGE_MAXVALUE:
					{
						int precision = (int)(cmd.objectValue as object[])[0];
						long value = (long)(cmd.objectValue as object[])[1];

						switch (precision)
						{
							case 1:
								RTC_CustomEngine.MaxValue8Bit = value;
								break;
							case 2:
								RTC_CustomEngine.MaxValue16Bit = value;
								break;
							case 4:
								RTC_CustomEngine.MaxValue32Bit = value;
								break;
						}
						break;
					}



				case CommandType.REMOTE_EVENT_LOADGAMEDONE_NEWGAME:

					if (RTC_Core.isStandalone && RTC_GameProtection.isRunning)
						RTC_GameProtection.Reset();

					RTC_Core.AutoCorrupt = false;
					//RTC_StockpileManager.isCorruptionApplied = false;
					S.GET<RTC_MemoryDomains_Form>().RefreshDomains();
					S.GET<RTC_MemoryDomains_Form>().SetMemoryDomainsAllButSelectedDomains(RTC_MemoryDomains.GetBlacklistedDomains());
					S.GET<RTC_CorruptionEngine_Form>().UpdateDefaultPrecision();
					break;
				case CommandType.REMOTE_EVENT_LOADGAMEDONE_SAMEGAME:
					//RTC_StockpileManager.isCorruptionApplied = false;
					S.GET<RTC_MemoryDomains_Form>().RefreshDomainsAndKeepSelected();
					S.GET<RTC_CorruptionEngine_Form>().UpdateDefaultPrecision();
					break;

				case CommandType.REMOTE_EVENT_CLOSEBIZHAWK:
					RTC_Hooks.BIZHAWK_MAINFORM_CLOSE();
					break;

				case CommandType.REMOTE_EVENT_SAVEBIZHAWKCONFIG:
					RTC_Hooks.BIZHAWK_MAINFORM_SAVECONFIG();
					break;

				case CommandType.REMOTE_EVENT_BIZHAWKSTARTED:

					S.GET<RTC_Core_Form>().AutoCorrupt &= RTC_StockpileManager.BackupedState != null;

					RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.REMOTE_PUSHPARAMS) { objectValue = new RTC_Params() }, true, true);

					RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.REMOTE_PUSHVMDS) { objectValue = RTC_MemoryDomains.VmdPool.Values.Select(it => (it as VirtualMemoryDomain).Proto).ToArray() }, true, true);

					Thread.Sleep(100);

					if (RTC_StockpileManager.BackupedState != null)
						S.GET<RTC_MemoryDomains_Form>().RefreshDomainsAndKeepSelected(RTC_StockpileManager.BackupedState.SelectedDomains.ToArray());

					if (S.GET<RTC_Core_Form>().cbUseGameProtection.Checked)
						RTC_GameProtection.Start();

					break;

				case CommandType.REMOTE_HOTKEY_MANUALBLAST:
					S.GET<RTC_Core_Form>().btnManualBlast_Click(null, null);
					break;

				case CommandType.REMOTE_HOTKEY_AUTOCORRUPTTOGGLE:
					S.GET<RTC_Core_Form>().btnAutoCorrupt_Click(null, null);
					break;
				case CommandType.REMOTE_HOTKEY_ERRORDELAYDECREASE:
					if (S.GET<RTC_GeneralParameters_Form>().nmErrorDelay.Value > 1)
						S.GET<RTC_GeneralParameters_Form>().nmErrorDelay.Value--;
					break;

				case CommandType.REMOTE_HOTKEY_ERRORDELAYINCREASE:
					if (S.GET<RTC_GeneralParameters_Form>().nmErrorDelay.Value < S.GET<RTC_GeneralParameters_Form>().track_ErrorDelay.Maximum)
						S.GET<RTC_GeneralParameters_Form>().nmErrorDelay.Value++;
					break;

				case CommandType.REMOTE_HOTKEY_INTENSITYDECREASE:
					if (S.GET<RTC_GeneralParameters_Form>().nmIntensity.Value > 1)
						S.GET<RTC_GeneralParameters_Form>().nmIntensity.Value--;
					break;

				case CommandType.REMOTE_HOTKEY_INTENSITYINCREASE:
					if (S.GET<RTC_GeneralParameters_Form>().nmIntensity.Value < S.GET<RTC_GeneralParameters_Form>().track_Intensity.Maximum)
						S.GET<RTC_GeneralParameters_Form>().nmIntensity.Value++;
					break;

				case CommandType.REMOTE_HOTKEY_GHLOADCORRUPT:
					if (!RTC_NetCore.NetCoreCommandSynclock)
					{
						RTC_NetCore.NetCoreCommandSynclock = true;

						S.GET<RTC_GlitchHarvester_Form>().cbAutoLoadState.Checked = true;
						S.GET<RTC_GlitchHarvester_Form>().btnCorrupt_Click(null, null);

						RTC_NetCore.NetCoreCommandSynclock = false;
					}
					break;

				case CommandType.REMOTE_HOTKEY_GHCORRUPT:
					if (!RTC_NetCore.NetCoreCommandSynclock)
					{
						RTC_NetCore.NetCoreCommandSynclock = true;

						bool isload = S.GET<RTC_GlitchHarvester_Form>().cbAutoLoadState.Checked;
						S.GET<RTC_GlitchHarvester_Form>().cbAutoLoadState.Checked = false;
						S.GET<RTC_GlitchHarvester_Form>().btnCorrupt_Click(null, null);
						S.GET<RTC_GlitchHarvester_Form>().cbAutoLoadState.Checked = isload;

						RTC_NetCore.NetCoreCommandSynclock = false;
					}
					break;

				case CommandType.REMOTE_HOTKEY_GHLOAD:
					S.GET<RTC_GlitchHarvester_Form>().btnSaveLoad.Text = "LOAD";
					S.GET<RTC_GlitchHarvester_Form>().btnSaveLoad_Click(null, null);
					break;
				case CommandType.REMOTE_HOTKEY_GHSAVE:
					S.GET<RTC_GlitchHarvester_Form>().btnSaveLoad.Text = "SAVE";
					S.GET<RTC_GlitchHarvester_Form>().btnSaveLoad_Click(null, null);
					break;
				case CommandType.REMOTE_HOTKEY_GHSTASHTOSTOCKPILE:
					S.GET<RTC_GlitchHarvester_Form>().AddStashToStockpile(false);
					break;

				case CommandType.REMOTE_HOTKEY_SENDRAWSTASH:
					S.GET<RTC_GlitchHarvester_Form>().btnSendRaw_Click(null, null);
					break;

				case CommandType.REMOTE_HOTKEY_BLASTRAWSTASH:
					RTC_Core.SendCommandToBizhawk(new RTC_Command(CommandType.ASYNCBLAST));
					S.GET<RTC_GlitchHarvester_Form>().btnSendRaw_Click(null, null);
					break;
				case CommandType.REMOTE_HOTKEY_BLASTLAYERTOGGLE:
					S.GET<RTC_GlitchHarvester_Form>().btnBlastToggle_Click(null, null);
					break;
				case CommandType.REMOTE_HOTKEY_BLASTLAYERREBLAST:

					if (RTC_StockpileManager.CurrentStashkey == null || RTC_StockpileManager.CurrentStashkey.BlastLayer.Layer.Count == 0)
					{
						S.GET<RTC_GlitchHarvester_Form>().IsCorruptionApplied = false;
						break;
					}

					S.GET<RTC_GlitchHarvester_Form>().IsCorruptionApplied = true;
					RTC_Core.SendCommandToRTC(new RTC_Command(CommandType.BLAST) { blastlayer = RTC_StockpileManager.CurrentStashkey.BlastLayer });
					break;

				case CommandType.REMOTE_RENDER_START:
					RTC_Render.StartRender_NET();
					break;

				case CommandType.REMOTE_RENDER_STOP:
					RTC_Render.StopRender_NET();
					break;

				case CommandType.REMOTE_RENDER_SETTYPE:
					RTC_Render.lastType = (RENDERTYPE)cmd.objectValue;
					break;

				case CommandType.REMOTE_RENDER_STARTED:
					S.GET<RTC_GlitchHarvester_Form>().btnRender.Text = "Stop Render";
					S.GET<RTC_GlitchHarvester_Form>().btnRender.ForeColor = Color.GreenYellow;
					break;

				case CommandType.REMOTE_RENDER_RENDERATLOAD:
					RTC_StockpileManager.RenderAtLoad = (bool)cmd.objectValue;
					break;
			}

			return cmdBack;
		}

		public bool PeerHasRom(string RomFilename)
		{
			if (TransferedRomFilenames.ContainsKey(RomFilename))
				return true;

			TransferedRomFilenames.Add(RomFilename, true);
			return false;
		}

		public void SwapGameState()
		{
			if (side == NetworkSide.DISCONNECTED)
				return;

			RTC_Command cmd = new RTC_Command(CommandType.PULLSWAPSTATE);

			string romFullFilename = RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM();
			//cmd.romFilename = romFullFilename.Substring(romFullFilename.LastIndexOf("\\") + 1, romFullFilename.Length - (romFullFilename.LastIndexOf("\\") + 1));
			cmd.romFilename = Path.GetFileName(romFullFilename);

			if (!PeerHasRom(cmd.romFilename))
				cmd.romData = File.ReadAllBytes(romFullFilename);

			StashKey sk = RTC_StockpileManager.SaveState(false);
			cmd.stashkey = sk;
			sk.EmbedState();

			cmd.Priority = true;
			SendCommand(cmd, false);
		}

		public void SendStashkey()
		{
			if (side == NetworkSide.DISCONNECTED)
				return;

			if (RTC_StockpileManager.CurrentStashkey == null)
			{
				MessageBox.Show("Couldn't fetch Stashkey from RTC_StockpileManager.currentStashkey");
				return;
			}

			RTC_Command cmd = new RTC_Command(CommandType.STASHKEY)
			{
				romFilename = Path.GetFileName(RTC_Hooks.BIZHAWK_GET_CURRENTLYOPENEDROM())
			};

			if (!PeerHasRom(cmd.romFilename))
				cmd.romData = File.ReadAllBytes(cmd.romFilename);

			cmd.stashkey = RTC_StockpileManager.CurrentStashkey;
			cmd.stashkey.EmbedState();

			cmd.Priority = true;

			SendCommand(cmd, false);
		}

		private RTC_Command GetLatestScreenFrame(LinkedList<RTC_Command> cmdQueue)
		{
			RTC_Command cmd = null;

			try
			{
				Stack<RTC_Command> vidcmds = new Stack<RTC_Command>();

				foreach (var vidcmd in cmdQueue.ToArray())
					if (vidcmd.Type == CommandType.PUSHSCREEN)
						vidcmds.Push(vidcmd);

				cmd = vidcmds.Peek();

				foreach (var vidcmd in vidcmds)
					cmdQueue.Remove(vidcmd);
			}
			catch (Exception ex)
			{
				OutputException(ex);
			}

			return cmd;
		}

		public void UpdatePeerScreen(Image img)
		{
			if (S.GET<RTC_Multiplayer_Form>().btnPopoutPeerGameScreen.Visible == false)
				S.GET<RTC_MultiPeerPopout_Form>().pbPeerScreen.Image = img;
			else
				S.GET<RTC_Multiplayer_Form>().pbPeerScreen.Image = img;
		}

		public void SendBlastlayer()
		{
			if (side == NetworkSide.DISCONNECTED)
				return;

			if (RTC_StockpileManager.CurrentStashkey == null || RTC_StockpileManager.CurrentStashkey.BlastLayer == null)
			{
				MessageBox.Show("Couldn't fetch BlastLayer from RTC_StockpileManager.currentStashkey");
				return;
			}

			RTC_Command cmd = new RTC_Command(CommandType.BLAST)
			{
				blastlayer = RTC_StockpileManager.CurrentStashkey.BlastLayer
			};

			SendCommand(cmd, false);
		}
	}
}
