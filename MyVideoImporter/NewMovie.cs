using MediaPortal.Database;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Video.Database;

using NLog;

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MyVideoImporter
{
  public class NewMovie : IMDB.IProgress
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();

    public string Title
    {
      get { return _title; }
    }
    private string _title = string.Empty;

    public string SearchTitle
    {
      get { return _moviedetails.SearchString; }
      set { _moviedetails.SearchString = value; }
    }

    public string FileName
    {
      get { return _filename; }
      set
      {
        _filename = value;

        _moviedetails.Path = Path.GetDirectoryName(_filename);
        _moviedetails.File = Path.GetFileName(_filename);
        _moviedetails.SearchString = MediaPortal.Util.Utils.GetFilename(_filename, true);
        _title = _moviedetails.SearchString;

        Utils.SendMovieListRefresh();
      }
    }
    private string _filename = string.Empty;

    public string NewFileName
    {
      get { return _newfilename; }
      set { _newfilename = value; }
    }
    private string _newfilename = string.Empty;

    public Utils.ImporterStatus Status
    {
      get { return _status; }
      set
      {
        if (_status != value)
        {
          _status = value;
          switch (_status)
          {
            case Utils.ImporterStatus.ADDED:
            case Utils.ImporterStatus.WAITING:
            case Utils.ImporterStatus.NONE:
              _selected = -1;
              _progresspercent = 0;
              _progress = string.Empty;
              break;
            case Utils.ImporterStatus.GETTING_IMDB:
              _selected = -1;
              _hasnearest = false;
              lock (((ICollection)_grabbermovies).SyncRoot)
              {
                _grabbermovies.Clear();
              }
              break;
            case Utils.ImporterStatus.COMPLETE:
              _progresspercent = 0;
              _progress = string.Empty;
              break;
          }

          Utils.SendMovieListRefresh();
        }
      }
    }
    private Utils.ImporterStatus _status;

    public IMDBMovie MovieDetails
    {
      get { return _moviedetails; }
      set { _moviedetails = value; }
    }
    private IMDBMovie _moviedetails;

    public IMDBFetcher Fetcher
    {
      get { return _fetcher; }
      set { _fetcher = value; }
    }
    private IMDBFetcher _fetcher;

    public int Selected
    {
      get { return _selected; }
      set
      {
        _selected = value;

        if (_selected > -1 && _selected < _grabbermovies.Count)
        {
          _grabbermovies[_selected].Selected = true;
        }
      }
    }
    private int _selected;

    public bool HasNearest
    {
      get { return _hasnearest; }
    }
    private bool _hasnearest;

    public List<GrabberMovie> GrabberMovies
    {
      get { return _grabbermovies; }
    }
    private List<GrabberMovie> _grabbermovies;

    public string Progress
    {
      get { return _progress; }
    }
    private string _progress;

    public int ProgressPercent
    {
      get { return _progresspercent; }
    }
    private int _progresspercent;

    public bool IsNeedRescan
    {
      get { return (_status == Utils.ImporterStatus.NONE ||
                    _status == Utils.ImporterStatus.WAITING ||
                    _status == Utils.ImporterStatus.ADDED); }
    }

    public bool IsGettingInfo
    {
      get { return (_status == Utils.ImporterStatus.GETTING_IMDB ||
                    _status == Utils.ImporterStatus.GETTING_INFO); }
    }

    public bool IsScanning
    {
      get { return (_status == Utils.ImporterStatus.GETTING_IMDB ||
                    _status == Utils.ImporterStatus.GETTING_INFO ||
                    _status == Utils.ImporterStatus.QUEUED_IMDB ||
                    _status == Utils.ImporterStatus.QUEUED_INFO); }
    }

    public bool IsComplete
    {
      get { return (_status == Utils.ImporterStatus.COMPLETE); }
    }

    public NewMovie()
    {
      _status = Utils.ImporterStatus.NONE;
      _selected = -1;
      _moviedetails = new IMDBMovie();
      _fetcher = new IMDBFetcher(this);
      _grabbermovies = new List<GrabberMovie>();
      _hasnearest = false;

      _moviedetails.ID = -1;
    }

    public NewMovie(string filename) : this()
    {
      _filename = filename;

      _moviedetails.Path = Path.GetDirectoryName(filename);
      _moviedetails.File = Path.GetFileName(filename);
      _moviedetails.SearchString = MediaPortal.Util.Utils.GetFilename(filename, true);
      _title = _moviedetails.SearchString;
      Utils.SendMovieListRefresh();
    }

    #region Fetcher

    public void StartFetch()
    {
      if (Status == Utils.ImporterStatus.COMPLETE)
      {
        return;
      }

      if (!Fetcher.Fetch(MovieDetails.SearchString))
      {
        Status = Utils.ImporterStatus.NONE;
      }
    }

    public void StopFetch()
    {
      if (Status == Utils.ImporterStatus.COMPLETE)
      {
        return;
      }

      Fetcher.CancelFetch();
      Status = Utils.ImporterStatus.NONE;
    }

    public void StartFetchDetails()
    {
      StartFetchDetails(_selected);
    }

    public void StartFetchDetails(int movie)
    {
      if (Status == Utils.ImporterStatus.COMPLETE)
      {
        return;
      }
      if (movie == -1)
      {
        return;
      }

      Status = Utils.ImporterStatus.GETTING_INFO;
      GetInfoFromIMDB(ref _moviedetails, false);
    }

    public void StopFetchhDetails()
    {
      if (Status == Utils.ImporterStatus.COMPLETE)
      {
        return;
      }

      Fetcher.CancelFetchDetails();
      Status = Utils.ImporterStatus.NONE;
    }

    #endregion

    #region IMDB Info

    private void GetInfoFromIMDB(ref IMDBMovie movieDetails, bool fuzzyMatch)
    {
      string file;
      string path = movieDetails.Path;
      string filename = movieDetails.File;

      if (path != string.Empty)
      {
        if (path.EndsWith(@"\"))
        {
          path = path.Substring(0, path.Length - 1);
          movieDetails.Path = path;
        }

        if (filename.StartsWith(@"\"))
        {
          filename = filename.Substring(1);
          movieDetails.File = filename;
        }

        file = path + Path.DirectorySeparatorChar + filename;
      }
      else
      {
        file = filename;
      }

      int id = movieDetails.ID;

      if (id < 0)
      {
        logger.Info("Adding file to Database: {0}", file);
        id = VideoDatabase.AddMovieFile(file);
        VirtualDirectory dir = new VirtualDirectory();
        dir.SetExtensions(MediaPortal.Util.Utils.VideoExtensions);
        List<GUIListItem> items = dir.GetDirectoryUnProtectedExt(path, true);

        foreach (GUIListItem item in items)
        {
          if (item.IsFolder)
          {
            continue;
          }
          if (MediaPortal.Util.Utils.ShouldStack(item.Path, file) && item.Path != file)
          {
            string strPath, strFileName;
            DatabaseUtility.Split(item.Path, out strPath, out strFileName);
            DatabaseUtility.RemoveInvalidChars(ref strPath);
            DatabaseUtility.RemoveInvalidChars(ref strFileName);
            int pathId = VideoDatabase.AddPath(strPath);
            VideoDatabase.AddFile(id, pathId, strFileName);
          }
        }

        movieDetails.ID = id;
        string searchString = movieDetails.SearchString;
        VideoDatabase.SetMovieInfoById(movieDetails.ID, ref movieDetails, true);
        movieDetails.SearchString = searchString;
      }

      /*
      if (IMDBFetcher.RefreshIMDB(this, ref movieDetails, fuzzyMatch, true, true))
      {
        if (movieDetails != null)
        {
          logger.Info("Movie info added:{0}", movieDetails.Title);
        }
      }
      */

      if (!Fetcher.FetchDetails(_selected, ref _moviedetails, true))
      {
        Status = Utils.ImporterStatus.NONE;
      }
    }

    private void CheckIMDBDetails()
    {
      if (_moviedetails.ID == -1 || _moviedetails.IsEmpty)
      {
        VideoDatabase.DeleteMovieInfoById(_moviedetails.ID);
        Status = Utils.ImporterStatus.ERROR;
      }
      else
      {
        Status = Utils.ImporterStatus.COMPLETE;
      }
    }
    #endregion

    #region IMDB.IProgress

    public bool OnDisableCancel(IMDBFetcher fetcher)
    {
      logger.Debug("OnDisableCancel: {0} - {1}", _status, _filename);
      return true;
    }

    public void OnProgress(string line1, string line2, string line3, int percent)
    {
      _progress = line1 + " " + line2 + " " + line3;
      _progress.Trim();
      _progresspercent = percent;
      Utils.SendMovieListRefresh();
    }

    public bool OnSearchStarted(IMDBFetcher fetcher)
    {
      Status = Utils.ImporterStatus.GETTING_IMDB;
      logger.Debug("OnSearchStarted: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnSearchStarting(IMDBFetcher fetcher)
    {
      Status = Utils.ImporterStatus.GETTING_IMDB;
      logger.Debug("OnSearchStarting: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnSearchEnd(IMDBFetcher fetcher)
    {
      _moviedetails.SearchString = MediaPortal.Util.Utils.GetFilename(_moviedetails.File, true);

      int minDistance = int.MaxValue;
      int idxNearest = -1;

      lock (((ICollection)_grabbermovies).SyncRoot)
      {
        if (fetcher.Count > 0)
        {
          for (int i = 0; i < fetcher.Count; ++i)
          {
            GrabberMovie movie = new GrabberMovie(fetcher[i].Title, _title);
            if (movie.Distance < minDistance)
            {
              minDistance = movie.Distance;
              idxNearest = i;
            }
            _grabbermovies.Add(movie);
          }

          if (idxNearest > -1 && idxNearest < _grabbermovies.Count && minDistance <= Utils.NearestFactor)
          {
            _grabbermovies[idxNearest].IsNearest = true;
            _hasnearest = true;
          }
        }
      }

      if (Utils.ApproveIfOne && fetcher.Count == 1)
      {
        _selected = 0;
        Status = Utils.ImporterStatus.QUEUED_INFO;
        logger.Debug("OnSearchEnd - Approve (One): {0} - {1} - {2}", _status, _grabbermovies[_selected].Title, fetcher[_selected].Database);
      }
      else if (Utils.ApproveForNearest && _hasnearest)
      {
        _selected = idxNearest;
        Status = Utils.ImporterStatus.QUEUED_INFO;
        logger.Debug("OnSearchEnd - Approve (Nearest): {0} - {1} - {2} <= {3} - {4}", _status, _grabbermovies[_selected].Title, _grabbermovies[_selected].Distance, Utils.NearestFactor, fetcher[_selected].Database);
      }
      else
      {
        Status = Utils.ImporterStatus.WAITING;
      }
      logger.Debug("OnSearchEnd: {0} [{1}] {2} - {3}", _status, fetcher.Count, fetcher.MovieName, _filename);
      return true;
    }

    public bool OnMovieNotFound(IMDBFetcher fetcher)
    {
      Status = Utils.ImporterStatus.WAITING;
      logger.Debug("OnMovieNotFound: {0} - {1}", _status, _filename);
      return false;
    }

    public bool OnDetailsStarted(IMDBFetcher fetcher)
    {
      Status = Utils.ImporterStatus.GETTING_INFO;
      logger.Debug("OnDetailsStarted: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnDetailsStarting(IMDBFetcher fetcher)
    {
      Status = Utils.ImporterStatus.GETTING_INFO;
      logger.Debug("OnDetailsStarting: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnDetailsEnd(IMDBFetcher fetcher)
    {
      CheckIMDBDetails();
      logger.Debug("OnDetailsEnd: {0} - {1}", _status, _filename);
      Utils.SendMovieRefresh();
      return true;
    }

    public bool OnActorsStarted(IMDBFetcher fetcher)
    {
      logger.Debug("OnActorsStarted: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnActorsStarting(IMDBFetcher fetcher)
    {
      logger.Debug("OnActorsStarting: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnActorInfoStarting(IMDBFetcher fetcher)
    {
      logger.Debug("OnActorInfoStarting: {0} - {1}", _status, _filename);
      return false;
    }

    public bool OnActorsEnd(IMDBFetcher fetcher)
    {
      logger.Debug("OnActorsEnd: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnDetailsNotFound(IMDBFetcher fetcher)
    {
      Status = Utils.ImporterStatus.NONE;
      logger.Debug("OnDetailsNotFound: {0} - {1}", _status, _filename);
      return false;
    }

    public bool OnRequestMovieTitle(IMDBFetcher fetcher, out string movieName)
    {
      movieName = _moviedetails.SearchString;
      logger.Debug("OnRequestMovieTitle: {0} - {1}", _status, _filename);
      return false;
    }

    public bool OnSelectMovie(IMDBFetcher fetcher, out int selected)
    {
      selected = -1;
      logger.Debug("OnSelectMovie: {0} - {1}", _status, _filename);
      return false;
    }

    public bool OnSelectActor(IMDBFetcher fetcher, out int selected)
    {
      selected = -1;
      logger.Debug("OnSelectActor: {0} - {1}", _status, _filename);
      return false;
    }

    public bool OnScanStart(int total)
    {
      logger.Debug("OnScanStart: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnScanEnd()
    {
      logger.Debug("OnScanEnd: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnScanIterating(int count)
    {
      logger.Debug("OnScanIterating: {0} - {1}", _status, _filename);
      return true;
    }

    public bool OnScanIterated(int count)
    {
      logger.Debug("OnScanIterated: {0} - {1}", _status, _filename);
      return true;
    }

    #endregion
  }
}
