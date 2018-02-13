using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Services;

using NLog;
using NLog.Config;
using NLog.Targets;

using System;
using System.IO;

namespace MyVideoImporter
{
  public class Utils
  {
    #region Log 
    private static Logger logger = LogManager.GetCurrentClassLogger(); // log
    private const string LogFileName = "MyVideoImporter.log";          // log's filename
    private const string OldLogFileName = "MyVideoImporter.bak";       // log's old filename        
    #endregion

    #region Variables
    public static ePriority _priority = ePriority.Lowest;
    #endregion

    public static int WindowID = 99555;
    public static int FacadeID = 50;

    #region Settings
    private const string ConfigFilename = "MyVideoImporter.xml";

    public static bool ApproveIfOne = false;
    public static bool ApproveForNearest = false;
    public static int NearestFactor = 20;
    #endregion

    static Utils()
    {
    }

    public static ePriority Priority
    {
      get { return _priority; }
      set { _priority = value; }
    }

    public static void InitLogger()
    {
      LoggingConfiguration config = LogManager.Configuration ?? new LoggingConfiguration();

      try
      {
        FileInfo logFile = new FileInfo(Config.GetFile(Config.Dir.Log, LogFileName));
        if (logFile.Exists)
        {
          if (File.Exists(Config.GetFile(Config.Dir.Log, OldLogFileName)))
            File.Delete(Config.GetFile(Config.Dir.Log, OldLogFileName));

          logFile.CopyTo(Config.GetFile(Config.Dir.Log, OldLogFileName));
          logFile.Delete();
        }
      }
      catch (Exception) { }

      FileTarget fileTarget = new FileTarget();
      fileTarget.FileName = Config.GetFile(Config.Dir.Log, LogFileName);
      fileTarget.Encoding = "utf-8";
      fileTarget.Layout   = "${date:format=dd-MMM-yyyy HH\\:mm\\:ss} " +
                            "${level:fixedLength=true:padding=5} " +
                            "[${logger:fixedLength=true:padding=20:shortName=true}]: ${message} " +
                            "${exception:format=tostring}";

      config.AddTarget("myvideo-importer", fileTarget);

      // Get current Log Level from MediaPortal 
      LogLevel logLevel;
      MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));

      string str = xmlreader.GetValue("general", "ThreadPriority");
      Priority = str == null || !str.Equals("Normal", StringComparison.CurrentCulture) ? (str == null || !str.Equals("BelowNormal", StringComparison.CurrentCulture) ? ePriority.BelowNormal : ePriority.Lowest) : ePriority.Lowest;

      switch ((Level)xmlreader.GetValueAsInt("general", "loglevel", 0))
      {
        case Level.Error:
          logLevel = LogLevel.Error;
          break;
        case Level.Warning:
          logLevel = LogLevel.Warn;
          break;
        case Level.Information:
          logLevel = LogLevel.Info;
          break;
        case Level.Debug:
        default:
          logLevel = LogLevel.Debug;
          break;
      }

      #if DEBUG
      logLevel = LogLevel.Debug;
      #endif

      LoggingRule rule = new LoggingRule("MyVideoImporter.*", logLevel, fileTarget);
      config.LoggingRules.Add(rule);

      LogManager.Configuration = config;
    }

    #region Settings 
    public static string Check(bool Value, bool Box = true)
    {
      return (Box ? "[" : string.Empty) + (Value ? "x" : " ") + (Box ? "]" : string.Empty);
    }

    public static string Check(string Value, bool Box = true)
    {
      return Check(Value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || Value.Equals("yes", StringComparison.CurrentCultureIgnoreCase), Box);
    }

    public static void LoadSettings()
    {
      ApproveIfOne = false;
      ApproveForNearest = false;
      NearestFactor = 20;

      try
      {
        logger.Debug("Load settings from: " + ConfigFilename);
        #region Load settings
        using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, ConfigFilename)))
        {
          ApproveIfOne = xmlreader.GetValueAsBool("Fetcher", "ApproveIfOne", ApproveIfOne);
          ApproveForNearest = xmlreader.GetValueAsBool("Fetcher", "ApproveForNearest", ApproveForNearest);
          NearestFactor = xmlreader.GetValueAsInt("Fetcher", "NearestFactor", NearestFactor);
        }
        #endregion
        logger.Debug("Load settings from: " + ConfigFilename + " complete.");
      }
      catch (Exception ex)
      {
        logger.Error("LoadSettings: " + ex);
      }

      #region Report Settings
      logger.Debug("Importer: " + Check(ApproveIfOne) + " If one, " + Check(ApproveForNearest) + " Nearest <= " + NearestFactor);
      #endregion
    }

    public static void SaveSettings()
    {
      try
      {
        logger.Debug("Save settings to: " + ConfigFilename);
        #region Save settings
        using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, ConfigFilename)))
        {
          xmlwriter.SetValue("Fetcher", "ApproveIfOne", ApproveIfOne);
          xmlwriter.SetValue("Fetcher", "ApproveForNearest", ApproveForNearest);
          xmlwriter.SetValue("Fetcher", "NearestFactor", NearestFactor);
        }
        #endregion
        logger.Debug("Save settings to: " + ConfigFilename + " complete.");
      }
      catch (Exception ex)
      {
        logger.Error("SaveSettings: " + ex);
      }
    }

    #endregion

    /// <summary>
    /// Returns plugin version.
    /// </summary>
    internal static string GetAllVersionNumber()
    {
      return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    internal static void SendMovieListRefresh()
    {
      GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_REFRESH, WindowID, 0, FacadeID, 0, 0, null);
      GUIGraphicsContext.SendMessage(msg);
    }

    internal static void SendMovieRefresh()
    {
      // Send global message that movie is refreshed/scanned
      GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_VIDEOINFO_REFRESH, 0, 0, 0, 0, 0, null);
      GUIWindowManager.SendMessage(msg);
    }

    #region Properties

    internal static void SetProperty(string property, string value)
    {
      if (string.IsNullOrEmpty(property))
      {
        return;
      }
      if (string.IsNullOrEmpty(value))
      {
        value = string.Empty;
      }

      try
      {
        GUIPropertyManager.SetProperty(property, value);
      }
      catch (Exception ex)
      {
        logger.Error("SetProperty: " + ex.ToString());
      }
    }

    internal static void UpdateImporterProperties(int count, bool hasnew)
    {
      SetProperty("#importer.header", Translation.Header);
      SetProperty("#importer.hasnew", hasnew ? "true" : "false");
      SetProperty("#importer.hasmovies", count > 0 ? "true" : "false");
      SetProperty("#importer.count", count.ToString());
    }

    #endregion
    
    internal static string GetIcon(ImporterStatus status)
    {
      return "importer_" + status.ToString() + ".png";
    }

    internal static string GetStatus(ImporterStatus status)
    {
      switch (status)
      {
        case ImporterStatus.ADDED:
          return Translation.StatusADDED;
        case ImporterStatus.GETTING_IMDB:
          return Translation.StatusGETTING_IMDB;
        case ImporterStatus.GETTING_INFO:
          return Translation.StatusGETTING_INFO;
        case ImporterStatus.QUEUED_IMDB:
          return Translation.StatusQUEUED_IMDB;
        case ImporterStatus.QUEUED_INFO:
          return Translation.StatusQUEUED_INFO;
        case ImporterStatus.SKIP:
          return Translation.StatusSKIP;
        case ImporterStatus.WAITING:
          return Translation.StatusWAITING;
        case ImporterStatus.COMPLETE:
          return Translation.StatusCOMPLETE;
        case ImporterStatus.NONE:
          return Translation.StatusNONE;
      }
      return string.Empty;
    }

    #region Enums

    public enum ePriority
    {
      Lowest,
      BelowNormal,
    }

    public enum View
    {
      List = 0,
      Icons = 1,
      BigIcons = 2,
      Albums = 3,
      Filmstrip = 4,
    }

    public enum ViewLevel
    {
      Files,
      Movies,
    }

    public enum ItemType
    {
      Search,
      Rescan,
      Research,
      File,
      IMDB,
      Stop,
      None,
    }

    public enum ImporterStatus
    {
      ADDED,
      QUEUED_IMDB,
      QUEUED_INFO,
      WAITING,
      GETTING_IMDB,
      GETTING_INFO,
      COMPLETE,
      SKIP,
      NONE,
    }

    #endregion
  }
}
