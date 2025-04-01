using System.Windows;
using System.IO;
using System.Windows.Controls;

namespace FileManagerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string CurrentDirectory;

        public MainWindow()
        {
            InitializeComponent();

            // При первом запуске устанавливать HOMEPATH, потом читать и писать в конфиг
            CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            OpenDirectory(CurrentDirectory);
        }

        // Открытие папки - до
        public void OpenDirectory(string directoryPath)
        {
            if (wrapPanel.Children.Count > 0)
            {
                wrapPanel.Children.Clear();
            }

            string[] files = GetDirectoryFilesAndDirectories(directoryPath);

            foreach (string file in files)
            {
                Button newButton = new Button
                {
                    Content = file,
                    //Width = auto,
                    Height = 30
                };

                wrapPanel.Children.Add(newButton);
            }
        }

        public string[] GetDirectoryFilesAndDirectories(string directoryPath)
        {
            string[] directories = Directory.GetDirectories(directoryPath);
            string[] files = Directory.GetFiles(directoryPath);

            string[] directoriesAndFiles = new string[directories.Length + files.Length];

            directories.CopyTo(directoriesAndFiles, 0);
            files.CopyTo(directoriesAndFiles, directories.Length);

            return directoriesAndFiles;
        }
    }
}