using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace TestMahDisk {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        System.Threading.Thread testThread = null;
        public MainWindow() {
            InitializeComponent();

            foreach (var drive in DriveInfo.GetDrives()) {
                try {
                    double freeSpace = drive.TotalFreeSpace;
                    double totalSpace = drive.TotalSize;
                    double percentFree = (freeSpace / totalSpace) * 100;
                    float num = (float)percentFree;

                    if (drive.DriveType == DriveType.Removable) {
                        comboBox.Items.Add(drive.Name);
                    }
                    comboBox.Items.Add(drive.Name);
                } catch {

                } 
            }
        }

        private void button_Click(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Running this tool might damage the data on your drive, or the drive itself.\nWe are not to be held liable for any damages!\nDo you want to test your drive anyways?", "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No){
                //do no stuff
            }else{
                
                testThread = new Thread(new ThreadStart(runTest));
                testThread.Start();
            }           
        }

        public bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        void runTest () {
            string selectedDrive = "C:\\";

            if (comboBox.Dispatcher.CheckAccess()) {
                selectedDrive = (comboBox.SelectedItem).ToString();
            } else {
                Dispatcher.Invoke((Action)delegate () {
                    selectedDrive = (comboBox.SelectedItem).ToString();
                });
            }
            if (IsDirectoryEmpty(selectedDrive)){
                this.Dispatcher.Invoke(() => { label4.Content = "Generating random testfile..."; });
                const int blockSizeMb = 16;
                const int blockSize = 1024 * blockSizeMb;
                const int blocksPerMb = (1024 * 1024) / blockSize;
                byte[] data = new byte[blockSize];
                Random rng = new Random();

                int sizeInMb = 8;
                string cacheFile = Environment.CurrentDirectory + "/cache.tmd";
                using (FileStream stream = File.OpenWrite(cacheFile)) {
                    // There 
                    for (int i = 0; i < sizeInMb * blocksPerMb; i++) {
                        rng.NextBytes(data);
                        stream.Write(data, 0, data.Length);
                    }
                }
                this.Dispatcher.Invoke(() => { label4.Content = "Creating checksum of testfile..."; });
                var md5sum = HashFile(cacheFile);
                this.Dispatcher.Invoke(() => { label3.Content = "Cache Checksum: " + md5sum; });


                // calculate amount of files needed to fill drive
                this.Dispatcher.Invoke(() => { label4.Content = "Checking files needed for test..."; });
                var driveinfo = new System.IO.DriveInfo(selectedDrive);
                var filesNeeded = (((driveinfo.TotalSize / 1024) / 1024) / sizeInMb) - 1;

                this.Dispatcher.Invoke(() => { label4.Content = "Testing drive..."; });
                int filesDone = 0;
                bool fail = false;
                while (filesDone < filesNeeded) {
                    string fileTarget = selectedDrive + "/TestMahDisk_" + RandomString(8) + ".tmd";
                    try {
                        File.Copy(cacheFile, fileTarget);
                        if (HashFile(cacheFile) == HashFile(fileTarget)) {
                            this.Dispatcher.Invoke(() => { label2.Content = "Tested: " + ((filesDone + 1) * ((sizeInMb * 1024) / 1024)) + "MB"; });
                        } else {
                            Console.WriteLine("checksum mismatch!" + filesDone);
                            break;
                        }
                        filesDone += 1;
                    } catch {
                        fail = true;
                        Console.WriteLine("Error!");
                    }
                }
                this.Dispatcher.Invoke(() => { label4.Content = "Cleaning up..."; });
                var filesToDelete = Directory.EnumerateFiles(selectedDrive, "TestMahDisk_*.tmd");
                foreach (var fileToDelete in filesToDelete) {
                    try {
                        File.Delete(fileToDelete);
                    } catch (Exception ex) {
                        // log this...
                    }
                }
                this.Dispatcher.Invoke(() => { label4.Content = "Done"; });
                if (fail) {
                    MessageBox.Show("Done\nDrive might be defective!");
                } else {
                    MessageBox.Show("Done");
                }
               
            } else {
                MessageBox.Show("Drive not empty! can't continue!");
            }
            
        }

        public string HashFile(string filePath) {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                return HashFile(fs);
            }
        }

        public string HashFile(FileStream stream) {
            StringBuilder sb = new StringBuilder();

            if (stream != null) {
                stream.Seek(0, SeekOrigin.Begin);

                MD5 md5 = MD5CryptoServiceProvider.Create();
                byte[] hash = md5.ComputeHash(stream);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));

                stream.Seek(0, SeekOrigin.Begin);
            }

            return sb.ToString();
        }

        private static Random random = new Random();
        public static string RandomString(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var driveinfo = new System.IO.DriveInfo((comboBox.SelectedItem).ToString());
            label1.Content = "Claimed Size: " + ((driveinfo.TotalSize / 1024) / 1024) + "MB";
        }
    }
}
