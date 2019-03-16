using MediaPortal.Profile;
using MediaPortal.Video.Database;

using NLog;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MyVideoImporter
{
  public class VideoImporter
  {
    #region Private Variables

    private static Logger logger = LogManager.GetCurrentClassLogger();
    private readonly object syncRoot = new object();

    private Thread pathScannerThread;
    private Thread watcherThread;
    private Thread queueThread;
    private List<FileSystemWatcher> fileSystemWatchers;

    // Files that have recently been added to the filesystem, and need to be processed.
    public List<NewMovie> MovieList
    {
      get { return movielist; }
    }
    private List<NewMovie> movielist;

    bool importerStarted = false;
    internal int SyncPointScann;

    #endregion

    // Creates a MovieImporter object which will scan ImportPaths and import new media.
    public VideoImporter()
    {
      Initialize();
    }

    private void Initialize()
    {
      movielist = new List<NewMovie>();
      fileSystemWatchers = new List<FileSystemWatcher>();
      SyncPointScann = 0;
      Utils.UpdateImporterProperties(0, false);
    }

    ~VideoImporter()
    {
      Stop();
    }

    #region Public Methods

    public void Start()
    {
      lock (syncRoot)
      {
        if (importerStarted)
        {
          return;
        }

        if (watcherThread == null)
        {
          watcherThread = new Thread(new ThreadStart(MonitorPaths));
          watcherThread.Start();
          watcherThread.Name = "MyVideoFolderWathcer";
        }
        if (pathScannerThread == null)
        {
          pathScannerThread = new Thread(new ThreadStart(ScanPaths));
          pathScannerThread.Start();
          pathScannerThread.Name = "MyVideoFolderScanner";
        }
        if (queueThread == null)
        {
          queueThread = new Thread(new ThreadStart(ProcessQueue));
          queueThread.Start();
          queueThread.Name = "MyVideoQueueProcess";
        }
      }
      importerStarted = true;
    }

    public void Stop()
    {
      lock (syncRoot)
      {
        if (!importerStarted)
        {
          return;
        }

        if (fileSystemWatchers != null && fileSystemWatchers.Count > 0)
        {
          foreach (FileSystemWatcher currWatcher in fileSystemWatchers)
          {
            currWatcher.EnableRaisingEvents = false;
            currWatcher.Created -= OnFileAdded;
            currWatcher.Deleted -= OnFileDeleted;
            currWatcher.Renamed -= OnFileRenamed;
          }

          fileSystemWatchers.Clear();
        }
        
        if (watcherThread != null)
        {
          logger.Info("Shutting Down MyVideo Wather Thread...");
          watcherThread.Abort();

          // wait for the watcher to shut down
          while (watcherThread.IsAlive)
            Thread.Sleep(100);

          watcherThread = null;
        }

        if (pathScannerThread != null)
        {
          logger.Info("Shutting Down MyVideo Path Scanner Thread...");
          pathScannerThread.Abort();

          // wait for the path scanner to shut down
          while (pathScannerThread.IsAlive)
            Thread.Sleep(100);

          pathScannerThread = null;
        }

        if (queueThread != null)
        {
          logger.Info("Shutting Down MyVideo Queue Process Thread...");
          queueThread.Abort();

          // wait for the path scanner to shut down
          while (queueThread.IsAlive)
            Thread.Sleep(100);

          queueThread = null;
        }

        logger.Info("Stopped MyVideo Importer");
        importerStarted = false;
      }
    }

    public bool IsScanning
    {
      get
      {
        return SyncPointScann == 1;
      }
    }

    public bool IsStarted
    {
      get
      {
        return importerStarted;
      }
    }

    public void RestartImporter()
    {
      this.Stop();
      this.Initialize();
      this.Start();
    }

    public void RescanFiles()
    {
      lock (syncRoot)
      {
        if (IsScanning)
        {
          return;
        }

        if (pathScannerThread == null)
        {
          pathScannerThread = new Thread(new ThreadStart(ScanPaths));
          pathScannerThread.Start();
          pathScannerThread.Name = "MyVideoFolderScanner";
        }
        else
        {
          if (!pathScannerThread.IsAlive)
          {
            pathScannerThread.Start();
          }
        }
      }
    }

    public void RescanIMDB()
    {
      lock (((ICollection)movielist).SyncRoot)
      {
        foreach (NewMovie movie in movielist)
        {
          if (movie.IsNeedRescan)
          {
            // movie.ReStartFetch();
            movie.Status = Utils.ImporterStatus.QUEUED_IMDB;
            logger.Debug("Add file {0} to IMDB fetch queue.", movie.FileName);
          }
        }
      }
    }

    public bool HasUnComplete()
    {
      if (movielist == null || movielist.Count <= 0)
      {
        return false;
      }

      bool result = false;
      lock (((ICollection)movielist).SyncRoot)
      {
        foreach (NewMovie movie in movielist)
        {
          if (!movie.IsComplete)
          {
            result = true;
          }
        }
      }
      return result;
    }

    #endregion

    #region File System Scanner

    private List<string> GetAllVideoPath()
    {
      lock (syncRoot)
      {
        int maximumShares = 128;
        List<string> availablePaths = new List<string>();

        using (Settings xmlreader = new MPSettings())
        {
          for (int index = 0; index < maximumShares; index++)
          {
            string sharePath = String.Format("sharepath{0}", index);
            string shareDir = xmlreader.GetValueAsString("movies", sharePath, "");
            string shareScan = String.Format("sharescan{0}", index);
            bool shareScanData = xmlreader.GetValueAsBool("movies", shareScan, true);

            if (shareScanData && !string.IsNullOrEmpty(shareDir))
            {
              if (!availablePaths.Contains(shareDir))
              {
                availablePaths.Add(shareDir);
              }
              else
              {
                logger.Debug("GetAllVideoPath: Skip due exist in Path lists...");
              }
            }
          }
        }

        return availablePaths;
      }
    }

    private void ProcessQueue()
    {
      try
      {
        while (true)
        {
          Thread.Sleep(1000);
          if (movielist.Count == 0)
          {
            continue;
          }

          lock (((ICollection)movielist).SyncRoot)
          {
            if (movielist.Count(m => m.IsGettingInfo) == 0)
            {
              NewMovie movie = movielist.FirstOrDefault(m => m.Status == Utils.ImporterStatus.QUEUED_IMDB);
              if (movie != null)
              {
                logger.Debug("Starting search IMDB info for {0} - {1}.", movie.MovieDetails.SearchString, movie.FileName);
                movie.StartFetch();
              }
              else
              {
                movie = movielist.FirstOrDefault(m => m.Status == Utils.ImporterStatus.QUEUED_INFO);
                if (movie != null)
                {
                  logger.Debug("Starting fetch IMDB details for {0}: {1} - {2}.", movie.Selected, movie.MovieDetails.SearchString, movie.FileName);
                  movie.StartFetchDetails();
                }
              }
            }
          }
          Utils.UpdateImporterProperties(movielist.Count, HasUnComplete());
        }
      }
      catch (ThreadAbortException)
      {
      }
    }

    private void MonitorPaths()
    {
      try
      {
        logger.Info("Initiating file watchers on MyVideo folders.");
        SetupFileSystemWatchers();
      }
      catch (ThreadAbortException)
      {
      }
    }

    private void ScanPaths()
    {
      if (Interlocked.CompareExchange(ref SyncPointScann, 1, 0) != 0)
      {
        return;
      }

      try
      {
        logger.Info("Scan of MyVideo folders.");

        List<string> paths = GetAllVideoPath();
        foreach (string currPath in paths)
        {
          ScanPath(currPath);
        }
        Utils.SendMovieListRefresh();
      }
      catch (ThreadAbortException)
      {
      }

      SyncPointScann = 0;
    }

    // Sets up the objects that will watch the file system for changes, specifically
    // new files added to the import path, or old files removed.
    private void SetupFileSystemWatchers()
    {
      // clear out old watchers, if any
      foreach (FileSystemWatcher currWatcher in fileSystemWatchers)
      {
        currWatcher.EnableRaisingEvents = false;
        currWatcher.Dispose();
      }
      fileSystemWatchers.Clear();

      List<string> paths = GetAllVideoPath();
      // fill the watcher queue with import paths
      foreach (string currPath in paths)
      {
        if (Directory.Exists(currPath))
        {
          try
          {
            WatchImportPath(currPath);
          }
          catch
          {
            logger.Info("Cancelled watching: '{0}' - Path does not exist.", currPath);
          }
        }
      }
    }

    private void WatchImportPath(string importPath)
    {
      FileSystemWatcher watcher = new FileSystemWatcher();
      watcher.Path = importPath;
      watcher.IncludeSubdirectories = true;
      watcher.Error += OnWatcherError;
      watcher.Created += OnFileAdded;
      watcher.Deleted += OnFileDeleted;
      watcher.Renamed += OnFileRenamed;
      watcher.EnableRaisingEvents = true;

      fileSystemWatchers.Add(watcher);
      logger.Info("Started watching '{0}' - Path is now being monitored for changes.", importPath);
    }

    // When a FileSystemWatcher gets corrupted this handler will add it to the queue again
    private void OnWatcherError(object source, ErrorEventArgs e)
    {
      Exception watchException = e.GetException();
      FileSystemWatcher watcher = source as FileSystemWatcher;

      if (watcher != null)
      {
        if (fileSystemWatchers.Contains(watcher))
        {
          fileSystemWatchers.Remove(watcher);
        }
        // Clean the old watcher
        watcher.Dispose();
      }
    }

    // When a FileSystemWatcher detects a new file, this method queues it up for processing.
    private void OnFileAdded(Object source, FileSystemEventArgs e)
    {
      string filename = e.FullPath;
      if (File.Exists(filename))
      {
        if (MediaPortal.Util.Utils.IsVideo(filename))
        {
          if (!VideoDatabase.HasMovieInfo(filename))
          {
            AddToMovieList(filename);
          }
        }
      }
      else
      {
        return;
      }
    }

    // When a FileSystemWatcher detects a file has been removed, delete it.
    private void OnFileDeleted(Object source, FileSystemEventArgs e)
    {
      string filename = e.FullPath;
      RemoveFromMovieList(filename);
    }

    private void OnFileRenamed(object source, RenamedEventArgs e)
    {
      List<string> localMediaRenamed = new List<string>();

      logger.Debug("OnRenamedEvent: ChangeType={0}, OldFullPath='{1}', FullPath='{2}'", e.ChangeType.ToString(), e.OldFullPath, e.FullPath);

      if (File.Exists(e.FullPath))
      {
        // if the old filename still exists then this probably isn't a reliable rename event
        if (File.Exists(e.OldFullPath))
          return;

        if (VideoDatabase.HasMovieInfo(e.FullPath))
          return;

        RenameInMovieList(e.OldFullPath, e.FullPath);
      }
      else
      {
        return;
      }
    }

    // Grabs the files from the DBImportPath and add them to the queue for use
    // by the ScanMedia thread.
    private void ScanPath(string importPath)
    {
      try
      {
        // AddToMovieList(@"M:\Video\Movies\The Thing.mkv");
        if (Directory.Exists(importPath))
        {
          string[] files = Directory.GetFiles(importPath, "*.*", SearchOption.AllDirectories);
          if (files == null)
          {
            return;
          }

          foreach (string filename in files)
          {
            if (MediaPortal.Util.Utils.IsVideo(filename))
            {
              if (!VideoDatabase.HasMovieInfo(filename))
              {
                AddToMovieList(filename);
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.Error("ScanPath: " + ex);
      }
    }

    private void AddToMovieList(string filename)
    {
      lock (((ICollection)movielist).SyncRoot)
      {
        NewMovie newmovie = movielist.FirstOrDefault(x => x.FileName == filename);
        if (newmovie == null)
        {
          newmovie = ShouldStack(filename);
          if (newmovie != null)
          {
            logger.Debug("Stacked file {0} skip.", filename);
            return;
          }

          newmovie = new NewMovie(filename);
          newmovie.FileName = filename;
          newmovie.Status = Utils.ImporterStatus.QUEUED_IMDB;
          movielist.Add(newmovie);

          logger.Debug("Add new file {0} to IMDB fetch queue.", filename);
          Utils.UpdateImporterProperties(movielist.Count, HasUnComplete());
        }
      }
    }

    private void RenameInMovieList(string filename, string newfilename)
    {
      lock (((ICollection)movielist).SyncRoot)
      {
        NewMovie newmovie = movielist.FirstOrDefault(x => x.FileName == filename);
        if (newmovie != null)
        {
          newmovie.NewFileName = newfilename;

          logger.Debug("Watcher rename file {0} -> {1} in the file list.", filename, newfilename);
          Utils.UpdateImporterProperties(movielist.Count, HasUnComplete());
        }
      }
    }

    private void RemoveFromMovieList(string filename)
    {
      lock (((ICollection)movielist).SyncRoot)
      {
        NewMovie newmovie = movielist.FirstOrDefault(x => x.FileName == filename);
        if (newmovie != null)
        {
          newmovie.Fetcher.CancelFetch();
          Thread.Sleep(100);
          newmovie.Status = Utils.ImporterStatus.NONE;
          movielist.Remove(newmovie);

          logger.Debug("Watcher remove {0} from the file list.", filename);
          Utils.UpdateImporterProperties(movielist.Count, HasUnComplete());
        }
      }
    }

    private NewMovie ShouldStack(string filename)
    {
      foreach (NewMovie movie in movielist)
      {
        if (MediaPortal.Util.Utils.ShouldStack(movie.FileName, filename))
        {
          return movie;
        }
      }
      return null;
    }

    #endregion
  }
}
