using Blazorise;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Nabu.NetSim.UI.Models;
using Nabu.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels;


public record FileViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsSymLink { get; set; } = false;

}

public record DirectoryViewModel : FileViewModel { }


public enum FileViewAction
{
    None,
    CopyMove,
    Delete
}

public class FilesViewModel : ReactiveObject, IActivatableViewModel
{
    public string RootPath { get; set; } = string.Empty;
    public string CurrentPath { get; set; } = string.Empty;
    public FileViewModel? SelectedFile {get; set;}
    public List<FileViewModel> Files { get; set; } = new();
    public List<DirectoryViewModel> Directories { get; set; } = new();
    ILog<FilesViewModel> Logger { get; }
    public ViewModelActivator Activator { get; } = new();
    HomeViewModel Home { get; }
    public FilesViewModel(ILog<FilesViewModel> logger, HomeViewModel home) {
        Logger = logger;
        Home = home;
        this.WhenActivated(
            disposables =>
            {
                
                Home.WhenAnyValue(h => h.VisiblePage)
                    .Where(p => p is not VisiblePage.AdaptorSettings)
                    .Subscribe(p => {
                        if (RootPath != string.Empty)
                            SetRootDirectory(new NullAdaptorSettings());
                    }).DisposeWith(disposables);
            }
        );
    }

    public void UpdateList()
    {
        if (Path.Exists(CurrentPath))
        {
            Directories = Directory.GetDirectories(CurrentPath).Select(
                dir => new DirectoryViewModel() { 
                    Name = Path.GetFileName(dir), 
                    Path = dir 
                }
            ).ToList();
            Files = Directory.GetFiles(CurrentPath).Select(
                file => new FileViewModel() { 
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
        this.RaisePropertyChanged(nameof(CurrentPath));
        this.RaisePropertyChanged(nameof(Directories));
        this.RaisePropertyChanged(nameof(SelectedFile));
        this.RaisePropertyChanged(nameof(Files));
        this.RaisePropertyChanged(nameof(Uploading));
        this.RaisePropertyChanged(nameof(ShowUpload));
        this.RaisePropertyChanged(nameof(Pending));
    }

    public void SetRootDirectory(AdaptorSettings settings, string? path = null)
    {
        if (path is null || settings is NullAdaptorSettings)
        {
            RootPath = string.Empty;
            SetCurrentDirectory(RootPath);
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
        SelectedFile = null;
        UpdateList();
        NotifyChange();
    }

    public void UpDirectory()
    {
        if (RootPath == CurrentPath) return;
        SetCurrentDirectory(Directory.GetParent(CurrentPath)!.FullName);
    }

    public void SetSelectedFile(FileViewModel? file)
    {
        SelectedFile = file;
        NotifyChange();
    }

    public bool Uploading { get; set; } = false;
    readonly long maxFileSize = (1024 * 1024 * 25);
    public bool ShowUpload { get; set; } = false;
    public async Task Upload(InputFileChangeEventArgs e)
    {
        Uploading = true;
        ShowUpload = false;
        NotifyChange();
        

        foreach (var file in e.GetMultipleFiles())
        {
            try
            {
                var path = Path.Combine(CurrentPath, Path.GetFileName(file.Name));

                await using FileStream fs = new(path, FileMode.Create);
                await file.OpenReadStream(maxFileSize).CopyToAsync(fs);
                
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Empty, ex);
            }
           
        }
        
        Uploading = false;
        UpdateList();
        NotifyChange();

    }
    public bool NewFolderActive { get; set; }
    public Visibility NewFolderVisible => NewFolderActive ? Visibility.Visible : Visibility.Invisible;

    FileViewModel? Pending { get; set; }
    
    public Visibility ActionVisible => !ActionStarted ? Visibility.Visible : Visibility.Invisible;
    public Visibility ConfirmVisible => ActionStarted && (
                                            Action is FileViewAction.Delete
                                        ) ? Visibility.Visible : 
                                            Visibility.Invisible;
    public Visibility DeleteVisible => ActionStarted && Action is FileViewAction.Delete ? 
                                            Visibility.Visible : 
                                            Visibility.Invisible;
    public Visibility CopyMoveVisible => ActionStarted && Action is FileViewAction.CopyMove ?
                                            Visibility.Visible :
                                            Visibility.Invisible;
    public bool ActionStarted => Pending is not null;

    public bool IsActionable => SelectedFile?.IsSymLink is false;

    public bool Selected => SelectedFile is not null;
    FileViewAction Action { get; set; }
    Action<FileViewModel?>? ActionHandler { get; set; }

    public Visibility CurrentAction(FileViewAction action) 
        => Action == action ? Visibility.Visible : Visibility.Invisible;
    public void StartAction(FileViewAction action)
    {
        Action = action;
        ActionHandler = action switch
        {   
            FileViewAction.Delete => Delete,
            _ => _ => { }
        };
        Pending = SelectedFile;
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
    

    public void Delete(FileViewModel? pending)
    {
        if (pending is null) return;
            File.Delete(pending.Path);

    }

    public void CopyMove(FileViewModel? pending)
    {
        
    }
}
