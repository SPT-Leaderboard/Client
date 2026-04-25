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

#if DEBUG
		public ConfigEntry<bool> Debug;
#endif
#if DEBUG || BETA
		public ConfigEntry<float> PositionOverlayX;
		public ConfigEntry<float> PositionOverlayY;
		public ConfigEntry<int> FontSizeOverlay;
		public ConfigEntry<int> ZoneOverlayFontSize;
		public ConfigEntry<float> ZoneOverlayMaxDistance;
		public ConfigEntry<float> ZoneOverlayUpDistance;
		public ConfigEntry<bool> ShowZoneCoordinateAxes;
		public ConfigEntry<bool> ShowZoneOverlays;
		public ConfigEntry<bool> ShowZones;
		public ConfigEntry<bool> ZonesSeeThroughWalls;
		public ConfigEntry<bool> ShowZonePlanes;
		public ConfigEntry<float> ZonePlanesTransparency;
#endif

		private Settings(ConfigFile configFile)
		{
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
				"SPTLB Server Endpoint", 
				"https://sptlb.katrinfoxvr.com", 
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
				20, 
				new ConfigDescription(
					"Timer for requests in server for support status IN_RAID",
					null, 
					new ConfigurationAttributes
					{
						Order = 0,
						IsAdvanced = true
					}));

#if DEBUG
			Debug = configFile.Bind(
				"2. Debug",
				"Debug",
				true,
				new ConfigDescription("Developer toggle"));
#endif
#if DEBUG || BETA
			// Main overlay settings
			PositionOverlayX = configFile.Bind(
				"2. Debug",
				"PositionX",
				10f,
				new ConfigDescription("X Position", new AcceptableValueRange<float>(-2000f, 2000f)));

			PositionOverlayY = configFile.Bind(
				"2. Debug",
				"PositionY",
				-10f,
				new ConfigDescription("Y Position", new AcceptableValueRange<float>(-2000f, 2000f)));

			FontSizeOverlay = configFile.Bind(
				"2. Debug",
				"FontSizeDebug",
				28,
				new ConfigDescription("FontSizeDebug", new AcceptableValueRange<int>(0, 200)));

			// Zones overlay settings
			ZoneOverlayFontSize = configFile.Bind(
				"2. Debug",
				"ZoneOverlayFontSize",
				10,
				new ConfigDescription(
					"Set font size in overlay zone",
					new AcceptableValueRange<int>(2, 32),
					new ConfigurationAttributes()));

			ZoneOverlayMaxDistance = configFile.Bind(
				"2. Debug",
				"ZoneOverlayMaxDistance",
				200f,
				new ConfigDescription(
					"Max distance to render an overlay zone",
					new AcceptableValueRange<float>(0f, 1000f),
					new ConfigurationAttributes()));

			ZoneOverlayUpDistance = configFile.Bind(
				"2. Debug",
				"ZoneOverlayUpDistance",
				1.5f,
				new ConfigDescription(
					"Distance the overlay is above the zones",
					new AcceptableValueRange<float>(0f, 5f),
					new ConfigurationAttributes()));

			ShowZoneCoordinateAxes = configFile.Bind(
				"3. Zones",
				"Show Zone Coordinate Axes",
				false,
				new ConfigDescription(
					"Show zone coordinate axes (X=red, Y=green, Z=blue) for each zone",
					null,
					new ConfigurationAttributes()));

			ShowZoneOverlays = configFile.Bind(
				"3. Zones",
				"Show Zone Overlays",
				false,
				new ConfigDescription(
					"Show text overlays for zones",
					null,
					new ConfigurationAttributes()));

			ShowZones = configFile.Bind(
				"3. Zones",
				"Show Zones",
				false,
				new ConfigDescription(
					"Master toggle to show or hide all zone visuals",
					null,
					new ConfigurationAttributes()));

			ZonesSeeThroughWalls = configFile.Bind(
				"3. Zones",
				"Zones See Through Walls",
				false,
				new ConfigDescription(
					"Make zone lines visible through walls",
					null,
					new ConfigurationAttributes()));

			ShowZonePlanes = configFile.Bind(
				"3. Zones",
				"Show Zone Planes",
				false,
				new ConfigDescription(
					"Show semi-transparent colored planes for each zone face",
					null,
					new ConfigurationAttributes()));

			ZonePlanesTransparency = configFile.Bind(
				"3. Zones",
				"Zone Planes Transparency Value",
				0.3f,
				new ConfigDescription("Value", new AcceptableValueRange<float>(0f, 1f)));
			
			PositionOverlayX.SettingChanged += (_, __) =>
			{
				OverlayDebug.Instance.SetOverlayPosition(new Vector2(PositionOverlayX.Value, PositionOverlayY.Value));
			};
			
			PositionOverlayY.SettingChanged += (_, __) =>
			{
				OverlayDebug.Instance.SetOverlayPosition(new Vector2(PositionOverlayX.Value, PositionOverlayY.Value));
			};

			FontSizeOverlay.SettingChanged += (_, __) =>
			{
				OverlayDebug.Instance.SetFontSize(FontSizeOverlay.Value);
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