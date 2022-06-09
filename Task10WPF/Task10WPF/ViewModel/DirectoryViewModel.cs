using Syncfusion.UI.Xaml.TreeView.Engine;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

using Directory = Task10WPF.Model.Directory;

namespace Task10WPF.ViewModel
{
    public class DirectoryViewModel
    {

        public ObservableCollection<Directory> Directories { get; set; }
        public ICommand TreeViewLoadOnDemandCommand { get; set; }

        public DirectoryViewModel()
        {
            this.Directories = this.GetDrives();
            TreeViewLoadOnDemandCommand = new LoadOnDemandCommand(ExecuteOnDemandLoading, CanExecuteOnDemandLoading);
        }

        private bool CanExecuteOnDemandLoading(object sender)
        {
            var hasChildNodes = ((sender as TreeViewNode).Content as Directory).HasChildNodes;
            if (hasChildNodes)
                return true;
            else
                return false;
        }


        private void ExecuteOnDemandLoading(object obj)
        {
            var node = obj as TreeViewNode;

            if (node.ChildNodes.Count > 0)
            {
                node.IsExpanded = true;
                return;
            }
            node.ShowExpanderAnimation = true;
            Directory Directory = node.Content as Directory;

            System.Windows.Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background,
               new Action(async () =>
               {
                   var items = GetDirectories(Directory);
                   node.PopulateChildNodes(items);

                   if (items.Count() > 0)
                   {
                       node.IsExpanded = true;
                   }

                   node.ShowExpanderAnimation = false;

                   await UpdateSize(items);

               }));
        }
        public double ConvertToMB(long size)
        {
            var mb = 1024 * 1024;
            return Math.Round((double)size / mb, 2);
        }

        private ObservableCollection<Directory> GetDrives()
        {
            ObservableCollection<Directory> directories = new ObservableCollection<Directory>();
            DriveInfo[] driveInfo = DriveInfo.GetDrives();

            foreach (DriveInfo drive in driveInfo)
            {
                if (drive.IsReady)
                {
                    directories.Add(new Directory { Name = drive.Name, FullName = drive.Name, HasChildNodes = true, Size = ConvertToMB(drive.TotalSize) });
                }
            }

            return directories;
        }
        public async Task UpdateSize(ObservableCollection<Directory> directories)
        {
            foreach (Directory directory in directories)
            {
                if (directory.HasChildNodes == true)
                {
                    directory.Size = ConvertToMB(await GetDirectorySizeAsync(directory));
                }
            }
        }
        public async Task<long> GetDirectorySizeAsync(Directory directoryPath)
        {
            long spaceUsedInBytes = 0;

            var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true };
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath.FullName);

            try
            {
                spaceUsedInBytes = await Task.Run(
                  () => directoryInfo.EnumerateFiles("*", enumerationOptions)
                    .Sum(fileInfo => fileInfo.Length));

            }
            catch (Exception)
            { }

            return spaceUsedInBytes;
        }

        public ObservableCollection<Directory> GetDirectories(Directory directory)
        {
            ObservableCollection<Directory> directories = new ObservableCollection<Directory>();
            DirectoryInfo dirInfo = new DirectoryInfo(directory.FullName);

            foreach (DirectoryInfo directoryInfo in dirInfo.GetDirectories())
            {
                try
                {
                    directories.Add(new Directory()
                    {
                        Name = directoryInfo.Name,
                        HasChildNodes = directoryInfo.GetDirectories().Length > 0 || directoryInfo.GetFiles().Length > 0,
                        FullName = directoryInfo.FullName,
                        Size = 0

                    });
                }
                catch { }
            }
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
            {

                directories.Add(new Directory()
                {
                    Name = fileInfo.Name,
                    HasChildNodes = false,
                    FullName = fileInfo.FullName,
                    Size = ConvertToMB(fileInfo.Length)
                });
            }
            return directories;
        }
    }

    public class LoadOnDemandCommand : ICommand
    {
        public LoadOnDemandCommand(Action<object> executeAction,
                               Predicate<object> canExecute)
        {
            this.executeAction = executeAction;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        Action<object> executeAction;
        Predicate<object> canExecute;

        private bool canExecuteCache = true;
        public bool CanExecute(object parameter)
        {
            if (canExecute != null)
            {
                bool tempCanExecute = canExecute(parameter);

                if (canExecuteCache != tempCanExecute)
                {
                    canExecuteCache = tempCanExecute;
                    RaiseCanExecuteChanged();
                }
            }
            return canExecuteCache;

        }

        public void Execute(object parameter)
        {
            executeAction(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, new EventArgs());
            }
        }
    }
}
