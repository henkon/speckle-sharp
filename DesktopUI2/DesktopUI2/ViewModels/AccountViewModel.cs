using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia.Media.Imaging;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Stream = System.IO.Stream;

namespace DesktopUI2.ViewModels;

public class AccountViewModel : ReactiveObject
{
  private Bitmap _avatarImage;

  public string _avatarUrl = "";

  private bool _firstDownload = true;

  private string _role = "contributor";

  public AccountViewModel() { }

  public AccountViewModel(Account account)
  {
    Name = account.userInfo.name;
    Id = account.userInfo.id;
    AvatarUrl = account.userInfo.avatar;
    Account = account;
  }

  public AccountViewModel(UserBase user)
  {
    Name = user.name;
    Id = user.id;
    AvatarUrl = user.avatar;
  }

  public AccountViewModel(Collaborator user)
  {
    Name = user.name;
    Id = user.id;
    AvatarUrl = user.avatar;
    Role = user.role;
  }

  public AccountViewModel(PendingStreamCollaborator user)
  {
    Name = user.title;
    Id = user.id;
    AvatarUrl = user?.user?.avatar;
    Role = user.role;
    Pending = true;
  }

  public string Name { get; set; }

  public string Role
  {
    get => _role;
    set => this.RaiseAndSetIfChanged(ref _role, value);
  }

  public string Id { get; }
  public bool Pending { get; }

  public Account Account { get; private set; }

  private Client _client { get; set; }

  public Client Client
  {
    get
    {
      if (_client == null)
        _client = new Client(Account);
      return _client;
    }
  }

  public string SimpleName
  {
    get
    {
      if (HomeViewModel.Instance.Accounts.Any(x => x.Account.userInfo.id == Id))
        return "You";
      return Name;
    }
  }

  public string FullAccountName => $"{Name} ({Account.userInfo.email})";

  public string FullServerName => $"{Account.serverInfo.name} ({Account.serverInfo.url})";

  public string AvatarUrl
  {
    get => _avatarUrl;
    set
    {
      //if the user manually uploaded their avatar it'll be a base64 string of the image
      //otherwise if linked the account eg via google, it'll be a link
      if (value != null && value.StartsWith("data:"))
        try
        {
          SetImage(Convert.FromBase64String(value.Split(',')[1]));
          return;
        }
        catch
        {
          value = null;
        }

      // only use robohas if it's the first attempt
      // otherwise it'll end up in a loop
      if (value == null && Id != null && _firstDownload)
        this.RaiseAndSetIfChanged(ref _avatarUrl, $"https://robohash.org/{Id}.png?size=28x28");
      else
        this.RaiseAndSetIfChanged(ref _avatarUrl, value);
      DownloadImage(AvatarUrl);
    }
  }

  public Bitmap AvatarImage
  {
    get => _avatarImage;
    set => this.RaiseAndSetIfChanged(ref _avatarImage, value);
  }

  public async void DownloadImage(string url)
  {
    if (string.IsNullOrEmpty(url))
      return;

    _firstDownload = false;

    using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
    {
      HttpClient client = Http.GetHttpProxyClient();
      //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "Bearer " + Account.token);
      var result = await client.SendAsync(request).ConfigureAwait(true);

      if (!result.IsSuccessStatusCode)
      {
        SpeckleLog.Logger
          .ForContext("imageUrl", url)
          .Warning("{methodName} failed with code: {exceptionMessage}", nameof(DownloadImage), result.StatusCode);
        AvatarUrl = null; // Could not download...
        return;
      }

      try
      {
        var bytes = await result.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
        SetImage(bytes);
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger
          .ForContext("imageUrl", url)
          .Warning(ex, "Swallowing exception in {methodName}: {exceptionMessage}", nameof(DownloadImage), ex.Message);
        AvatarUrl = null; // Could not download...
      }
    }
  }

  private void SetImage(byte[] bytes)
  {
    var resizedBytes = Utils.ResizeImage(bytes, 28, 28);
    Stream stream = new MemoryStream(resizedBytes);
    AvatarImage = new Bitmap(stream);
    this.RaisePropertyChanged(nameof(AvatarImage));
  }

  public async void DownloadImage(Uri url)
  {
    throw new NotImplementedException();
  }
}
