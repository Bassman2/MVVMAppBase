using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Shell;

namespace MVVMAppBase.ViewModel
{
    public partial class FileViewModel : AppViewModel
    {
        private readonly string saveBefore = "Do you want to store current file before? If not all changes will be lost.";

        protected string DefaultFileExt { get; set; }
        protected string FileFilter { get; set; }
        protected int MaxNumOfRecentFiles { get; set; }
        

        public class FileItem
        {
            public FileItem(string path)
            {
                this.Name = System.IO.Path.GetFileName(path);
                this.Path = System.IO.Path.GetFullPath(path);
                //this.Image = ShellIcon.GetLargeIcon(path);          
            }

            public string Name { get; }
            public string Path { get; }
            //public ImageSource Image { get; private set; }
        }

        public FileViewModel()
        {
            this.FilePath = null;
            this.FileChanged = false;
            this.DefaultFileExt = "*.*";
            this.FileFilter = "All Files|*.*";
            this.MaxNumOfRecentFiles = 10;
        }

        //[RelayCommand]
        public override void OnStartup()
        {
            LoadRecentFileList();

            string[] cmd = Environment.GetCommandLineArgs();
            if (cmd.Length > 1 && File.Exists(cmd[1]))
            {
                string path = Path.GetFullPath(cmd[1]);
                OnLoad(path);
                this.FilePath = path;
            }
            else if (this.Autoload && File.Exists(this.AutoloadFile))
            {
                OnLoad(this.AutoloadFile);
                this.FilePath = this.AutoloadFile;
            }
            base.OnStartup();
        }

        public override string Title => string.Format("{0}{1}{2}", base.Title, string.IsNullOrEmpty(this.FilePath) ? "" : " - " + this.FileName, this.FileChanged ? " *" : "");
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileName))]
        [NotifyPropertyChangedFor(nameof(Title))]
        private string filePath;
        

        public string FileName => Path.GetFileNameWithoutExtension(this.FilePath);


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        private bool fileChanged;

        /// <summary>
        /// Collection with the recent files.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<FileItem> recentFiles = new();

        protected virtual bool Autoload { get { return false; } }

        protected virtual string AutoloadFile { get { return string.Empty; } set { } }

        
        public virtual void OnCreate()
        {
            throw new NotImplementedException();
        }

        
        public virtual void OnLoad(string path)
        {
            throw new NotImplementedException();
        }

        public virtual void OnStore(string path)
        {
            throw new NotImplementedException();
        }

        protected virtual bool OnCanNew() => this.ProgressState == TaskbarItemProgressState.None;        

        [RelayCommand(CanExecute = nameof(OnCanNew))]
        protected virtual void OnNew()
        {
            if (StoreChanges())
            {
                this.FilePath = null;
                this.FileChanged = true;
                OnCreate();
            }
        }

        protected virtual bool OnCanOpen() => this.ProgressState == TaskbarItemProgressState.None;
        
        [RelayCommand(CanExecute = nameof(OnCanOpen))]
        protected virtual void OnOpen()
        {
            if (StoreChanges())
            {
                OpenFileDialog dlg = new OpenFileDialog()
                {
                    CheckFileExists = true,
                    CheckPathExists = true,
                    DefaultExt = DefaultFileExt,
                    Filter = FileFilter,
                    Multiselect = false,
                    Title = "Open file ..."
                };
                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    this.FilePath = dlg.FileName;
                    OnLoad(dlg.FileName);
                    this.AutoloadFile = dlg.FileName;
                    this.FileChanged = false;
                    AddRecentFile(dlg.FileName);
                }
            }
        }

        protected virtual bool OnCanOpenFile(string path) => this.ProgressState == TaskbarItemProgressState.None;

        [RelayCommand(CanExecute = nameof(OnCanOpenFile))]
        protected virtual void OnOpenFile(string path)
        {

            if (StoreChanges())
            {
                OnLoad(path);
                this.FilePath = path;
                this.AutoloadFile = path;
                this.FileChanged = false;
                AddRecentFile(path);
            }
        }

        protected virtual bool OnCanSave() => this.ProgressState == TaskbarItemProgressState.None && !string.IsNullOrEmpty(this.FilePath) && this.FileChanged;
        
        [RelayCommand(CanExecute = nameof(OnCanSave))]
        protected virtual void OnSave()
        {
            OnStore(this.FilePath);
            this.FileChanged = false;
        }

        protected virtual bool OnCanSaveAs() => this.ProgressState == TaskbarItemProgressState.None;
        
        [RelayCommand(CanExecute = nameof(OnCanSaveAs))]
        protected virtual void OnSaveAs()
        {
            SaveFileDialog dlg = new SaveFileDialog()
            {
                OverwritePrompt = true,
                ValidateNames = true,
                CheckPathExists = true,
                DefaultExt = DefaultFileExt,
                Filter = FileFilter,
                Title = "Save file as ..."
            };
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                //this.watcher.EnableRaisingEvents = false;
                OnStore(dlg.FileName);
                this.FilePath = dlg.FileName;
                this.AutoloadFile = dlg.FileName;
                this.FileChanged = false;
                AddRecentFile(dlg.FileName);
            }
        }

        [RelayCommand]
        protected virtual void OnRecentFile(string path)
        {
            if (File.Exists(path))
            {
                OnOpenFile(path);
            }
            else
            {
                //this.recentFiles.Remove(i => i.Path == path);
            }
        }

        //protected virtual void OnFileChanged(object source, FileSystemEventArgs e)
        //{
        //    if (e.ChangeType == WatcherChangeTypes.Changed)
        //    {
        //        Application.Current.Dispatcher.Invoke(new Action(() =>
        //        {
        //            if (MessageBox.Show(Application.Current.MainWindow, "File changed from outside.\r\nDo you want to reload the file and loose all changes?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        //            {
        //                OnLoad(e.FullPath);
        //                this.FileChanged = false;
        //            }
        //        }));
        //    }
        //}

        //protected virtual void OnFileRenamed(object source, RenamedEventArgs e)
        //{
        //    if (e.ChangeType == WatcherChangeTypes.Renamed)
        //    {
        //        Application.Current.Dispatcher.Invoke(new Action(() =>
        //        {
        //            if (MessageBox.Show(Application.Current.MainWindow, "File renamed from outside.\r\nDo you want to rename your file too?", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        //            {
        //                this.FilePath = e.FullPath;
        //                this.AutoloadFile = e.FullPath;
        //            }
        //        }));
        //    }
        //}

        /// <summary>
        /// Drag handler for drag and drop files
        /// </summary>
        /// <param name="args"></param>
        /// <example>
        /// &lt;Window x:Class="InternalInvoiceManager.MainView" xmlns:i="clr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity"&gt;
        /// &lt;i:Interaction.Triggers&gt;
        ///     &lt;i:EventTrigger EventName="PreviewDragEnter"&gt;
        ///         &lt;EventToCommand Command="{Binding DragCommand}" PassEventArgsToCommand="True"/&gt;
        ///     &lt;/i:EventTrigger&gt;
        ///     &lt;i:EventTrigger EventName="PreviewDragOver"&gt;
        ///         &lt;EventToCommand Command="{Binding DragCommand}" PassEventArgsToCommand="True"/&gt;
        ///     &lt;/i:EventTrigger&gt;
        ///     &lt;i:EventTrigger EventName="PreviewDrop"&gt;
        ///     &lt;EventToCommand Command="{Binding DropCommand}" PassEventArgsToCommand="True"/&gt;
        ///     &lt;/i:EventTrigger&gt;
        /// &lt;/i:Interaction.Triggers&gt;
        /// &lt;/example&gt;
        [RelayCommand]
        protected virtual void OnDrag(DragEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            bool isCorrect = true;
            if (args.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] files = args.Data.GetData(DataFormats.FileDrop, true) as string[];
                foreach (string file in files)
                {
                    if (!File.Exists(file) || !OnCanOpenFile(file))
                    {
                        isCorrect = false;
                    }
                }
            }
            args.Effects = isCorrect ? DragDropEffects.All : DragDropEffects.None;
            args.Handled = true;
        }

        /// <summary>
        /// Drop handler for drag and drop files
        /// </summary>
        /// <param name="args"></param>
        /// <example>
        /// &lt;Window x:Class="InternalInvoiceManager.MainView" xmlns:i="clr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity"&gt;
        /// &lt;i:Interaction.Triggers&gt;
        ///     &lt;i:EventTrigger EventName="PreviewDragEnter"&gt;
        ///         &lt;EventToCommand Command="{Binding DragCommand}" PassEventArgsToCommand="True"/&gt;
        ///     &lt;/i:EventTrigger&gt;
        ///     &lt;i:EventTrigger EventName="PreviewDragOver"&gt;
        ///         &lt;EventToCommand Command="{Binding DragCommand}" PassEventArgsToCommand="True"/&gt;
        ///     &lt;/i:EventTrigger&gt;
        ///     &lt;i:EventTrigger EventName="PreviewDrop"&gt;
        ///     &lt;EventToCommand Command="{Binding DropCommand}" PassEventArgsToCommand="True"/&gt;
        ///     &lt;/i:EventTrigger&gt;
        /// &lt;/i:Interaction.Triggers&gt;
        /// &lt;/example&gt;
        [RelayCommand]
        protected virtual void OnDrop(DragEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = args.Data.GetData(DataFormats.FileDrop, true) as string[];
                foreach (string file in files)
                {
                    OnOpenFile(file);
                }
            }
            args.Handled = true;
        }

        /// <summary>
        /// Closing handler to store changed files on exit.
        /// </summary>
        /// <example>
        /// &lt;Window x:Class="InternalInvoiceManager.MainView" xmlns:i="clr-namespace:System.Windows.Interactivity;assembly=System.Windows.Interactivity"&gt;
        /// &lt;i:Interaction.Triggers&gt;
        /// &lt;i:EventTrigger EventName="Closing"&gt;
        ///     &lt;EventToCommand Command="{Binding ClosingCommand}" PassEventArgsToCommand="True"/&gt;
        /// &lt;/i:EventTrigger&gt;
        /// </example>
        protected override bool OnClosing()
        {
            return StoreChanges();
        }

        protected virtual bool StoreChanges()
        {
            if (this.FileChanged)
            {
                switch (MessageBox.Show(saveBefore, "Info", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                case MessageBoxResult.Yes:
                    if (string.IsNullOrEmpty(this.FilePath))
                    {
                        OnSaveAs();
                    }
                    else
                    {
                        OnSave();
                    }
                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    return false;
                }
            }
            return true;
        }

        private void LoadRecentFileList()
        {
            ApplicationSettingsBase settings = GetApplicationSettings();

            if (settings["RecentFiles"] != null)
            {
                //foreach (string path in Settings.Default.RecentFiles)
                //{
                //    if (File.Exists(path))
                //    {
                //        this.recentFiles.Add(new FileItem(path));
                //    }
                //}

                //settings["RecentFiles"] = new(settings["RecentFiles"]?.Cast<string>().Where(p => File.Exists(p)).Select(p => new FileItem(p)));
            }
        }

        protected void AddRecentFile(string path)
        {
            //if (this.recentFiles.Count <= this.MaxNumOfRecentFiles && !this.recentFiles.Any(i => i.Name == Path.GetFileName(path)))
            //{
            //    this.recentFiles.Add(new FileItem(path));

            //    // store recent file list
            //    StringCollection col = new StringCollection();
            //    col.AddRange(this.recentFiles.Select(f => f.Path).ToArray());
            //    Settings.Default.RecentFiles = col;
            //    Settings.Default.Save();
            //}
        }
    }
}
