using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Windows;
using System.Data.SQLite;

namespace WindowsForensicsTool
{
    public class ActivityHistory
    {
        private MainWindow _mainWindow;

        public ActivityHistory(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void ActivityHistorySearchButton_Click()
        {
            if (_mainWindow.AccessedFilesRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetLastDateTimeFiles(LastDateTime.Accessed);

            if (_mainWindow.CreatedFilesRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetLastDateTimeFiles(LastDateTime.Created);

            if (_mainWindow.ModifiedFilesRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetLastDateTimeFiles(LastDateTime.Modified);

            if (_mainWindow.BrowserHistoryRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetBrowserHistory();

            if (_mainWindow.PowershellCommandsRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetPowerShellCommands();

            if (_mainWindow.RunCommandsRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetRunCommands();

            if (_mainWindow.UsbsHistoryRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetUsbsHistory();

            if (_mainWindow.WifiHistoryRadioButton.IsChecked == true)
                _mainWindow.ActivityHistoryDataGrid.ItemsSource = GetWifiHistory();
        }

        private List<FileDetails> GetLastDateTimeFiles(LastDateTime lastDateTime)
        {
            try
            {
                if (string.IsNullOrEmpty(_mainWindow.ActivityHistoryDirectoryTextBox.Text))
                    throw new Exception("Please enter a valid directory.");

                if (!Directory.Exists(_mainWindow.ActivityHistoryDirectoryTextBox.Text))
                    throw new Exception("Directory does not exist.");

                DateTime? startDate = null;
                DateTime? endDate = null;

                if (!string.IsNullOrEmpty(_mainWindow.StartDatePicker.Text))
                    startDate = DateTime.ParseExact(_mainWindow.StartDatePicker.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                if (!string.IsNullOrEmpty(_mainWindow.EndDatePicker.Text))
                    endDate = DateTime.ParseExact(_mainWindow.EndDatePicker.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                List<FileInfo> allFiles = GetAllFiles(_mainWindow.ActivityHistoryDirectoryTextBox.Text);
                DateTime dateTime = DateTime.Now;
                List<FileDetails> fileDetails = new List<FileDetails>();

                foreach (FileInfo file in allFiles)
                {
                    switch (lastDateTime)
                    {
                        case LastDateTime.Accessed:
                            dateTime = file.LastAccessTime;
                            break;
                        case LastDateTime.Created:
                            dateTime = file.CreationTime;
                            break;
                        case LastDateTime.Modified:
                            dateTime = file.LastWriteTime;
                            break;
                    }

                    string filename = file.FullName;

                    if (startDate != null && endDate != null)
                    {
                        if (dateTime >= startDate && dateTime <= endDate)
                            fileDetails.Add(new FileDetails { FileName = filename, DateTime = dateTime });
                    }
                    else if (startDate == null && endDate != null)
                    {
                        if (dateTime <= endDate)
                            fileDetails.Add(new FileDetails { FileName = filename, DateTime = dateTime });
                    }
                    else if (startDate != null && endDate == null)
                    {
                        if (dateTime >= startDate)
                            fileDetails.Add(new FileDetails { FileName = filename, DateTime = dateTime });
                    }
                    else
                        fileDetails.Add(new FileDetails { FileName = filename, DateTime = dateTime });
                }

                return fileDetails;
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

        private List<BrowserHistoryEntry> GetBrowserHistory()
        {
            try
            {
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (!string.IsNullOrEmpty(_mainWindow.StartDatePicker.Text))
                    startDate = DateTime.ParseExact(_mainWindow.StartDatePicker.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                if (!string.IsNullOrEmpty(_mainWindow.EndDatePicker.Text))
                    endDate = DateTime.ParseExact(_mainWindow.EndDatePicker.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                List<BrowserHistoryEntry> historyList = new List<BrowserHistoryEntry>();

                historyList.Add(new BrowserHistoryEntry { Browser = "Firefox", Title = "=== Firefox History ===" });
                historyList.AddRange(GetFirefoxHistory(startDate, endDate));

                historyList.Add(new BrowserHistoryEntry());

                historyList.Add(new BrowserHistoryEntry { Browser = "Chrome", Title = "=== Google Chrome History ===" });
                historyList.AddRange(GetChromeHistory(startDate, endDate));

                return historyList;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private List<BrowserHistoryEntry> GetFirefoxHistory(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Mozilla\Firefox\Profiles");

                if (!Directory.Exists(firefoxProfilePath))
                    return new List<BrowserHistoryEntry>();

                string[] profiles = Directory.GetDirectories(firefoxProfilePath, "*.default-release");

                if (profiles.Length == 0)
                    return new List<BrowserHistoryEntry>();

                string firefoxHistoryPath = Path.Combine(profiles[0], "places.sqlite");

                if (!File.Exists(firefoxHistoryPath))
                    return new List<BrowserHistoryEntry>();

                string tempFile = Path.Combine(Path.GetTempPath(), "FirefoxHistoryCopy.db");
                File.Copy(firefoxHistoryPath, tempFile, true);

                List<BrowserHistoryEntry> history = new List<BrowserHistoryEntry>();

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={tempFile};Version=3;"))
                {
                    conn.Open();

                    long? startTimestamp = startDate.HasValue ? new DateTimeOffset(startDate.Value).ToUnixTimeMilliseconds() * 1000 : null;
                    long? endTimestamp = endDate.HasValue ? new DateTimeOffset(endDate.Value).ToUnixTimeMilliseconds() * 1000 : null;

                    string query = "SELECT title, url, visit_date FROM moz_places " +
                        "JOIN moz_historyvisits ON moz_places.id = moz_historyvisits.place_id WHERE 1=1";
                    if (startTimestamp.HasValue)
                        query += " AND visit_date >= " + startTimestamp.Value;
                    if (endTimestamp.HasValue)
                        query += " AND visit_date <= " + endTimestamp.Value;
                    query += " ORDER BY visit_date DESC";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new BrowserHistoryEntry
                            {
                                Browser = "Firefox",
                                Title = reader["title"].ToString(),
                                URL = reader["url"].ToString(),
                                VisitTime = DateTimeOffset.FromUnixTimeMilliseconds(((long)reader["visit_date"]) / 1000).DateTime
                            });
                        }
                    }
                }

                File.Delete(tempFile);

                return history;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<BrowserHistoryEntry> GetChromeHistory(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                string chromeHistoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\User Data\Default\History");

                if (!File.Exists(chromeHistoryPath))
                    return new List<BrowserHistoryEntry>();

                string tempFile = Path.Combine(Path.GetTempPath(), "ChromeHistoryCopy.db");
                File.Copy(chromeHistoryPath, tempFile, true);

                List<BrowserHistoryEntry> history = new List<BrowserHistoryEntry>();

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={tempFile};Version=3;"))
                {
                    conn.Open();

                    long? startTimestamp = startDate.HasValue ? (startDate.Value.ToUniversalTime().AddYears(369).Ticks - 116444736000000000) / 10 : null;
                    long? endTimestamp = endDate.HasValue ? (endDate.Value.ToUniversalTime().AddYears(369).Ticks - 116444736000000000) / 10 : null;

                    string query = "SELECT title, url, last_visit_time FROM urls WHERE 1=1";
                    if (startTimestamp.HasValue)
                        query += " AND last_visit_time >= " + startTimestamp.Value;
                    if (endTimestamp.HasValue)
                        query += " AND last_visit_time <= " + endTimestamp.Value;
                    query += " ORDER BY last_visit_time DESC";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new BrowserHistoryEntry
                            {
                                Browser = "Chrome",
                                Title = reader["title"].ToString(),
                                URL = reader["url"].ToString(),
                                VisitTime = DateTimeOffset.FromUnixTimeSeconds(((long)reader["last_visit_time"]) / 1000000 - 11644473600).DateTime
                            });
                        }
                    }
                }

                File.Delete(tempFile);

                return history;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<PowerShellCommand> GetPowerShellCommands()
        {
            try
            {
                List<PowerShellCommand> powershellHistory = new List<PowerShellCommand>();
                string historyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\PowerShell\PSReadline\ConsoleHost_history.txt");

                if (!File.Exists(historyPath))
                    throw new Exception("PowerShell history file not found.");

                foreach (var line in File.ReadAllLines(historyPath))
                {
                    powershellHistory.Add(new PowerShellCommand { Command = line });
                }

                return powershellHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private List<RunCommand> GetRunCommands()
        {
            try
            {
                List<RunCommand> runHistory = new List<RunCommand>();
                string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            if (valueName != "MRUList")
                            {
                                string command = key.GetValue(valueName) as string;

                                if (!string.IsNullOrEmpty(command))
                                    runHistory.Add(new RunCommand { Command = command });
                            }
                        }
                    }
                }

                return runHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private List<USBDevice> GetUsbsHistory()
        {
            try
            {
                List<USBDevice> devices = new List<USBDevice>();
                string registryPath = @"SYSTEM\CurrentControlSet\Enum\USB";

                using (RegistryKey usbKey = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (usbKey != null)
                    {
                        foreach (string subKeyName in usbKey.GetSubKeyNames())
                        {
                            using (RegistryKey deviceKey = usbKey.OpenSubKey(subKeyName))
                            {
                                if (deviceKey != null)
                                {
                                    foreach (string deviceInstance in deviceKey.GetSubKeyNames())
                                    {
                                        using (RegistryKey instanceKey = deviceKey.OpenSubKey(deviceInstance))
                                        {
                                            if (instanceKey != null)
                                            {
                                                USBDevice device = new USBDevice
                                                {
                                                    DeviceDescription = instanceKey.GetValue("DeviceDesc") as string ?? "Unknown Device",
                                                    Manufacturer = instanceKey.GetValue("Mfg") as string ?? "Unknown Manufacturer",
                                                    DeviceID = subKeyName,
                                                    SerialNumber = deviceInstance
                                                };

                                                devices.Add(device);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return devices;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private List<WifiConnections> GetWifiHistory()
        {
            try
            {
                List<WifiConnections> wifiConnections = new List<WifiConnections>();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c netsh wlan show profiles",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    Regex regex = new Regex(@"All User Profile\s*:\s*(.+)");
                    MatchCollection matches = regex.Matches(result);

                    foreach (Match match in matches)
                    {
                        wifiConnections.Add(new WifiConnections { ConnectionName = match.Groups[1].Value.Trim() });
                    }
                }

                return wifiConnections;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public void ActivityHistoryRadioButtons_Checked()
        {
            try
            {
                if (_mainWindow.ActivityHistoryDataGrid != null)
                    _mainWindow.ActivityHistoryDataGrid.ItemsSource = null;

                if (_mainWindow.BrowserHistoryRadioButton != null)
                {
                    if (_mainWindow.BrowserHistoryRadioButton.IsChecked == true)
                    {
                        _mainWindow.StartDatePicker.IsEnabled = true;
                        _mainWindow.EndDatePicker.IsEnabled = true;
                        _mainWindow.ActivityHistoryDirectoryTextBox.Text = string.Empty;
                        _mainWindow.ActivityHistoryDirectoryTextBox.IsEnabled = false;

                        return;
                    }
                    else
                        _mainWindow.ActivityHistoryDirectoryTextBox.IsEnabled = true;
                }

                if (_mainWindow.PowershellCommandsRadioButton != null && _mainWindow.RunCommandsRadioButton != null
                    && _mainWindow.UsbsHistoryRadioButton != null && _mainWindow.WifiHistoryRadioButton != null)
                {
                    if (_mainWindow.PowershellCommandsRadioButton.IsChecked == true || _mainWindow.RunCommandsRadioButton.IsChecked == true
                        || _mainWindow.UsbsHistoryRadioButton.IsChecked == true || _mainWindow.WifiHistoryRadioButton.IsChecked == true)
                    {
                        _mainWindow.StartDatePicker.IsEnabled = false;
                        _mainWindow.EndDatePicker.IsEnabled = false;
                        _mainWindow.ActivityHistoryDirectoryTextBox.Text = string.Empty;
                        _mainWindow.ActivityHistoryDirectoryTextBox.IsEnabled = false;
                    }
                    else
                    {
                        _mainWindow.StartDatePicker.IsEnabled = true;
                        _mainWindow.EndDatePicker.IsEnabled = true;
                        _mainWindow.ActivityHistoryDirectoryTextBox.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public enum LastDateTime
    {
        Accessed,
        Created,
        Modified
    }

    public class FileDetails
    {
        public string FileName { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class BrowserHistoryEntry
    {
        public string Browser { get; set; }
        public string Title { get; set; }
        public string URL { get; set; }
        public DateTime? VisitTime { get; set; } = null;
    }

    public class PowerShellCommand
    {
        public string Command { get; set; }
    }

    public class RunCommand
    {
        public string Command { get; set; }
    }

    public class USBDevice
    {
        public string DeviceDescription { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceID { get; set; }
        public string SerialNumber { get; set; }
    }

    public class WifiConnections
    {
        public string ConnectionName { get; set; }
    }
}
