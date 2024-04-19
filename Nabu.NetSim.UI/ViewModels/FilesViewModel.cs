using Blazorise;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Nabu.Logs;
using Nabu.NetSim.UI.Models;
using Nabu.Network;
using Nabu.Settings;
using ReactiveUI;
using System.IO.Compression;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public enum AlertState
{
    Info,
    Warning,
    Error
}

public class FilesViewModel : ReactiveObject, IActivatableViewModel
{
    public string RootPath { get; set; } = string.Empty;
    public string CurrentPath { get; set; } = string.Empty;

    public List<FileModel> Files { get; set; } = new();
    public List<DirectoryModel> Directories { get; set; } = new();
    private ILogger<FilesViewModel> Logger { get; }
    public ViewModelActivator Activator { get; } = new();
    public HomeViewModel Home { get; }
    public GlobalSettings Settings { get; }    public LocationService Location { get; }    public FilesViewModel(        ILogger<FilesViewModel> logger,         HomeViewModel home,         GlobalSettings settings,         LocationService location)
    {
        Logger = logger;
        Home = home;
        Settings = settings;        Location = location;        this.WhenActivated(
            disposables =>
            {
                var whenPageChanged = Home.WhenAnyValue(h => h.VisiblePage);
                whenPageChanged.Subscribe(_ => NotifyChange());
                whenPageChanged.Where(p => p is not VisiblePage.AdaptorSettings or VisiblePage.Files)
                    .Subscribe(
                        p =>
                        {
                            if (RootPath != string.Empty)
                            {
                                
                                SetRootDirectory(new NullAdaptorSettings());
                                
                            }
                        }
                    ).DisposeWith(disposables);
                this.WhenAnyValue(f => f.AlertVisible)
                    .Where(f => f is Visibility.Visible)
                    .Subscribe(
                        async f =>
                        {
                            await Task.Delay(5000);
                            AlertActive = false;
                            this.RaisePropertyChanged(nameof(AlertVisible));
                        }
                    ).DisposeWith(disposables);

                Observable.Interval(TimeSpan.FromSeconds(5))
                          .Subscribe(
                            _ =>
                            {
                                UpdateList();
                                this.RaisePropertyChanged(nameof(Directories));
                                this.RaisePropertyChanged(nameof(Files));
                            }
                          ).DisposeWith(disposables);
            }
        );
    }

    public void UpdateList()
    {
        if (Path.Exists(CurrentPath))
        {
            Directories = Directory.GetDirectories(CurrentPath).Select(
                dir => new DirectoryModel()
                {
                    Name = Path.GetFileName(dir),
                    Path = dir
                }
            ).ToList();
            Files = Directory.GetFiles(CurrentPath).Select(
                file => new FileModel()
                {
                    Name = Path.GetFileName(file),
                    Path = file,
                    IsSymLink = NabuLib.IsSymLink(file)
                }
            ).ToList();

        }
        else
        {
            Directories.Clear();
            Files.Clear();
        }
    }

    public void NotifyChange()
    {
        this.RaisePropertyChanged(nameof(PlaceHolderIconName));
        this.RaisePropertyChanged(nameof(CurrentPath));
        this.RaisePropertyChanged(nameof(Directories));
        this.RaisePropertyChanged(nameof(SelectedFile));
        this.RaisePropertyChanged(nameof(Files));
        this.RaisePropertyChanged(nameof(Uploading));
        this.RaisePropertyChanged(nameof(ShowUpload));
        this.RaisePropertyChanged(nameof(Pending));
    }

    public void SetRootDirectory(AdaptorSettings? settings, string? path = null)
    {
        if (path is null || settings is NullAdaptorSettings)
        {
            SetCurrentDirectory(RootPath = string.Empty);
        }
        else
        {
            if (settings is TCPAdaptorSettings tcp && tcp.Connection is false)
            {
                RootPath = Path.Combine(path, "Source");
            }
            else
            {
                RootPath = path;
            }
            RootPath = new DirectoryInfo(RootPath).FullName;
            SetCurrentDirectory(RootPath);
        }
        this.RaisePropertyChanged(nameof(RootPath));
    }

    public void SetCurrentDirectory(string path)
    {
        CurrentPath = path == string.Empty ? string.Empty : new DirectoryInfo(path).FullName;
        RootPath ??= CurrentPath;
        SelectedFile = null;
        UpdateList();
        NotifyChange();
    }

    public string DownloadLink(FileModel? file)
    {
        if (file is null) return string.Empty;

        var path = Path.GetRelativePath(Location.Home, file.Path).Replace("\\","/");

        return $"/api/download/" + path;
    }

    public void UpDirectory()
    {
        if (RootPath == CurrentPath) return;
        SetCurrentDirectory(Directory.GetParent(CurrentPath)!.FullName);
    }

    public bool ShowUpload { get; set; } = false;
    public bool ShowNewFolder { get; set; }

    public Visibility OperationVisible => !ShowUpload && !Uploading && !ShowNewFolder ? Visibility.Visible : Visibility.Invisible;

    public Visibility PlaceHolderVisible => RootPath == string.Empty ? Visibility.Visible : Visibility.Invisible;
    public Visibility Visible => RootPath != string.Empty ? Visibility.Visible : Visibility.Invisible;
    public IconName PlaceHolderIconName => Home.VisiblePage is VisiblePage.AdaptorSettings ? IconName.SliderHorizontal : IconName.Folder;

    #region Alert

    public Visibility AlertVisible => AlertActive ? Visibility.Visible : Visibility.Invisible;

    public bool AlertActive { get; set; }
    public string AlertText { get; set; } = string.Empty;
    public AlertState AlertState { get; set; } = AlertState.Info;
    public void Alert(string message, AlertState state = AlertState.Info)
    {
        AlertText = message;
        AlertState = state;
        AlertActive = true;
        this.RaisePropertyChanged(nameof(AlertVisible));
    }

    #endregion

    #region Upload
    public Visibility UploadVisible => !ShowNewFolder && ShowUpload && Uploading ? Visibility.Visible : Visibility.Invisible;

    public bool Uploading { get; set; } = false;
    private readonly long maxFileSize = 1024 * 1024 * 25;
    
    public async Task Upload(InputFileChangeEventArgs e)
    {
        Uploading = true;
        ShowUpload = false;
        NotifyChange();
        var files = e.GetMultipleFiles().Distinct();
        var zipFiles = files.Where(f => f.Name.EndsWith(".zip"));
        if (zipFiles.Any()) 
        {
            if (zipFiles.Count() > 1)
            {
                Alert("Only 1 zip file allowed", AlertState.Error);
                return;
            }

            try
            {
                var archive = zipFiles.First();
                var reader = archive.OpenReadStream(maxFileSize);
                var zip = new ZipArchive(reader);

                var conflicts = zip.Entries.Where(e => NabuLib.IsSymLink(Path.Join(CurrentPath, e.FullName)));
                if (!conflicts.Any())
                {
                    zip.ExtractToDirectory(CurrentPath, true);
                    return;
                }
                else
                {
                    Alert("Content conflicts with links", AlertState.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Empty, ex);
                Alert(ex.Message, AlertState.Error);
            }
        }

        
        foreach (var file in files)
        {
            var name = Path.GetFileName(NabuLib.SanitizePath(file.Name));
            try
            {
                var path = Path.Combine(CurrentPath, Path.GetFileName(name));
                await using FileStream fs = new(path, FileMode.Create);
                file.OpenReadStream(maxFileSize).CopyTo(fs);

            }
            catch (Exception ex)
            {
                Logger.LogError(string.Empty, ex);
                Alert(ex.Message, AlertState.Error);
            }
        }

        Uploading = false;
        UpdateList();
        NotifyChange();
    }
    #endregion

    #region New Folder

    public Visibility NewFolderVisible => ShowNewFolder && !ShowUpload && !Uploading ? Visibility.Visible : Visibility.Invisible;

    public string NewFolderName { get; set; } = string.Empty;
    public void NewFolder()
    {
        Directory.CreateDirectory(Path.Join(CurrentPath, NewFolderName));
        NewFolderName = string.Empty;
        ShowNewFolder = false;
        UpdateList();
        NotifyChange();
    }

    #endregion

    #region Lower Action Bar
    public FileModel? SelectedFile { get; set; }
    public bool ActionStarted => Pending is not null;

    public bool IsActionable => SelectedFile?.IsSymLink is false;

    public void SetSelectedFile(FileModel? file)
    {
        if (!ActionEnabled) return;

        SelectedFile = file;
        NotifyChange();
    }
    private FileModel? Pending { get; set; }

    public bool ActionEnabled => !ActionStarted && !ShowUpload && !ShowNewFolder && !Uploading;

    public Visibility ActionVisible => !ActionStarted && !ShowUpload && !ShowNewFolder && !Uploading ? Visibility.Visible : Visibility.Invisible;

    public Visibility ConfirmVisible => ActionStarted ? Visibility.Visible :
                                            Visibility.Invisible;

    public Visibility DeleteVisible => ActionStarted && Action is FileViewAction.DeleteFile or FileViewAction.DeleteFolder ?
                                            Visibility.Visible :
                                            Visibility.Invisible;

    public Visibility CopyMoveVisible => ActionStarted && (Action is FileViewAction.CopyFile or FileViewAction.MoveFile) ?
                                            Visibility.Visible :
                                            Visibility.Invisible;

    public bool ActionButtonDisabled => ActionStarted || !Selected || !IsActionable;
    public bool DeleteFolderDisabled => ActionStarted || CurrentPath == RootPath || Files.Any(f => f.IsSymLink);

    public bool Selected => SelectedFile is not null;
    private FileViewAction Action { get; set; }
    private Action<FileModel?>? ActionHandler { get; set; }

    public Visibility CurrentAction(FileViewAction action)
        => Action == action ? Visibility.Visible : Visibility.Invisible;

    public void StartAction(FileViewAction action, string? pending = null)
    {
        Action = action;
        ActionHandler = action switch
        {
            FileViewAction.DeleteFile => DeleteFile,
            FileViewAction.DeleteFolder => DeleteFolder,
            FileViewAction.CopyFile => CopyFile,
            FileViewAction.MoveFile => MoveFile,
            _ => _ => { }
        };
        Pending = pending is null ? SelectedFile : new DirectoryModel { Path = pending };
        NotifyChange();
    }

    public void CancelAction()
    {
        Pending = null;
        NotifyChange();
    }

    public void CompleteAction()
    {
        SelectedFile = null;
        ActionHandler?.Invoke(Pending);
        Pending = null;

        UpdateList();
        NotifyChange();
    }

    public void DeleteFile(FileModel? pending)
    {
        if (pending is null) return;
        File.Delete(pending.Path);
    }

    public void DeleteFolder(FileModel? pending)
    {
        if (pending is null || pending is not DirectoryModel) return;
        if (CurrentPath == RootPath || Files.Any(f => f.IsSymLink)) return;

        UpDirectory();
        Directory.Delete(pending.Path, true);
    }

    public void CopyFile(FileModel? pending)
    {
        if (pending is null) return;

        var destination = NabuLib.PlatformPath(Path.Join(CurrentPath, Path.GetFileName(pending.Path)));
        if (destination is null || pending.Path == destination) return;
        File.Copy(pending.Path, destination);
    }

    public void MoveFile(FileModel? pending)
    {
        if (pending is null) return;

        var destination = NabuLib.PlatformPath(Path.Join(CurrentPath, Path.GetFileName(pending.Path)));
        if (destination is null || pending.Path == destination) return;
        File.Move(pending.Path, destination);
    }
    #endregion
}