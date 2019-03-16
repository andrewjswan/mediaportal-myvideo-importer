using MediaPortal.Util;

using NLog;

using System.Text.RegularExpressions;

namespace MyVideoImporter
{
  public class GrabberMovie
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();

    public string Title
    {
      get { return _title; }
    }
    private string _title = string.Empty;
    
    public string GrabberTitle
    {
      get { return _grabbertitle; }
    }
    private string _grabbertitle = string.Empty;

    public string GrabberYear
    {
      get { return _grabberyear; }
    }
    private string _grabberyear = string.Empty;

    public string GrabberIMDBTitle
    {
      get { return _grabberimdbtitle; }
    }
    private string _grabberimdbtitle = string.Empty;

    public string GrabberIMDBYear
    {
      get { return _grabberimdbyear; }
    }
    private string _grabberimdbyear = string.Empty;

    public string GrabberIMDBId
    {
      get { return _grabberimdbid; }
    }
    private string _grabberimdbid = string.Empty;

    public bool Selected
    {
      get { return _selected; }
      set { _selected = value; }
    }
    private bool _selected = false;

    public bool IsNearest
    {
      get { return _isnearest; }
      set { _isnearest = value; }
    }
    private bool _isnearest = false;

    public int Distance
    {
      get { return _distance; }
    }
    private int _distance = int.MaxValue;

    public bool IsEquals
    {
      get { return _equals; }
    }
    private bool _equals = false;

    public GrabberMovie()
    {      
    }

    public GrabberMovie(string title) : this()
    {
      if (string.IsNullOrEmpty(title))
      {
        return;
      }
      _title = Regex.Replace(title, @"\t|\n|\r", string.Empty).Trim();

      string regexPattern = @"^(?<title>.+?)(?:\s\((?<year>\d{4})\))?(?:\sIMDB:\s(?<imdbtitle>.+?)\s(?:\((?<imdbyear>\d{4})\))\s-\s(?<imdbid>tt\d{7}))?$";
      Match match = Regex.Match(_title, regexPattern, RegexOptions.IgnoreCase);
      if (match.Success)
      {
        _grabbertitle = match.Groups["title"].Value;
        _grabberyear = match.Groups["year"].Value;
        _grabberimdbtitle = match.Groups["imdbtitle"].Value;
        _grabberimdbyear = match.Groups["imdbyear"].Value;
        _grabberimdbid = match.Groups["imdbid"].Value;
      }
      logger.Debug("Added: {0}: {1} ({2}) - {3} ({4}) {5}", _title, _grabbertitle, _grabberyear, _grabberimdbtitle, _grabberimdbyear, _grabberimdbid);
    }

    public GrabberMovie(string title, string movietitle) : this(title)
    {
      if (string.IsNullOrEmpty(title))
      {
        return;
      }
      if (string.IsNullOrEmpty(movietitle))
      {
        return;
      }

      string fuzzytitle = _grabbertitle.Replace(" / ", string.Empty).Replace(" - ", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
      string fuzzymovie = movietitle.Replace(" / ", string.Empty).Replace(" - ", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
      if (!string.IsNullOrEmpty(_grabberyear))
      {
        fuzzytitle = fuzzytitle + "("+_grabberyear+")";
      }
      _distance = Levenshtein.Match(fuzzytitle, fuzzymovie);
      _equals = fuzzytitle.Equals(fuzzymovie);

      logger.Debug("Added: {0}: Equals: {1}, Nearest factor: {2} [{3}][{4}]", _title, _equals, _distance, fuzzytitle, fuzzymovie);
    }
  }
}
