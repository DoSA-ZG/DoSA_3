namespace RPPP_WebApp
{
  public class AppSettings
  {
    public int PageSize { get; set; } = 5;
    public int PageOffset { get; set; } = 5;
    public int AutoCompleteCount { get; set; } = 50;

    public ImageSettingsData ImageSettings { get; set; }

    public class ImageSettingsData
    {
      public int ThumbnailHeight { get; set; } = 100;     
    }
  }
}