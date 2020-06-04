using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Cloud_Manager.Managers
{
    public enum CloudManagerType
    {
        GoogleDrive,
        Dropbox
    }

    public abstract class CloudDrive
    {
        public abstract void DownloadFile(string fullPath, string id, bool isEncrypted);
        public abstract void UploadFile(FileStructure curDir, string filePath);
        public abstract void UploadFile(FileStructure curDir, string content, string filePath);
        public abstract void PasteFiles(ICollection<FileStructure> cutFiles, FileStructure curDir);
        public abstract void CreateFolder(string name, FileStructure parentDir);
        public abstract void RemoveFile(ICollection<FileStructure> selectedFiles);
        public abstract void TrashFile(ICollection<FileStructure> selectedFiles);
        public abstract void UnTrashFile(ICollection<FileStructure> selectedFiles);
        public abstract void ClearTrash();
        public abstract void RenameFile(ICollection<FileStructure> selectedFiles, string newName);
        public abstract ObservableCollection<FileStructure> GetFiles();
    }
}
