using System.IO;
using System.Windows;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace WindowsForensicsTool
{
    public class KeywordSearch
    {
        private MainWindow _mainWindow;

        public KeywordSearch(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void KeywordSearchSearchButton_Click()
        {
            _mainWindow.KeywordSearchDataGrid.ItemsSource = GetKeywordFiles();
        }

        public List<FileNames> GetKeywordFiles()
        {
            try
            {
                if (string.IsNullOrEmpty(_mainWindow.KeywordSearchDirectoryTextBox.Text))
                    throw new Exception("Please enter a valid directory.");

                if (!Directory.Exists(_mainWindow.KeywordSearchDirectoryTextBox.Text))
                    throw new Exception("Directory does not exist.");

                if (string.IsNullOrEmpty(_mainWindow.KeywordSearchKeywordTextBox.Text))
                    throw new Exception("Please enter keyword(s).");

                List<FileInfo> allFiles = GetAllFiles(_mainWindow.KeywordSearchDirectoryTextBox.Text);
                List<FileNames> fileNames = new List<FileNames>();

                List<string> keywords = _mainWindow.KeywordSearchKeywordTextBox.Text.Split(',')
                    .Select(w => w.Trim())
                    .Where(w => !string.IsNullOrEmpty(w))
                    .ToList();

                foreach (FileInfo file in allFiles)
                {
                    string content = string.Empty;
                    string extension = file.Extension.ToLower();

                    switch (extension)
                    {
                        case ".txt":
                            content = File.ReadAllText(file.FullName);
                            break;
                        case ".docx":
                            using (var doc1 = WordprocessingDocument.Open(file.FullName, false))
                            {
                                content = doc1.MainDocumentPart?.Document.Body?.InnerText ?? "";
                                break;
                            }
                        case ".xlsx":
                            using (var doc2 = SpreadsheetDocument.Open(file.FullName, false))
                            {
                                var sheets = doc2.WorkbookPart?.Workbook.Sheets.Elements<Sheet>();
                                var sb2 = new System.Text.StringBuilder();
                                foreach (var sheet in sheets)
                                {
                                    var wsPart = (WorksheetPart)doc2.WorkbookPart.GetPartById(sheet.Id);
                                    sb2.Append(wsPart.Worksheet.InnerText);
                                }
                                content = sb2.ToString();
                                break;
                            }
                        case ".pptx":
                            using (var doc3 = PresentationDocument.Open(file.FullName, false))
                            {
                                var sb3 = new System.Text.StringBuilder();
                                foreach (var slidePart in doc3.PresentationPart.SlideParts)
                                {
                                    sb3.Append(slidePart.Slide.InnerText);
                                }
                                content = sb3.ToString();
                                break;
                            }
                        case ".pdf":
                            using (var pdfReader = new PdfReader(file.FullName))
                            {
                                using (var pdfDoc = new PdfDocument(pdfReader))
                                {
                                    var sb4 = new System.Text.StringBuilder();
                                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                                    {
                                        sb4.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                                    }
                                    content = sb4.ToString();
                                    break;
                                }
                            }
                        default:
                            content = string.Empty;
                            break;
                    }

                    if (keywords.Any(word => content.Contains(word, StringComparison.OrdinalIgnoreCase)))
                        fileNames.Add(new FileNames { FileName = file.FullName });
                }

                return fileNames;
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
                string[] files = null;

                try
                {
                    files = Directory.GetFiles(directoryPath);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("denied"))
                        throw;
                }

                if (files != null)
                {
                    foreach (string file in files)
                    {
                        filesList.Add(new FileInfo(file));
                    }

                    string[] subDirectories = Directory.GetDirectories(directoryPath);

                    foreach (string subDir in subDirectories)
                    {
                        filesList.AddRange(GetAllFiles(subDir));
                    }
                }

                return filesList;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class FileNames
    {
        public string FileName { get; set; }
    }
}
