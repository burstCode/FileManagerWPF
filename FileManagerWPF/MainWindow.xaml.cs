using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;

namespace FileManagerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string CurrentDirectory;

        /* 
         * LimitedArray позволяет хранить 10 последних путей, удаляя пути,
         * которые хранились более 10 запросов назад. 
         */
        private LimitedArray<string> _directoriesPath;

        // Индекс для отслеживания в _directoriesPath
        private int _currentDirectoryIndex;

        public MainWindow()
        {
            InitializeComponent();

            // TODO: При первом запуске устанавливать HOMEPATH, потом читать и писать в конфиг
            CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            _directoriesPath = new LimitedArray<string>(10);

            ChangeDirectory(CurrentDirectory, false);

            _currentDirectoryIndex = 0;
        }

        // Смена директории
        public void ChangeDirectory(string directoryPath, bool isSwitchedByHistory)
        {
            // Обновление текущей директории
            UpdateCurrentDirectory(directoryPath, isSwitchedByHistory);

            LoadDirectory(directoryPath);
        }

        // Загрузка содержимого директории
        private void LoadDirectory(string directoryPath)
        {
            // Очистка содержимого прошлой директории
            if (wrapPanel.Children.Count > 0)
            {
                wrapPanel.Children.Clear();
            }

            try
            {
                // Получаем все поддиректории
                foreach (var dir in Directory.GetDirectories(directoryPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    Button dirButton = new Button
                    {
                        Style = (Style)FindResource("ButtonFolderStyle"),
                        Content = dirInfo.Name,
                        Tag = dir
                    };

                    // Добавляем обработчик клика для входа в директорию
                    dirButton.Click += (s, e) => ChangeDirectory(dir, false);

                    wrapPanel.Children.Add(dirButton);
                }

                // Получаем все файлы
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    var fileInfo = new FileInfo(file);
                    Button fileButton = new Button
                    {
                        Style = (Style)FindResource("ButtonFileStyle"),
                        Content = fileInfo.Name,
                        Tag = file
                    };

                    // Добавляем обработчик клика для файла
                    fileButton.Click += (s, e) => OpenFile(file);

                    wrapPanel.Children.Add(fileButton);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет доступа к директории");
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Директория не найдена");
            }
        }

        // Метод для открытия файла
        private void OpenFile(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}");
            }
        }

        // Обновление текущей директории в поле "адресной строки"
        private void UpdateCurrentDirectory(string newDirectoryPath, bool isSwitchedByHistory)
        {
            CurrentDirectory = newDirectoryPath;
            TextBoxCurrentPath.Text = CurrentDirectory;

            if ( !isSwitchedByHistory )
            {
                // Добавление в очередь путей нового
                _directoriesPath.Add(CurrentDirectory);

                if (_currentDirectoryIndex < 9)
                {
                    _currentDirectoryIndex++;
                }
            }
            //else
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            if ( _currentDirectoryIndex == 0 )
            {
                return;
            }

            _currentDirectoryIndex--;

            ChangeDirectory(_directoriesPath.Get(_currentDirectoryIndex), true);

            TextBoxCurrentPath.Text = CurrentDirectory;
        }

        private void ButtonForward_Click(object sender, RoutedEventArgs e)
        {
            if ( _currentDirectoryIndex >= _directoriesPath.Count - 1 )
            {
                return;
            }

            _currentDirectoryIndex++;

            ChangeDirectory(_directoriesPath.Get(_currentDirectoryIndex), true);

            TextBoxCurrentPath.Text = CurrentDirectory;
        }

        private void ButtonDirectoriesHistory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonGoUp_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonCMD_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = CurrentDirectory,
                FileName = "cmd.exe"
            });
        }
    }
}
