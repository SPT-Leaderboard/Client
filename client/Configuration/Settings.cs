using BepInEx.Configuration;
using SPTLeaderboard.Utils;
using UnityEngine;

namespace SPTLeaderboard.Configuration
{
	/// <summary>
	/// Model with config fields
	/// </summary>
	public class Settings
	{
		public static Settings Instance { get; private set; }
		
#if DEBUG || BETA
		public ConfigEntry<KeyboardShortcut> KeyBind;
		public ConfigEntry<KeyboardShortcut> KeyBindTwo;
		public ConfigEntry<float> PositionXDebug;
		public ConfigEntry<float> PositionYDebug;
		public ConfigEntry<int> FontSizeDebug;
		public ConfigEntry<int> OverlayFontSize;
		public ConfigEntry<float> OverlayMaxDist;
		public ConfigEntry<float> OverlayUpDist;
#endif
#if DEBUG
		public ConfigEntry<bool> Debug;
#endif

		public ConfigEntry<KeyboardShortcut> ToggleZonesInterfaceKey;

		public ConfigEntry<bool> EnableSendData;
		public ConfigEntry<bool> ShowPointsNotification;
		public ConfigEntry<bool> ShowExperienceNotification;
		public ConfigEntry<bool> ModCasualMode;
		public ConfigEntry<bool> EnableModSupport;
		public ConfigEntry<int> ConnectionTimeout;
		public ConfigEntry<string> PhpEndpoint;
		public ConfigEntry<string> PhpPath;
		public ConfigEntry<int> SupportInRaidConnectionTimer;

		private Settings(ConfigFile configFile)
		{
#if DEBUG || BETA
			KeyBind = configFile.Bind(
				"2. Debug",
				"Test key bind 1", 
				new KeyboardShortcut(KeyCode.LeftArrow), 
				new ConfigDescription(
					"Just keybind for tests")); 
			
			KeyBindTwo = configFile.Bind(
				"2. Debug",
				"Test key bind 2", 
				new KeyboardShortcut(KeyCode.UpArrow), 
				new ConfigDescription(
					"Just keybind for tests"));

			PositionXDebug = configFile.Bind(
				"2. Debug",
				"PositionX",
				10f,
				new ConfigDescription("X Position", new AcceptableValueRange<float>(-2000f, 2000f)));

			PositionYDebug = configFile.Bind(
				"2. Debug",
				"PositionY",
				-10f,
				new ConfigDescription("Y Position", new AcceptableValueRange<float>(-2000f, 2000f)));
			
			FontSizeDebug = configFile.Bind(
				"2. Debug",
				"FontSizeDebug",
				28,
				new ConfigDescription("FontSizeDebug", new AcceptableValueRange<int>(0, 200)));
			
			OverlayFontSize = configFile.Bind(
				"2. Debug",
				"OverlayFontSize",
				18,
				new ConfigDescription(
					"Sets the font size of overlays.",
					new AcceptableValueRange<int>(2, 32),
					new ConfigurationAttributes()));
			
			OverlayMaxDist = configFile.Bind(
				"2. Debug",
				"OverlayMaxDist",
				200f,
				new ConfigDescription(
					"Max distance to render an overlay",
					new AcceptableValueRange<float>(0f, 1000f),
					new ConfigurationAttributes()));
			
			OverlayUpDist = configFile.Bind(
				"2. Debug",
				"OverlayUpDist",
				1.5f,
				new ConfigDescription(
					"Distance the overlay is above the objects",
					new AcceptableValueRange<float>(0f, 5f),
					new ConfigurationAttributes()));
			
			ToggleZonesInterfaceKey = configFile.Bind(
				"1. Settings",
				"Toggle Zones Interface Key",
				new KeyboardShortcut(KeyCode.F2),
				new ConfigDescription(
					"Keybind to toggle ZonesInterface visibility",
					null,
					new ConfigurationAttributes
					{
						Order = 9
					}));
#endif
#if DEBUG
			Debug = configFile.Bind(
				"2. Debug",
				"Debug",
				true,
				new ConfigDescription(
					"Display debug messages in console and log them inside SPT server .log file"));
#endif
			
			EnableSendData = configFile.Bind(
				"1. Settings", 
				"Is Sending Data", 
				true, 
				new ConfigDescription(
					"When disable, stops sending your scores and statistics to the leaderboard server",
					null, 
					new ConfigurationAttributes
					{
						Order = 8
					}));
			
			ShowPointsNotification = configFile.Bind(
				"1. Settings", 
				"Show Notification Points", 
				true, 
				new ConfigDescription(
					"When turned on, display a notification about the issuance of leaderboard points at the end of the raid.",
					null, 
					new ConfigurationAttributes
					{
						Order = 7
					}));
			
			ShowExperienceNotification = configFile.Bind(
				"1. Settings", 
				"Show Notification Experience", 
				true, 
				new ConfigDescription(
					"When turned on, display a notification about the issuance of leaderboard experience at the end of the raid.",
					null, 
					new ConfigurationAttributes
					{
						Order = 6
					}));

			ModCasualMode = configFile.Bind(
				"1. Settings", 
				"Casual mode", 
				false, 
				new ConfigDescription(
					"Enabling this will switch you to a Casual Mode.\n You will not be ranked in the leaderboard and your stats won't count towards its progress.\n You'll be free off any leaderboard restrictions (except reasonable ones), have access to raid history and your profile like usual.\n DANGER - Once you played with this ON - YOU CANT GET BACK INTO RANKING.",
					null, 
					new ConfigurationAttributes
					{
						Order = 5
					}));
			
			EnableModSupport = configFile.Bind(
				"1. Settings", 
				"Mod Support", 
				true, 
				new ConfigDescription(
					"Enable mod support to send extra data for your profile\n Mod automatically detects mods that it supports\n Currently supports: \n Stattrack by AcidPhantasm (extra weapon stats at battlepass tab and weapon mastery)",
					null, 
					new ConfigurationAttributes
					{
						Order = 4
					}));
			
			ConnectionTimeout = configFile.Bind(
				"1. Settings", 
				"Connection Timeout", 
				10, 
				new ConfigDescription(
					"How long mod will be waiting for the response from Leaderboard API, in SECONDS",
					null, 
					new ConfigurationAttributes
					{
						Order = 3,
						IsAdvanced = true
					}));
			
			PhpEndpoint = configFile.Bind(
				"1. Settings", 
				"Server Endpoint", 
				"https://sptlb.yuyui.moe", 
				new ConfigDescription(
					"DO NOT TOUCH UNLESS YOU KNOW WHAT YOU ARE DOING.\n Domain (or both subdomain + domain) used for requests",
					null, 
					new ConfigurationAttributes
					{
						Order = 2,
						IsAdvanced = true
					}));
			
			PhpPath = configFile.Bind(
				"1. Settings", 
				"Server Path", 
				"/api/main/", 
				new ConfigDescription(
					"DO NOT TOUCH UNLESS YOU KNOW WHAT YOU ARE DOING.\n Domain (or both subdomain + domain) used for requests",
					null, 
					new ConfigurationAttributes
					{
						Order = 1,
						IsAdvanced = true
					}));
			
			SupportInRaidConnectionTimer = configFile.Bind(
				"1. Settings", 
				"Support In Raid Connection Timer", 
				60, 
				new ConfigDescription(
					"Timer for requests in server for support status IN_RAID",
					null, 
					new ConfigurationAttributes
					{
						Order = 0,
						IsAdvanced = true
					}));
			
			#if DEBUG || BETA
			PositionXDebug.SettingChanged += (_, __) =>
			{
				OverlayDebug.Instance.SetOverlayPosition(new Vector2(PositionXDebug.Value, PositionYDebug.Value));
			};
			
			PositionYDebug.SettingChanged += (_, __) =>
			{
				OverlayDebug.Instance.SetOverlayPosition(new Vector2(PositionXDebug.Value, PositionYDebug.Value));
			};

			FontSizeDebug.SettingChanged += (_, __) =>
			{
				OverlayDebug.Instance.SetFontSize(FontSizeDebug.Value);
			};
			#endif
		}
		
		/// <summary>
		/// Init configs model
		/// </summary>
		/// <param name="configFile"></param>
		/// <returns></returns>
		public static Settings Create(ConfigFile configFile)
		{
			if (Instance != null)
			{
				return Instance;
			}
			return Instance = new Settings(configFile);
		}
	}
}