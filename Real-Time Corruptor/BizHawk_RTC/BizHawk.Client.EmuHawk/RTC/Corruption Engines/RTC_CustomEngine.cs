﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace RTC
{
	public static class RTC_CustomEngine
	{

		public static BlastUnit GenerateUnit(string domain, long address, int precision)
		{
			try
			{

				BlastUnitSource Source = (BlastUnitSource)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_SOURCE.ToString()];
				CustomValueSource ValueSource = (CustomValueSource)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_SOURCE.ToString()];
				string ValueListHash = (string)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_VALUELISTHASH.ToString()];
				string LimiterListHash = (string)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_LIMITERLISTHASH.ToString()];
				long MinValue8Bit =  (long)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_MINVALUE8BIT.ToString()];
				long MaxValue8Bit =  (long)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_MAXVALUE8BIT.ToString()];
				long MinValue16Bit = (long)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_MINVALUE16BIT.ToString()];
				long MaxValue16Bit = (long)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_MAXVALUE16BIT.ToString()];
				long MinValue32Bit = (long)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_MINVALUE32BIT.ToString()];
				long MaxValue32Bit = (long)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_MAXVALUE32BIT.ToString()];

				StoreTime StoreTime = (StoreTime)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_STORETIME.ToString()];
				CustomStoreAddress StoreAddress = (CustomStoreAddress)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_STOREADDRESS.ToString()];
				StoreType StoreType = (StoreType)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_STORETYPE.ToString()];
				int Delay = (int)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_DELAY.ToString()];
				int Lifetime = (int)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_LIFETIME.ToString()];
				LimiterTime LimiterTime = (LimiterTime)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_LIMITERTIME.ToString()];
				bool Loop = (bool)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_LOOP.ToString()];
				bool LimiterInverted = (bool)RTC_Unispec.RTCSpec[RTCSPEC.CUSTOM_LIMITERINVERTED.ToString()];

				if (domain == null)
					return null;

				MemoryDomainProxy mdp = RTC_MemoryDomains.GetProxy(domain, address);


				byte[] value = new byte[precision];
				long safeAddress = address - (address % precision);

			
				BlastUnit bu = new BlastUnit();

				switch (Source)
				{
					case BlastUnitSource.VALUE:
					{
						switch (ValueSource)
						{
							case CustomValueSource.VALUELIST:
							{
								value = RTC_Filtering.GetRandomConstant(ValueListHash, precision);
							}
							break;

							case CustomValueSource.RANGE:
							{
								long randomValue = -1;
								switch (precision)
								{
									case 1:
										randomValue = RTC_Core.RND.RandomLong(MinValue8Bit, MaxValue8Bit);
										break;
									case 2:
										randomValue = RTC_Core.RND.RandomLong(MinValue16Bit, MaxValue16Bit);
										break;
									case 4:
										randomValue = RTC_Core.RND.RandomLong(MinValue32Bit, MaxValue32Bit);
										break;
								}

								if (randomValue != -1)
									value = RTC_Extensions.GetByteArrayValue(precision, randomValue, true);
								else
									for (int i = 0; i < precision; i++)
										value[i] = (byte)RTC_Core.RND.Next();
							}
							break;

							case CustomValueSource.RANDOM:
							{
								for (int i = 0; i < precision; i++)
									value[i] = (byte)RTC_Core.RND.Next();
							}
							break;
								
						}
					}
					break;

					case BlastUnitSource.STORE:
					{
						bu.StoreType = StoreType;
						bu.StoreTime = StoreTime;

						switch (StoreAddress)
						{
							case CustomStoreAddress.RANDOM:
							{
								BlastTarget bt = RTC_Core.GetBlastTarget();
								long safeStartAddress = bt.Address - (bt.Address % precision);
								bu.SourceDomain = bt.Domain;
								bu.SourceAddress = safeStartAddress;
							}
							break;
							case CustomStoreAddress.SAME:
							{
								bu.SourceDomain = domain;
								bu.SourceAddress = safeAddress;
							}
							break;
						}
					}
					break;
				}
				//Precision has to be before Value
				bu.Precision = precision;
				bu.Value = value;
				bu.Address = safeAddress;
				bu.Domain = domain;
				bu.Source = Source;
				bu.ExecuteFrame = Delay;
				bu.Lifetime = Lifetime;
				bu.LimiterTime = LimiterTime;
				bu.Loop = Loop;
				bu.InvertLimiter = LimiterInverted;

				//Only set a list if it's used to save on memory
				if (LimiterTime != LimiterTime.NONE)
					bu.LimiterListHash = LimiterListHash;

				//Limiter handling
				if (LimiterTime == LimiterTime.IMMEDIATE)
				{
					if (LimiterInverted)
					{
						//If it's store, we need to use the sourceaddress and sourcedomain
						if (Source == BlastUnitSource.STORE && RTC_Filtering.LimiterPeekBytes(bu.SourceAddress,
								bu.SourceAddress + bu.Precision, bu.SourceDomain, LimiterListHash, mdp))
							return null;
						if (Source == BlastUnitSource.VALUE && RTC_Filtering.LimiterPeekBytes(bu.Address,
															 bu.Address + bu.Precision, bu.Domain, LimiterListHash, mdp))
							return null;
					}
					else
					{
						//If it's store, we need to use the sourceaddress and sourcedomain
						if (Source == BlastUnitSource.STORE && !RTC_Filtering.LimiterPeekBytes(bu.SourceAddress,
								bu.SourceAddress + bu.Precision, bu.SourceDomain, LimiterListHash, mdp))
							return null;
						if (Source == BlastUnitSource.VALUE && !RTC_Filtering.LimiterPeekBytes(bu.Address,
															 bu.Address + bu.Precision, bu.Domain, LimiterListHash, mdp))
							return null;
					}
				}

				return bu;
			}
			catch (Exception ex)
			{
				throw new Exception("Custom Engine GenerateUnit Threw Up" + ex);
			}
		}


		public static bool IsConstant(byte[] bytes, string[] list, bool bigEndian)
		{
			if (list == null)
				return true;
			if (!bigEndian)
				return list.Contains(ByteArrayToString(bytes));
			Array.Reverse(bytes);
			return list.Contains(ByteArrayToString(bytes));
		}

		public static string ByteArrayToString(byte[] bytes)
		{
			return BitConverter.ToString(bytes).Replace("-", "").ToLower();
		}

		public static byte[] GetRandomConstant(string[] list)
		{
			if (list == null)
			{
				byte[] buffer = new byte[4];
				RTC_Core.RND.NextBytes(buffer);
				return buffer;
			}

			return StringToByteArray(list[RTC_Core.RND.Next(list.Length)]);
		}

		public static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
				.Where(x => x % 2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}

		public static void Initialize(PartialSpec pSpec)
		{
			pSpec[RTCSPEC.CUSTOM_DELAY.ToString()] = 0;
			pSpec[RTCSPEC.CUSTOM_LIFETIME.ToString()] = 1;
			pSpec[RTCSPEC.CUSTOM_LOOP.ToString()] = false;

			pSpec[RTCSPEC.CUSTOM_TILTVALUE.ToString()] = 1;

			pSpec[RTCSPEC.CUSTOM_LIMITERLISTHASH.ToString()] = null;
			pSpec[RTCSPEC.CUSTOM_LIMITERTIME.ToString()] = LimiterTime.NONE;
			pSpec[RTCSPEC.CUSTOM_LIMITERINVERTED.ToString()] = false;

			pSpec[RTCSPEC.CUSTOM_VALUELISTHASH.ToString()] = null;

			pSpec[RTCSPEC.CUSTOM_MINVALUE8BIT.ToString()] = 0L;
			pSpec[RTCSPEC.CUSTOM_MINVALUE16BIT.ToString()] = 0L;
			pSpec[RTCSPEC.CUSTOM_MINVALUE32BIT.ToString()] = 0L;
			pSpec[RTCSPEC.CUSTOM_MAXVALUE8BIT.ToString()] = 0xFFL;
			pSpec[RTCSPEC.CUSTOM_MAXVALUE16BIT.ToString()] = 0xFFFFL;
			pSpec[RTCSPEC.CUSTOM_MAXVALUE32BIT.ToString()] = 0xFFFFFFFFL;

			pSpec[RTCSPEC.CUSTOM_VALUESOURCE.ToString()] = CustomValueSource.RANDOM;

			pSpec[RTCSPEC.CUSTOM_SOURCE.ToString()] = BlastUnitSource.VALUE;

			pSpec[RTCSPEC.CUSTOM_STOREADDRESS.ToString()] = CustomStoreAddress.RANDOM;
			pSpec[RTCSPEC.CUSTOM_STORETIME.ToString()] = StoreTime.IMMEDIATE;
			pSpec[RTCSPEC.CUSTOM_STORETYPE.ToString()] = StoreType.ONCE;
		}
	}
}
