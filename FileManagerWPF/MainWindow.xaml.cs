using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace FileManagerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* 
         * LimitedArray позволяет хранить 10 последних путей, удаляя пути,
         * которые хранились более 10 запросов назад. 
         */
        private LimitedArray<string> _directoriesPath;

        // Путь к текущей директории
        public string CurrentDirectory;

        // Индекс для отслеживания в _directoriesPath
        private int _currentDirectoryIndex;

        public MainWindow()
        {
            InitializeComponent();

            _directoriesPath = new LimitedArray<string>(10);
            CurrentDirectory = "";
            _currentDirectoryIndex = -1;

            ChangeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), false);
        }

        // Смена директории
        public void ChangeDirectory(string directoryPath, bool isNavigatingHistory)
        {
            if (CurrentDirectory == directoryPath)
            {
                return;
            }

            if (directoryPath == "/")
            {
                UpdateCurrentDirectory("/", isNavigatingHistory);
                LoadRoot();

                return;
            }

            if ( !Directory.Exists(directoryPath) )
            {
                MessageBox.Show("Ты долбоеб, куда намылился", "Хуесос", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // Обновление текущей директории
            UpdateCurrentDirectory(directoryPath, isNavigatingHistory);

            // Загрузка текущей директории
            LoadDirectory(directoryPath);
        }

        // Обновление текущей директории в поле "адресной строки" и обновление истории
        private void UpdateCurrentDirectory(string newDirectoryPath, bool isNavigatingHistory)
        {
            // Если в произвольном месте истории директорий мы переходим в отличающуюся
            // от следующей в истории, то очищаем историю после текущей директории
            if ( isNavigatingHistory )
            {
                _currentDirectoryIndex = _directoriesPath.IndexOf(newDirectoryPath);
            }
            else if ( !isNavigatingHistory )
            {
                // Если это не навигация по истории, обрабатываем как новый путь
                if (_currentDirectoryIndex != -1 && _currentDirectoryIndex < _directoriesPath.Count - 1)
                {
                    // Если мы не в конце истории, удаляем всё после текущего индекса
                    _directoriesPath.ClearAfterIndex(_currentDirectoryIndex);
                }

                // Добавляем новый путь только если он отличается от текущего
                if (_directoriesPath.Count == 0 || _directoriesPath.Get(_currentDirectoryIndex) != newDirectoryPath)
                {
                    _directoriesPath.Add(newDirectoryPath);
                    _currentDirectoryIndex = _directoriesPath.Count - 1;
                }
            }

            CurrentDirectory = newDirectoryPath;
            TextBoxCurrentPath.Text = CurrentDirectory;
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

                ButtonGoUp_Click(new object(), new RoutedEventArgs());
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Директория не найдена");
            }
        }

        // Загрузка корневой директории (отображение дисков)
        private void LoadRoot()
        {
            string[] drives = Directory.GetLogicalDrives();

            // Очистка содержимого прошлой директории
            if (wrapPanel.Children.Count > 0)
            {
                wrapPanel.Children.Clear();
            }

            foreach (var drive in drives)
            {
                var driveInfo = new DriveInfo(drive);
                Button driveButton = new Button
                {
                    Style = (Style)FindResource("ButtonDriveStyle"),
                    Content = driveInfo.Name,
                    Tag = drive
                };

                driveButton.Click += (s, e) => ChangeDirectory(driveInfo.Name, false);

                wrapPanel.Children.Add(driveButton);
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

        ////////////////////////////// Кнопки управления //////////////////////////////

        // Возврат на предыдущую директорию в истории
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

        // Переход на следующую директорию в истории
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

        // Открытие истории переходов по директориям
        private void ButtonDirectoriesHistory_Click(object sender, RoutedEventArgs e)
        {
            if ( _directoriesPath.Count == 0 )
            {
                MessageBox.Show("История пуста", "Сперва перейдите по ряду директорий!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var button = (Button)sender;
            var contextMenu = new ContextMenu();

            // Создаем элементы меню в обратном порядке (новые сверху)
            for (int i = _directoriesPath.Count - 1; i >= 0; i--)
            {
                var path = _directoriesPath.Get(i);
                var menuItem = new MenuItem
                {
                    Header = GetDisplayPath(path),
                    Tag = path,
                    FontWeight = i == _currentDirectoryIndex ? FontWeights.Bold : FontWeights.Normal
                };

                menuItem.Click += (s, args) =>
                {
                    var selectedPath = (string)((MenuItem)s).Tag;
                    ChangeDirectory(selectedPath, true);
                };

                contextMenu.Items.Add(menuItem);
            }

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        // Вспомогательный метод для отображения пути
        private string GetDisplayPath(string fullPath)
        {
            // Сокращаем путь для отображения
            if (fullPath.Length > 50)
            {
                return "..." + fullPath.Substring(fullPath.Length - 47);
            }
            return fullPath;
        }

        // Переход в родительскую директорию
        private void ButtonGoUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeDirectory(Directory.GetParent(CurrentDirectory).FullName, false);
            }
            catch (Exception ex)
            {
                ChangeDirectory("/", false);
            }
        }

        // Открытие командной строки в текущей директории
        private void ButtonCMD_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = CurrentDirectory,
                FileName = "cmd.exe"
            });
        }

        private void TextBoxCurrentPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string currentPath = TextBoxCurrentPath.Text;

                ChangeDirectory(currentPath, false);

                e.Handled = true;
            }
        }
    }
}
