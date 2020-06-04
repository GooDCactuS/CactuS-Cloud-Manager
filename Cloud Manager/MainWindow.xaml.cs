﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cloud_Manager.Properties;
using Microsoft.Win32;

namespace Cloud_Manager
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public static string WindowName { get; } = "Cactus Cloud Manager";

        public static MainWindow WindowObject { get; private set; }

        private readonly CloudManagerLogic _cloudManagerLogic;

        // Buttons availability
        public bool IsDriveOpened => _cloudManagerLogic.CurrentPath != "/";

        public bool IsExistItems => _cloudManagerLogic.CutItems.Any() && _cloudManagerLogic.CurrentPath != "/";

        public bool IsSingleSelected => _cloudManagerLogic.SelectedItems.Count() == 1 && _cloudManagerLogic.CurrentPath != "/";

        public bool IsSelected => _cloudManagerLogic.SelectedItems.Count > 0 && _cloudManagerLogic.CurrentPath != "/";

        public bool IsSelectedInTrash => _cloudManagerLogic.SelectedItems.Count > 0 && _cloudManagerLogic.CurrentPath.Substring(_cloudManagerLogic.CurrentPath.LastIndexOf('/')) == "/Trash";

        public bool IsDownloadAvailable
        {
            get { return _cloudManagerLogic.SelectedItems.Count(item => item.FileExtension != null) == 1 && _cloudManagerLogic.SelectedItems.Count == 1 && _cloudManagerLogic.CurrentPath != "/"; }
        }


        private ObservableCollection<FileStructure> _folderItems;
        public ObservableCollection<FileStructure> FolderItems
        {
            get => _folderItems;
            set
            {
                if (_cloudManagerLogic.CurrentPath != "/")
                {
                    value.Insert(0, new FileStructure { Name = ".." });
                }
                _folderItems = value;
                OnPropertyChanged("FolderItems");
            }
        }

        public string WindowTitle => $"{WindowName} - {_cloudManagerLogic.CurrentPath}";

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            SetOptions();
            _cloudManagerLogic = new CloudManagerLogic();
            InitializeComponent();
            DataContext = this;
            OnPropertyChanged("WindowTitle");
            WindowObject = this;

            FolderItems = _cloudManagerLogic.InitStartFolder();
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh()
        {
            refresh_Click(null, null);
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.RefreshInfo();
        }

        private void goUp_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.GetParentDirectory();
            NotifyMenuItems();
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.GetHomeDirectory();
            NotifyMenuItems();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.GetPreviousDirectory();
            NotifyMenuItems();
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = _cloudManagerLogic.SelectedItems.First();
            if (selectedItem != null)
            {
                bool isEncrypted = false;
                string fileName;
                if (selectedItem.Name.Substring(selectedItem.Name.LastIndexOf('.') + 1) == "enc")
                {
                    isEncrypted = true;
                    fileName = selectedItem.Name.Substring(0, selectedItem.Name.LastIndexOf('.'));
                }
                else
                {
                    fileName = selectedItem.Name;
                }
                var saveDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = "All files (*.*)|*.*"
                };
                if (saveDialog.ShowDialog() != true) { return; }

                _cloudManagerLogic.DownloadFile(saveDialog.FileName, selectedItem.Id, isEncrypted);
            }

            NotifyMenuItems();
        }

        private void upload_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "All files (*.*)|*.*", FileName = "" };
            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FileName != "")
                {
                    UploadFilePath.Text = openFileDialog.FileName;
                    PopupUploadConfirmation.IsOpen = true;
                }
            }

        }

        private void cut_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.CutFiles();
            NotifyMenuItems();
        }

        private void paste_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.PasteFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void makeDir_Click(object sender, RoutedEventArgs e)
        {
            PopupNewFolder.IsOpen = true;
        }

        private void createFolder_Click(object sender, RoutedEventArgs e)
        {
            PopupNewFolder.IsOpen = false;
            _cloudManagerLogic.CreateFolder(TxtNewFolderName.Text);
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.RemoveFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void trash_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.TrashFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void untrash_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.UnTrashFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void clearTrash_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.ClearTrash();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void rename_Click(object sender, RoutedEventArgs e)
        {
            PopupRenameFile.IsOpen = true;
        }

        private void renameFile_Click(object sender, RoutedEventArgs e)
        {
            WindowObject.PopupRenameFile.IsOpen = false;
            _cloudManagerLogic.RenameFile(TxtRenamedFile.Text);
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void NotifyMenuItems()
        {
            OnPropertyChanged("IsDriveOpened");
            OnPropertyChanged("IsExistItems");
            OnPropertyChanged("IsSingleSelected");
            OnPropertyChanged("IsSelected");
            OnPropertyChanged("IsDownloadAvailable");
            OnPropertyChanged("WindowTitle");
            OnPropertyChanged("CurrentPath");
        }

        private void gridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var items = e.AddedItems.Cast<FileStructure>();
                foreach (var item in items)
                {
                    _cloudManagerLogic.SelectedItems.Add(item);
                }
            }

            if (e.RemovedItems.Count > 0)
            {
                var items = e.RemovedItems.Cast<FileStructure>();
                foreach (var item in items)
                {
                    _cloudManagerLogic.SelectedItems.Remove(item);
                }
            }

            NotifyMenuItems();
        }

        private void gridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileStructure item = GridItems.SelectedItem as FileStructure;

            if (item == null)
            {
                return;
            }

            if (item.Name == "..")
            {
                goUp_Click(this, null);
            }
            else
            {
                FolderItems = _cloudManagerLogic.EnterFile(item);
                NotifyMenuItems();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            switch (menuItem?.Header.ToString())
            {
                case "English":
                    ChangeLanguage("en-US");
                    MessageBox.Show("Program will run in English after restart.");
                    break;
                case "Русский":
                    ChangeLanguage("ru-RU");
                    MessageBox.Show("Программа сменит язык после рестарта.");
                    break;
                default:
                    MessageBox.Show("Unexpected error. Restart the program.");
                    break;
            }
        }

        private void SetOptions()
        {
            if (Settings.Default.Language.Equals(string.Empty))
            {
                ChangeLanguage();
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Settings.Default.Language);
            }

            if (Settings.Default.Key == "" || Settings.Default.IV == "")
            {
                Aes aes = Aes.Create();
                Settings.Default.Key = Convert.ToBase64String(aes.Key);
                Settings.Default.IV = Convert.ToBase64String(aes.IV);
                Settings.Default.Save();
            }
        }

        private void ChangeLanguage(string language = null)
        {
            if (language == null)
            {
                Settings.Default.Language = Thread.CurrentThread.CurrentUICulture.ToString();
            }
            else
            {
                Settings.Default.Language = language;
            }
            Settings.Default.Save();
        }

        private void addCloud_Click(object sender, RoutedEventArgs e)
        {
            AddCloudWindow.AddCloud method = _cloudManagerLogic.AddCloud;
            var addCloudWindow = new AddCloudWindow(method, _cloudManagerLogic.CloudList);
            IsEnabled = false;
            addCloudWindow.Show();
        }

        private void renameCloud_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void removeCloud_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void copy_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs cancelEventArgs)
        {
            _cloudManagerLogic.SaveInfo();
        }

        private void search_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchWindow(_cloudManagerLogic.CloudList);
            IsEnabled = false;
            searchWindow.Show();
        }

        

        private void uploadConfirmation_Click(object sender, RoutedEventArgs e)
        {
            PopupUploadConfirmation.IsOpen = false;
            if (IsEncrypted != null)
            {
                _cloudManagerLogic.UploadFile(UploadFilePath.Text, (bool)IsEncrypted.IsChecked);
                FolderItems = _cloudManagerLogic.RefreshInfo();
                NotifyMenuItems();
            }
        }

        private void uploadCancel_Click(object sender, RoutedEventArgs e)
        {
            PopupUploadConfirmation.IsOpen = false;
        }
    }
}


