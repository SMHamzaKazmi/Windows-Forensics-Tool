using System.IO;
using System.Windows;

namespace WindowsForensicsTool
{
    public class ExtensionMismatch
    {
        private MainWindow _mainWindow;
        private static readonly Dictionary<string, byte[]> fileSignatures = new Dictionary<string, byte[]>
        {
            { "PDF", new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            { "DOCX/XLSX/PPTX", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { "JPEG/JPG", new byte[] { 0xFF, 0xD8, 0xFF } },
            { "PNG", new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { "GIF", new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            { "ZIP", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { "RAR", new byte[] { 0x52, 0x61, 0x72, 0x21 } },
            { "7Z", new byte[] { 0x37, 0x7A, 0xBC, 0xAF } },
            { "MP3", new byte[] { 0x49, 0x44, 0x33 } },
            { "MP4", new byte[] { 0x66, 0x74, 0x79, 0x70 } },
            { "EXE", new byte[] { 0x4D, 0x5A } },
            { "TXT", new byte[] { 0xEF, 0xBB, 0xBF } },
            { "ISO", new byte[] { 0x43, 0x44, 0x30, 0x30, 0x31 } },
            { "MSI", new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
            { "JSON", new byte[] { 0x7B } },
            { "XML", new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C } }
        };

        public ExtensionMismatch(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void ExtensionMismatchSearchButton_Click()
        {
            _mainWindow.ExtensionMismatchDataGrid.ItemsSource = GetMismatchFiles();
        }

        public List<MismatchFiles> GetMismatchFiles()
        {
            try
            {
                if (string.IsNullOrEmpty(_mainWindow.ExtensionMismatchDirectoryTextBox.Text))
                    throw new Exception("Please enter a valid directory.");

                if (!Directory.Exists(_mainWindow.ExtensionMismatchDirectoryTextBox.Text))
                    throw new Exception("Directory does not exist.");

                List<FileInfo> allFiles = GetAllFiles(_mainWindow.ExtensionMismatchDirectoryTextBox.Text);
                List<MismatchFiles> mismatchFiles = new List<MismatchFiles>();

                foreach (FileInfo file in allFiles)
                {
                    byte[] header = new byte[8];
                    using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(header, 0, header.Length);
                    }

                    string detectedType = "Unknown"; // if changed from docx then it could not match coz of slashes

                    foreach (var signature in fileSignatures)
                    {
                        if (header.Take(signature.Value.Length).SequenceEqual(signature.Value))
                        {
                            detectedType = signature.Key;
                            break;
                        }
                    }

                    string extension = file.Extension.ToUpper().TrimStart('.');

                    if (!detectedType.Split('/').Contains(extension))
                        mismatchFiles.Add(new MismatchFiles { FileName = file.FullName, FileType = extension, ActualType = detectedType });
                }

                return mismatchFiles;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private List<FileInfo> GetAllFiles(string directoryPath)
        {
            try
            {
                List<FileInfo> filesList = new List<FileInfo>();
                string[] files = Directory.GetFiles(directoryPath);

                foreach (string file in files)
                {
                    filesList.Add(new FileInfo(file));
                }

                string[] subDirectories = Directory.GetDirectories(directoryPath);

                foreach (string subDir in subDirectories)
                {
                    filesList.AddRange(GetAllFiles(subDir));
                }

                return filesList;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class MismatchFiles
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string ActualType { get; set; }
    }
}
