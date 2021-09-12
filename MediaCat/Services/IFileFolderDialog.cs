namespace MediaCat.Services {

    public interface IFileFolderDialog {

        string[] ShowOpenFileDialog(string title, string filter = null, bool multiselect = false, string initialDirectory = null, object owner = null);

        string ShowSaveFileDialog(string title, string filter = null, bool checkFileExists = true, string initialDirectory = null, object owner = null);

        string[] ShowOpenFolderDialog(string title, bool multiselect = false, string initialDirectory = null, object owner = null);

    }

}