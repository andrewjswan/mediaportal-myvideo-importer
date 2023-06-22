using Common.GUIPlugins;

using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.Video;
using MediaPortal.Util;
using MediaPortal.Video.Database;

using NLog;

using System.Globalization;
using System.Windows.Forms;
using System.Xml.Serialization;

using Layout = MediaPortal.GUI.Library.GUIFacadeControl.Layout;

namespace MyVideoImporter
{
  [PluginIcons("MyVideoImporter.Resources.MyVideoImporter.png", "MyVideoImporter.Resources.MyVideoImporter_Disabled.png")]
  public class MyVideoImporterGUI : WindowPluginBase, ISetupForm
  {
    #region MapSettings class

    [System.Serializable]
    public class MapSettings
    {
      protected int _SortBy;
      protected int _ViewAs;
      protected bool _SortAscending;

      public MapSettings()
      {
        // Set default view
        _SortBy = 0;
        _ViewAs = (int)Utils.View.List;
        _SortAscending = true;
      }

      [XmlElement("SortBy")]
      public int SortBy
      {
        get { return _SortBy; }
        set { _SortBy = value; }
      }

      [XmlElement("ViewAs")]
      public int ViewAs
      {
        get { return _ViewAs; }
        set { _ViewAs = value; }
      }

      [XmlElement("SortAscending")]
      public bool SortAscending
      {
        get { return _SortAscending; }
        set { _SortAscending = value; }
      }
    }

    #endregion

    #region Variables
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private VideoImporter videoImporter;
    private Utils.ViewLevel viewLevel;
    private MapSettings mapSettings = new MapSettings();
    private GUIListItem parentItem = null;
    private int parentSelectedIndex = 0;
    #endregion

    public MyVideoImporterGUI()
    {
    }

    #region ISetupForm Members

    public string PluginName()
    {
      return "MyVideo Importer";
    }

    public string Description()
    {
      return "MyVideo Movie Importer";
    }

    public string Author()
    {
      return "ajs";
    }

    public void ShowPlugin()
    {
      ((Control) new ConfigForm()).Show();
    }

    public bool CanEnable()
    {
      return true;
    }

    public int GetWindowId()
    {
      // WindowID of windowplugin belonging to this setup
      // enter your own unique code
      return GetID;
    }

    public bool DefaultEnabled()
    {
      return true;
    }

    public bool HasSetup()
    {
      return true;
    }

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = PluginName();
      strButtonImage = string.Empty;
      strButtonImageFocus = string.Empty;
      strPictureImage = "hover_myvideoimporter.png";
      return true;
    }

    // With GetID it will be an window-plugin / otherwise a process-plugin
    // Enter the id number here again
    public override int GetID
    {
      get { return Utils.WindowID; }
      set { }
    }

    #endregion

    #region Skin controls

    [SkinControl(10)] protected GUIButtonControl btnRescan = null;
    [SkinControl(11)] protected GUIButtonControl btnResearch = null;
    [SkinControl(12)] protected GUIButtonControl btnRestart = null;

    #endregion

    #region Properties

    #endregion

    #region Overrides

    protected override bool CurrentSortAsc
    {
      get { return mapSettings.SortAscending; }
      set { mapSettings.SortAscending = value; }
    }

    protected override Layout CurrentLayout
    {
      get { return (Layout)mapSettings.ViewAs; }
      set { mapSettings.ViewAs = (int)value; }
    }

    public override bool Init()
    {
      Start();

      bool load = Load(GUIGraphicsContext.GetThemedSkinFile(@"\MyVideoImporter.xml"));

      return load;
    }

    public override void DeInit()
    {
      base.DeInit();

      Stop();
    }

    public override void OnAdded()
    {
      base.OnAdded();
    }

    protected override void OnPageLoad()
    {
      ClearProperties();
      LoadList();
      base.OnPageLoad();

      if (null == btnRescan)
      {
        btnRescan.Label = Translation.Rescan;
      }
      if (null == btnResearch)
      {
        btnResearch.Label = Translation.Research;
      }
      else if (null == btnRestart)
      {
        btnRestart.Label = Translation.Restart;
      }
    }

    protected override void OnPageDestroy(int newWindowId)
    {
      parentSelectedIndex = facadeLayout.SelectedListItemIndex;
      base.OnPageDestroy(newWindowId);
    }

    protected override string SerializeName
    {
      get { return "myvideoimporter"; }
    }

    protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
    {
      if (control == null)
      {
        return;
      }

      base.OnClicked(controlId, control, actionType);

      // button clicks
      if (control == btnRescan)
      {
        videoImporter.RescanFiles();
      }
      if (control == btnResearch)
      {
        videoImporter.RescanIMDB();
      }
      else if (control == btnRestart)
      {
        videoImporter.RestartImporter();
      }
    }

    protected override void OnClick(int iItem)
    {
      GUIListItem item = GetSelectedItem();
      if (item == null)
      {
        return;
      }

      Utils.ItemType _itemType;
      if (item.TVTag == null)
      {
        return;
      }

      _itemType = (Utils.ItemType)item.TVTag;
      if (_itemType == Utils.ItemType.Rescan)
      {
        item.Label = Translation.IMDBScanning;
        item.TVTag = Utils.ItemType.Search;
        SetStatus(ref item);
        videoImporter.RestartImporter();
        return;
      }

      if (item.IsFolder)
      {
        LoadList(item);
      }
      else
      {
        if (_itemType == Utils.ItemType.Research)
        {
          NewMovie movie = parentItem.AlbumInfoTag as NewMovie;
          if (movie != null)
          {
            movie.Status = Utils.ImporterStatus.QUEUED_IMDB;
            item.Label = Translation.IMDBScanning;
            item.TVTag = Utils.ItemType.Search;
            SetStatus(ref item);
          }
        }
        /*
        else if (_itemType == Utils.ItemType.Stop)
        {
          NewMovie movie = parentItem.AlbumInfoTag as NewMovie;
          if (movie != null)
          {
            movie.StopFetchhDetails();
            movie.Status = Utils.ImporterStatus.WAITING;
          }
        }
        */
        else
        {
          NewMovie movie = item.AlbumInfoTag as NewMovie;
          if (movie != null)
          {
            if (movie.Status == Utils.ImporterStatus.COMPLETE)
            {
              ShowInfo(item);
            }
            else if (SelectYesNo(item.Label))
            {
              movie.Selected = item.ItemId;
              movie.Status = Utils.ImporterStatus.QUEUED_INFO;
              SetStatus(ref item, movie);
            }
          }
        }
      }
    }

    public override void OnAction(Action action)
    {
      if (viewLevel == Utils.ViewLevel.Movies)
      {
        if (action.wID == Action.ActionType.ACTION_PREVIOUS_MENU)
        {
          if (facadeLayout.Focus)
          {
            LoadList(null);
            return;
          }
        }
        if (action.wID == Action.ActionType.ACTION_PARENT_DIR)
        {
          LoadList(null);
          return;
        }
      }

      base.OnAction(action);
    }

    protected override void InitViewSelections()
    {
      btnViews.ClearMenu();
    }

    protected override void OnShowContextMenu()
    {
      GUIListItem item = GetSelectedItem();
      if (item == null)
      {
        return;
      }

      NewMovie movie = item.AlbumInfoTag as NewMovie;
      if (movie == null)
      {
        return;
      }

      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      
      if (dlg == null)
      {
        return;
      }

      dlg.Reset();
      dlg.SetHeading(498); // Menu

      dlg.AddLocalizedString(6032); // Search by title
      if (viewLevel == Utils.ViewLevel.Files)
      {
      }
      else if (movie.Status == Utils.ImporterStatus.COMPLETE)
      {
        dlg.AddLocalizedString(368); //IMDB      
      }

      dlg.DoModal(GetID);

      if (dlg.SelectedId == -1)
      {
        return;
      }

      switch (dlg.SelectedId)
      {
        case 6032: // Search by title 
          string moviename = movie.SearchTitle;
          if (moviename == "unknown")
          {
            moviename = MediaPortal.Util.Utils.GetFilename(movie.FileName, true);
          }
          if (VirtualKeyboard.GetKeyboard(ref moviename, GetID))
          {
            if (!string.IsNullOrEmpty(moviename))
            {
              movie.SearchTitle = moviename;
              movie.Status = Utils.ImporterStatus.QUEUED_IMDB;
              if (viewLevel == Utils.ViewLevel.Files)
              {
                SetStatus(ref item, movie);
              }
              else
              {
                item.Label = Translation.IMDBScanning;
                item.TVTag = Utils.ItemType.Search;
                SetStatus(ref item);
              }
            }
          }
          break;
        case 368: // IMDB
          ShowInfo(item);
          break;
      }
    }

    #endregion

    public void Start()
    {
      try
      {
        Utils.InitLogger();

        logger.Info("MyVideo Importer is starting...");
        logger.Info("MyVideo Importer version is " + Utils.GetAllVersionNumber());

        GUIWindowManager.Receivers += new SendMessageHandler(GUIWindowManager_OnNewMessage);
        GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GuiWindowManager_OnActivateWindow);

        Translation.Init();
        Utils.LoadSettings();

        videoImporter = new VideoImporter();

        logger.Info("MyVideo Importer is started.");
      }
      catch { }
    }

    public void Stop()
    {
      try
      {
        GUIWindowManager.OnActivateWindow += new GUIWindowManager.WindowActivationHandler(GuiWindowManager_OnActivateWindow);
        GUIWindowManager.Receivers -= new SendMessageHandler(GUIWindowManager_OnNewMessage);

        videoImporter.Stop();
        logger.Info("MyVideo Importer: Background tasks stopped.");
        logger.Info("MyVideo Importer is stopped.");
      }
      catch { }
    }

    private void LoadList()
    {
      viewLevel = Utils.ViewLevel.Files;

      LoadList(null);
    }

    private void LoadList(GUIListItem item)
    {
      if (facadeLayout == null)
      {
        return;
      }

      if (videoImporter == null)
      {
        return;
      }

      int newSelectedIndex = parentSelectedIndex;
      int oldSelectedIndex = facadeLayout.SelectedListItemIndex;
      int count = 1;
      string module = Translation.Base;

      GUIListItem _item = null;
      facadeLayout.CurrentLayout = CurrentLayout;
      facadeLayout.Clear();

      if (item == null) // Files
      {
        viewLevel = Utils.ViewLevel.Files;
        parentItem = null;

        if (videoImporter.MovieList.Count > 0)
        {
          foreach (NewMovie movie in videoImporter.MovieList)
          {
            _item = new GUIListItem();
            _item.Label = movie.Title;
            _item.Label2 = Utils.GetStatus(movie.Status);
            _item.TVTag = Utils.ItemType.File;
            _item.AlbumInfoTag = movie;
            _item.IsFolder = true;
            _item.OnItemSelected += OnItemSelected;
            SetStatus(ref _item, movie);
            facadeLayout.Add(_item);
          }
          count = videoImporter.MovieList.Count;
        }
        else
        {
          _item = new GUIListItem();
          if (videoImporter.IsScanning)
          {
            _item.Label = Translation.IMDBScanning;
            _item.TVTag = Utils.ItemType.Search;
          }
          else
          {
            _item.Label = Translation.FilesNotFound;
            _item.TVTag = Utils.ItemType.Rescan;
          }
          _item.Label2 = string.Empty;
          _item.IsFolder = true;
          _item.OnItemSelected += OnItemSelected;
          SetStatus(ref _item);
          facadeLayout.Add(_item);
        }
      }
      else
      {
        // module = module + ": " + item.Label;
        // module = item.Label;
        module = Translation.Movie;

        viewLevel = Utils.ViewLevel.Movies;
        parentItem = item;
        parentSelectedIndex = oldSelectedIndex;
        newSelectedIndex = 0;

        NewMovie newmovie = item.AlbumInfoTag as NewMovie;
        if (newmovie != null)
        {
          if (newmovie.GrabberMovies.Count > 0)
          {
            for (int i = 0; i < newmovie.GrabberMovies.Count; ++i)
            {
              _item = new GUIListItem();
              _item.ItemId = i;
              _item.Label = newmovie.GrabberMovies[i].Title;
              _item.Label2 = newmovie.Fetcher[i].Database;
              _item.TVTag = Utils.ItemType.IMDB;
              _item.AlbumInfoTag = newmovie;
              _item.OnItemSelected += OnItemSelected;
              SetStatus(ref _item, newmovie);
              facadeLayout.Add(_item);
            }
            count = newmovie.GrabberMovies.Count;
            /*
            if (newmovie.IsScanning)
            {
              _item = new GUIListItem();
              _item.ItemId = -1;
              _item.Label = Translation.Stop;
              _item.Label2 = string.Empty;
              _item.TVTag = Utils.ItemType.Stop;
              _item.AlbumInfoTag = newmovie;
              _item.OnItemSelected += OnItemSelected;
              SetStatus(ref _item);
              facadeLayout.Add(_item);
            }
            */
          }
          else
          {
            _item = new GUIListItem();
            if (newmovie.IsScanning)
            {
              _item.Label = Translation.IMDBScanning;
              _item.TVTag = Utils.ItemType.Search;
            }
            else
            {
              _item.Label = Translation.IMDBNotFound;
              _item.TVTag = Utils.ItemType.Research;
            }
            _item.Label2 = string.Empty;
            _item.OnItemSelected += OnItemSelected;
            _item.AlbumInfoTag = newmovie;
            SetStatus(ref _item, newmovie);
            facadeLayout.Add(_item);
          }
        }
        else
        {
          _item = new GUIListItem();
          _item.Label = Translation.IMDBNotFound;
          _item.Label2 = string.Empty;
          _item.TVTag = Utils.ItemType.Research;
          _item.OnItemSelected += OnItemSelected;
          SetStatus(ref _item);
          facadeLayout.Add(_item);
        }
      }
      facadeLayout.SelectedListItemIndex = newSelectedIndex;
      FillProperties(GetSelectedItem());

      Utils.SetProperty("#currentmodule", module);
      Utils.SetProperty("#itemcount", count.ToString());
    }

    private void UpdateList()
    {
      if (facadeLayout == null)
      {
        return;
      }

      if (videoImporter == null)
      {
        return;
      }

      if (facadeLayout.Count != 0)
      {
        if (facadeLayout[0].TVTag == null)
        {
          return;
        }
        Utils.ItemType _itemType = (Utils.ItemType)facadeLayout[0].TVTag;
        if (_itemType == Utils.ItemType.Rescan)
        {
          LoadList(null);
          return;
        }
        if (_itemType == Utils.ItemType.Research)
        {
          LoadList(parentItem);
          return;
        }
      }

      if (viewLevel == Utils.ViewLevel.Files)
      {
        UpdateFiles();
      }
      if (viewLevel == Utils.ViewLevel.Movies)
      {
        UpdateMovies();
      }
    }

    private void UpdateFiles()
    {
      if (facadeLayout == null)
      {
        return;
      }

      if (videoImporter == null)
      {
        return;
      }

      if (facadeLayout.Count != videoImporter.MovieList.Count)
      {
        LoadList(null);
        return;
      }

      if (videoImporter.MovieList.Count > 0)
      {
        for (int i = 0; i < facadeLayout.Count; ++i)
        {
          GUIListItem item = facadeLayout[i];
          NewMovie movie = videoImporter.MovieList[i]; // item.AlbumInfoTag as NewMovie;
          item.Label = movie.Title;
          item.Label2 = Utils.GetStatus(movie.Status);
          item.AlbumInfoTag = movie;
          SetStatus(ref item, movie);
        }
      }
      else
      {
        GUIListItem _item = GetSelectedItem();
        if (_item == null)
        {
          return;
        }

        if (videoImporter.IsScanning)
        {
          _item.Label = Translation.IMDBScanning;
          _item.TVTag = Utils.ItemType.Search;
        }
        else
        {
          _item.Label = Translation.FilesNotFound;
          _item.TVTag = Utils.ItemType.Rescan;
        }
        _item.Label2 = string.Empty;
        _item.IsFolder = true;
        SetStatus(ref _item);
      }
      FillProperties(GetSelectedItem());
    }

    private void UpdateMovies()
    {
      if (facadeLayout == null)
      {
        return;
      }

      if (videoImporter == null)
      {
        return;
      }
      if (videoImporter.MovieList == null)
      {
        return;
      }

      GUIListItem item = GetSelectedItem();
      if (facadeLayout.Count != videoImporter.MovieList.Count)
      {
        LoadList(item);
        return;
      }

      if (item == null)
      {
        return;
      }

      NewMovie newmovie = item.AlbumInfoTag as NewMovie;
      if (newmovie != null)
      {
        if (newmovie.GrabberMovies != null && newmovie.GrabberMovies.Count > 0)
        {
          for (int i = 0; i < newmovie.GrabberMovies.Count; ++i)
          {
            GUIListItem _item = facadeLayout[i];
            _item.Label = newmovie.GrabberMovies[i].Title;
            _item.Label2 = newmovie.Fetcher[i].Database;
            _item.TVTag = Utils.ItemType.IMDB;
            SetStatus(ref _item, newmovie);
          }
        }
        else
        {
          if (newmovie.IsScanning)
          {
            item.Label = Translation.IMDBScanning;
            item.Label2 = string.Empty;
            item.TVTag = Utils.ItemType.Search;
          }
          else
          {
            item.Label = Translation.IMDBNotFound;
            item.Label2 = string.Empty;
            item.TVTag = Utils.ItemType.Research;
          }
          SetStatus(ref item, newmovie);
        }
      }
      else
      {
        item.Label = Translation.IMDBNotFound;
        item.Label2 = string.Empty;
        item.TVTag = Utils.ItemType.Research;
        SetStatus(ref item);
      }
    }

    private void SetStatus(ref GUIListItem item)
    {
      if (item == null)
      {
        return;
      }

      Utils.ItemType _itemType;
      if (item.TVTag == null)
      {
        item.IconImage = Utils.GetIcon(Utils.ImporterStatus.NONE);
        item.IconImageBig = item.IconImage;
        item.ThumbnailImage = item.IconImage;
        return;
      }

      _itemType = (Utils.ItemType)item.TVTag;
      if (_itemType == Utils.ItemType.Rescan || _itemType == Utils.ItemType.Research)
      {
        item.IconImage = Utils.GetIcon(Utils.ImporterStatus.WAITING);
      }
      else if (_itemType == Utils.ItemType.Search)
      {
        item.IconImage = Utils.GetIcon(Utils.ImporterStatus.GETTING_IMDB);
      }
      else if (_itemType == Utils.ItemType.Stop)
      {
        item.IconImage = Utils.GetIcon(Utils.ImporterStatus.NONE);
      }
      else
      {
        item.IconImage = Utils.GetIcon(Utils.ImporterStatus.NONE);
      }
      item.IconImageBig = item.IconImage;
      item.ThumbnailImage = item.IconImage;
    }

    private void SetStatus(ref GUIListItem item, NewMovie movie)
    {
      if (movie == null)
      {
        SetStatus(ref item);
        return;
      }

      // Movie items ...
      if (item == null)
      {
        return;
      }

      // Complete
      item.IsPlayed = (movie.Status == Utils.ImporterStatus.COMPLETE);

      // Download
      if (viewLevel == Utils.ViewLevel.Movies)
      {
        if (movie.Selected == item.ItemId)
        {
          item.IsDownloading = !(movie.Status == Utils.ImporterStatus.COMPLETE);
        }
        else
        {
          item.IsDownloading = false;
        }
      }

      // Progress
      if (movie.ProgressPercent > 0)
      {
        if (viewLevel == Utils.ViewLevel.Movies)
        {
          if (movie.Selected == item.ItemId)
          {
            item.HasProgressBar = true;
            item.ProgressBarPercentage = movie.ProgressPercent;
          }
          else
          {
            item.HasProgressBar = false;
          }
        }
        else
        {
          item.HasProgressBar = true;
          item.ProgressBarPercentage = movie.ProgressPercent;
        }
      }
      else
      {
        item.HasProgressBar = false;
      }

      // Icon
      if (viewLevel == Utils.ViewLevel.Movies && (movie.IsScanning || movie.IsComplete))
      {
        if (movie.Selected == item.ItemId)
        {
          item.IconImage = Utils.GetIcon(movie.Status);
        }
        else
        {
          item.IconImage = Utils.GetIcon(Utils.ImporterStatus.SKIP);
        }
      }
      else
      {
        item.IconImage = Utils.GetIcon(movie.Status);
      }
      item.IconImageBig = item.IconImage;
      item.ThumbnailImage = item.IconImage;
    }

    private GUIListItem GetSelectedItem()
    {
      return facadeLayout.SelectedListItem;
    }

    private void GuiWindowManager_OnActivateWindow(int activeWindowId)
    {
      try
      {
        if (!videoImporter.IsStarted)
        {
          videoImporter.Start();
          logger.Info("MyVideo Importer: Background tasks started.");
        }
      }
      catch (System.Exception ex)
      {
        logger.Error("OnActivateWindow: " + ex);
      }
    }

    private void OnItemSelected(GUIListItem item, GUIControl parent)
    {
      ClearProperties();

      if (item == null)
      {
        return;
      }

      FillProperties(item);
    }

    private void FillProperties(GUIListItem item)
    {
      if (item == null)
      {
        ClearProperties();
        return;
      }

      NewMovie movie = null;
      if (item.AlbumInfoTag != null)
      {
        movie = item.AlbumInfoTag as NewMovie;
        Utils.SetProperty("#importer.title", movie.Title);
        Utils.SetProperty("#importer.file", movie.MovieDetails.File);
        Utils.SetProperty("#importer.path", movie.MovieDetails.Path);
        Utils.SetProperty("#importer.status", movie.Status.ToString().ToLowerInvariant());
        Utils.SetProperty("#importer.textstatus", Utils.GetStatus(movie.Status));
        Utils.SetProperty("#importer.progress", movie.Progress);
        Utils.SetProperty("#importer.view", viewLevel.ToString().ToLowerInvariant());
        Utils.SetProperty("#importer.hasnearest", movie.HasNearest ? "true" : "false");
        if (movie.Selected > -1 && movie.Selected < movie.GrabberMovies.Count)
        {
          Utils.SetProperty("#importer.selected", movie.GrabberMovies[movie.Selected].Title);
        }
        FillMovieProperties(movie.MovieDetails);
      }

      if (viewLevel == Utils.ViewLevel.Movies)
      {
        FillGrabberProperties(ref item, movie);
      }
    }

    private void FillMovieProperties(IMDBMovie movie)
    {
      if (movie == null || movie.ID == -1 || movie.IsEmpty)
      {
        ClearMovieProperties();
        return;
      }

      string strThumb = MediaPortal.Util.Utils.GetLargeCoverArtName(Thumbs.MovieTitle, movie.Title + "{" + movie.ID + "}");
      System.Int32 votes = 0;
      string strVotes = string.Empty;
      if (!string.IsNullOrEmpty(movie.Votes) && System.Int32.TryParse(movie.Votes.Replace(".", string.Empty).Replace(",", string.Empty), out votes))
      {
        strVotes = System.String.Format("{0:N0}", votes);
      }

      Utils.SetProperty("#movieid", movie.ID.ToString());
      Utils.SetProperty("#title", movie.Title);
      Utils.SetProperty("#imdbnumber", movie.IMDBNumber);
      Utils.SetProperty("#director", movie.Director);
      Utils.SetProperty("#thumb", strThumb);
      Utils.SetProperty("#genre", !string.IsNullOrEmpty(movie.Genre) ? movie.Genre.Replace(" /", ",") : string.Empty);
      Utils.SetProperty("#plotoutline", movie.Plot);
      Utils.SetProperty("#plotoutline", movie.PlotOutline);
      Utils.SetProperty("#rating", movie.Rating.ToString());
      Utils.SetProperty("#strrating", movie.Rating.ToString(CultureInfo.CurrentCulture) + "/10");
      Utils.SetProperty("#tagline", movie.TagLine);
      Utils.SetProperty("#votes", strVotes);
      Utils.SetProperty("#credits", !string.IsNullOrEmpty(movie.WritingCredits) ? movie.WritingCredits.Replace(" /", ",") : string.Empty);
      Utils.SetProperty("#year", ((movie.Year <= 1900) ? string.Empty : movie.Year.ToString()));
      Utils.SetProperty("#mpaarating", MediaPortal.Util.Utils.MakeFileName(movie.MPARating));
      Utils.SetProperty("#mpaatext", movie.MPAAText);
      Utils.SetProperty("#studios", !string.IsNullOrEmpty(movie.Studios) ? movie.Studios.Replace(" /", ",") : string.Empty);
      Utils.SetProperty("#country", movie.Country);
      Utils.SetProperty("#language", movie.Language);
      Utils.SetProperty("#tmdbnumber", movie.TMDBNumber);
      Utils.SetProperty("#localdbnumber", movie.LocalDBNumber);
      Utils.SetProperty("#moviecollection", !string.IsNullOrEmpty(movie.MovieCollection) ? movie.MovieCollection.Replace(" /", ",") : string.Empty);
      Utils.SetProperty("#usergroups", !string.IsNullOrEmpty(movie.UserGroup) ? movie.UserGroup.Replace(" /", ",") : string.Empty);
      Utils.SetProperty("#moviepath", movie.Path);
      Utils.SetProperty("#awards", movie.MovieAwards);

      Utils.SetProperty("#importer.hasmediainfo", "true");
    }

    private void FillGrabberProperties(ref GUIListItem item, NewMovie movie)
    {
      if (item == null)
      {
         return;
      }
      if (movie == null)
      {
         return;
      }

      if (item.ItemId > -1 && item.ItemId < movie.GrabberMovies.Count)
      {
        Utils.SetProperty("#importer.grabber.title", movie.GrabberMovies[item.ItemId].GrabberTitle);
        Utils.SetProperty("#importer.grabber.year", movie.GrabberMovies[item.ItemId].GrabberYear);
        Utils.SetProperty("#importer.grabber.imdbtitle", movie.GrabberMovies[item.ItemId].GrabberIMDBTitle);
        Utils.SetProperty("#importer.grabber.imdbyear", movie.GrabberMovies[item.ItemId].GrabberIMDBYear);
        Utils.SetProperty("#importer.grabber.imdbid", movie.GrabberMovies[item.ItemId].GrabberIMDBId);
        Utils.SetProperty("#importer.grabber.distance", movie.GrabberMovies[item.ItemId].Distance.ToString());
        Utils.SetProperty("#importer.grabber.nearest", movie.GrabberMovies[item.ItemId].IsNearest ? "true" : "false");
        Utils.SetProperty("#importer.grabber.equals", movie.GrabberMovies[item.ItemId].IsEquals ? "true" : "false");
      } 
    }

    private void ClearProperties()
    {
      Utils.SetProperty("#importer.title", string.Empty);
      Utils.SetProperty("#importer.file", string.Empty);
      Utils.SetProperty("#importer.path", string.Empty);
      Utils.SetProperty("#importer.status", string.Empty);
      Utils.SetProperty("#importer.textstatus", string.Empty);
      Utils.SetProperty("#importer.progress", string.Empty);
      Utils.SetProperty("#importer.view", string.Empty);
      Utils.SetProperty("#importer.hasnearest", string.Empty);
      Utils.SetProperty("#importer.selected", string.Empty);

      ClearMovieProperties();
      ClearGrabberProperties();
    }

    private void ClearMovieProperties()
    {
      Utils.SetProperty("#importer.hasmediainfo", "false");

      Utils.SetProperty("#movieid", string.Empty);
      Utils.SetProperty("#title", string.Empty);
      Utils.SetProperty("#imdbnumber", string.Empty);
      Utils.SetProperty("#director", string.Empty);
      Utils.SetProperty("#thumb", string.Empty);
      Utils.SetProperty("#genre", string.Empty);
      Utils.SetProperty("#plot", string.Empty);
      Utils.SetProperty("#plotoutline", string.Empty);
      Utils.SetProperty("#rating", string.Empty);
      Utils.SetProperty("#strrating", string.Empty);
      Utils.SetProperty("#tagline", string.Empty);
      Utils.SetProperty("#votes", string.Empty);
      Utils.SetProperty("#credits", string.Empty);
      Utils.SetProperty("#year", string.Empty);
      Utils.SetProperty("#mpaarating", string.Empty);
      Utils.SetProperty("#mpaatext", string.Empty);
      Utils.SetProperty("#studios", string.Empty);
      Utils.SetProperty("#country", string.Empty);
      Utils.SetProperty("#language", string.Empty);
      Utils.SetProperty("#tmdbnumber", string.Empty);
      Utils.SetProperty("#localdbnumber", string.Empty);
      Utils.SetProperty("#moviecollection", string.Empty);
      Utils.SetProperty("#usergroups", string.Empty);
      Utils.SetProperty("#moviepath", string.Empty);
      Utils.SetProperty("#awards", string.Empty);
    }

    private void ClearGrabberProperties()
    {
      Utils.SetProperty("#importer.grabber.title", string.Empty);
      Utils.SetProperty("#importer.grabber.year", string.Empty);
      Utils.SetProperty("#importer.grabber.imdbtitle", string.Empty);
      Utils.SetProperty("#importer.grabber.imdbyear", string.Empty);
      Utils.SetProperty("#importer.grabber.imdbid", string.Empty);
      Utils.SetProperty("#importer.grabber.distance", string.Empty);
      Utils.SetProperty("#importer.grabber.nearest", string.Empty);
      Utils.SetProperty("#importer.grabber.equals", string.Empty);
    }

    private void ShowInfo(GUIListItem item)
    {
      if (item == null)
      {
        return;
      }

      try
      {
        NewMovie newmovie = item.AlbumInfoTag as NewMovie;
        if (newmovie != null)
        {
          if (newmovie.MovieDetails != null)
          {
            IMDBMovie movie = (IMDBMovie) newmovie.MovieDetails;
            // Open video info screen
            GUIVideoInfo videoInfo = (GUIVideoInfo) GUIWindowManager.GetWindow((int) GUIWindow.Window.WINDOW_VIDEO_INFO);
            videoInfo.Movie = movie;

            GUIWindowManager.ActivateWindow((int) GUIWindow.Window.WINDOW_VIDEO_INFO);
          }
        }
      }
      catch (System.Exception ex)
      {
        logger.Error("ShowInfo: " + ex.ToString());
      }
    }

    private bool SelectYesNo(string label)
    {
      GUIDialogYesNo dlg = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      if (dlg == null)
      {
        return false;
      }

      dlg.SetHeading(196);
      dlg.SetLine(1, label);
      dlg.SetYesLabel(GUILocalizeStrings.Get(186)); //OK
      dlg.SetNoLabel(GUILocalizeStrings.Get(222)); //Cancel
      dlg.SetDefaultToYes(true);
      dlg.DoModal(GUIWindowManager.ActiveWindow);

      return dlg.IsConfirmed;
    }

    private void GUIWindowManager_OnNewMessage(GUIMessage message)
    {
      System.Threading.ThreadPool.QueueUserWorkItem(delegate { OnMessageTasks(message); }, null);
    }

    private void OnMessageTasks(GUIMessage message)
    {
      bool Update = false;

      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_VIDEOINFO_REFRESH:
          {
            logger.Debug("VideoInfo refresh detected: Refreshing filelist...");
            Update = true;
            break;
          }
        case GUIMessage.MessageType.GUI_MSG_REFRESH:
          {
            // logger.Debug("Refresh message detected: Refreshing filelist...");
            Update = true;
            break;
          }
      }

      if (Update)
      {
        UpdateList();
      }
    }
  }
}
