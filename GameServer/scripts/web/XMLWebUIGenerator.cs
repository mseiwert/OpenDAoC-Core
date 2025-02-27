using System;
using System.IO;
using System.Reflection;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Database.Connection;
using DOL.Events;

namespace DOL.GS.Scripts
{
	/// <summary>
	/// Generates an XML version of the web ui
	/// </summary>
	public class XMLWebUIGenerator
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
		public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			//Uncomment the following line to enable the WebUI
			//Start();
		}

		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//Uncomment the following line to enable the WebUI
			//Stop();
		}


		private static System.Timers.Timer m_timer = null;

		[DataTable(TableName="ServerInfo")]
		public class ServerInfo : DataObject
		{
			private string m_dateTime = "NA";
			private string m_srvrName = "NA";
			private string m_aac = "NA";
			private string m_srvrType = "NA";
			private string m_srvrStatus = "NA";
			private int m_numClients = 0;
			private int m_numAccts = 0;
			private int m_numMobs = 0;
			private int m_numInvItems = 0;
			private int m_numPlrChars = 0;
			private int m_numMerchantItems = 0;
			private int m_numItemTemplates = 0;
			private int m_numWorldObj = 0;

			[DataElement(AllowDbNull=true)]
			public string Time
			{
				get { return m_dateTime; }
				set { m_dateTime = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string ServerName
			{
				get { return m_srvrName; }
				set { m_srvrName = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string AAC
			{
				get { return m_aac; }
				set { m_aac = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string ServerType
			{
				get { return m_srvrType; }
				set { m_srvrType = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string ServerStatus
			{
				get { return m_srvrStatus; }
				set { m_srvrStatus = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumClients
			{
				get { return m_numClients; }
				set { m_numClients = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumAccounts
			{
				get { return m_numAccts; }
				set { m_numAccts = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumMobs
			{
				get { return m_numMobs; }
				set { m_numMobs = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumInventoryItems
			{
				get { return m_numInvItems; }
				set { m_numInvItems = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumPlayerChars
			{
				get { return m_numPlrChars; }
				set { m_numPlrChars = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumMerchantItems
			{
				get { return m_numMerchantItems; }
				set { m_numMerchantItems = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumItemTemplates
			{
				get { return m_numItemTemplates; }
				set { m_numItemTemplates = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int NumWorldObjects
			{
				get { return m_numWorldObj; }
				set { m_numWorldObj = value; }
			}
		}

		[DataTable(TableName="PlayerInfo")]
		public class PlayerInfo : DataObject
		{
			private string m_name = "NA";
			private string m_lastName = "NA";
			private string m_guild = "NA";
			private string m_race = "NA";
			private string m_class = "NA";
			private string m_alive = "NA";
			private string m_realm = "NA";
			private string m_region = "NA";
			private int m_lvl = 0;
			private int m_x = 0;
			private int m_y = 0;

			[DataElement(AllowDbNull=true)]
			public string Name
			{
				get { return m_name; }
				set { m_name = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string LastName
			{
				get { return m_lastName; }
				set { m_lastName = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string Guild
			{
				get { return m_guild; }
				set { m_guild = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string Race
			{
				get { return m_race; }
				set { m_race = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string Class
			{
				get { return m_class; }
				set { m_class = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string Alive
			{
				get { return m_alive; }
				set { m_alive = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string Realm
			{
				get { return m_realm; }
				set { m_realm = value; }
			}

			[DataElement(AllowDbNull=true)]
			public string Region
			{
				get { return m_region; }
				set { m_region = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int Level
			{
				get { return m_lvl; }
				set { m_lvl = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int X
			{
				get { return m_x; }
				set { m_x = value; }
			}

			[DataElement(AllowDbNull= false)]
			public int Y
			{
				get { return m_y; }
				set { m_y = value; }
			}
		}

		/// <summary>
		/// Reads in the template and generates the appropriate html
		/// </summary>
		public static void Generate()
		{
			try
			{
				var db = ObjectDatabase.GetObjectDatabase(EConnectionType.DATABASE_XML, "."+Path.DirectorySeparatorChar+"webui"+Path.DirectorySeparatorChar+"generated");

				//Obsolete with GSS Table Registering in SVN : 3337
				//db.RegisterDataObject(typeof (ServerInfo));
				//Obsolete with GSS Table Registering in SVN : 3337
				//db.RegisterDataObject(typeof (PlayerInfo));

				ServerInfo si = new ServerInfo();

				si.Time = DateTime.Now.ToString();
				si.ServerName = GameServer.Instance.Configuration.ServerName;
				si.NumClients = ClientService.ClientCount;
				si.NumAccounts = GameServer.Database.GetObjectCount<DbAccount>();
				si.NumMobs = GameServer.Database.GetObjectCount<DbMob>();
				si.NumInventoryItems = GameServer.Database.GetObjectCount<DbInventoryItem>();
				si.NumPlayerChars = GameServer.Database.GetObjectCount<DbCoreCharacter>();
				si.NumMerchantItems = GameServer.Database.GetObjectCount<DbMerchantItem>();
				si.NumItemTemplates = GameServer.Database.GetObjectCount<DbItemTemplate>();
				si.NumWorldObjects = GameServer.Database.GetObjectCount<DbWorldObject>();
				si.ServerType = GameServer.Instance.Configuration.ServerType.ToString();
				si.ServerStatus = GameServer.Instance.ServerStatus.ToString();
				si.AAC = GameServer.Instance.Configuration.AutoAccountCreation ? "enabled" : "disabled";

				db.AddObject(si);

				PlayerInfo pi = new PlayerInfo();

				foreach (GamePlayer player in ClientService.GetPlayers())
				{
					pi.Name = player.Name;
					pi.LastName = player.LastName;
					pi.Class = player.CharacterClass.Name;
					pi.Race = player.RaceName;
					pi.Guild = player.GuildName;
					pi.Level = player.Level;
					pi.Alive = player.IsAlive ? "yes" : "no";
					pi.Realm = player.Realm.ToString();
					pi.Region = player.CurrentRegion.Name;
					pi.X = player.X;
					pi.Y = player.Y;
				}

				// 2008-01-29 Kakuri - Obsolete
				//db.WriteDatabaseTables();
				db = null;

				if (log.IsInfoEnabled)
					log.Info("WebUI Generation initialized");
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("WebUI Generation: ", e);
			}
		}

		/// <summary>
		/// Starts the timer to generate the web ui
		/// </summary>
		public static void Start()
		{
			if (m_timer != null)
			{
				Stop();
			}

			m_timer = new System.Timers.Timer(60000.0); //1 minute
			m_timer.Elapsed += new System.Timers.ElapsedEventHandler(m_timer_Elapsed);
			m_timer.AutoReset = true;
			m_timer.Start();

			if (log.IsInfoEnabled)
				log.Info("Web UI generation started...");
		}

		/// <summary>
		/// Stops the timer that generates the web ui
		/// </summary>
		public static void Stop()
		{
			if (m_timer != null)
			{
				m_timer.Stop();
				m_timer.Close();
				m_timer.Elapsed -= new System.Timers.ElapsedEventHandler(m_timer_Elapsed);
				m_timer = null;
			}

			Generate();

			if (log.IsInfoEnabled)
				log.Info("Web UI generation stopped...");
		}

		/// <summary>
		/// The timer proc that generates the web ui every X milliseconds
		/// </summary>
		/// <param name="sender">Caller of this function</param>
		/// <param name="e">Info about the timer</param>
		private static void m_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Generate();
		}
	}
}
