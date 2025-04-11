using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

            LoadDirectoryTree();

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
            
            if (directoryPath != null && directoryPath.StartsWith("~"))
            {
                directoryPath = directoryPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                ChangeDirectory(directoryPath, isNavigatingHistory);

                return;
            }

            if (!Directory.Exists(directoryPath))
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
            if (isNavigatingHistory)
            {
                _currentDirectoryIndex = _directoriesPath.IndexOf(newDirectoryPath);
            }
            else if (!isNavigatingHistory)
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

                    dirButton.ContextMenu = CreateDirectoryContextMenu(dir);

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

                    fileButton.ContextMenu = CreateFileContextMenu(file);
                    
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
            if (_currentDirectoryIndex == 0)
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
            if (_currentDirectoryIndex >= _directoriesPath.Count - 1)
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
            if (_directoriesPath.Count == 0)
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

        private ContextMenu CreateDirectoryContextMenu(string directoryPath)
        {
            var menu = new ContextMenu();

            var openItem = new MenuItem { Header = "Открыть" };
            openItem.Click += (s, e) => ChangeDirectory(directoryPath, false);

            var copyItem = new MenuItem { Header = "Копировать" };
            copyItem.Click += (s, e) => CopyToClipboard(directoryPath, isCut: false);

            var cutItem = new MenuItem { Header = "Вырезать" };
            cutItem.Click += (s, e) => CopyToClipboard(directoryPath, isCut: true);

            var pasteItem = new MenuItem { Header = "Вставить" };
            pasteItem.Click += (s, e) => PasteFromClipboard(directoryPath);
            pasteItem.IsEnabled = Clipboard.ContainsFileDropList();

            var deleteItem = new MenuItem { Header = "Удалить" };
            deleteItem.Click += (s, e) => DeleteItem(directoryPath);

            var propertiesItem = new MenuItem { Header = "Свойства" };
            propertiesItem.Click += (s, e) => ShowProperties(directoryPath);

            menu.Items.Add(openItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(copyItem);
            menu.Items.Add(cutItem);
            menu.Items.Add(pasteItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(propertiesItem);

            return menu;
        }

        private ContextMenu CreateFileContextMenu(string filePath)
        {
            var menu = new ContextMenu();

            var openItem = new MenuItem { Header = "Открыть" };
            openItem.Click += (s, e) => OpenFile(filePath);

            var copyItem = new MenuItem { Header = "Копировать" };
            copyItem.Click += (s, e) => CopyToClipboard(filePath, isCut: false);

            var cutItem = new MenuItem { Header = "Вырезать" };
            cutItem.Click += (s, e) => CopyToClipboard(filePath, isCut: true);

            var deleteItem = new MenuItem { Header = "Удалить" };
            deleteItem.Click += (s, e) => DeleteItem(filePath);

            var propertiesItem = new MenuItem { Header = "Свойства" };
            propertiesItem.Click += (s, e) => ShowProperties(filePath);

            menu.Items.Add(openItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(copyItem);
            menu.Items.Add(cutItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(propertiesItem);

            return menu;
        }

        // Вспомогательные методы для операций
        private void CopyToClipboard(string path, bool isCut)
        {
            try
            {
                var fileList = new System.Collections.Specialized.StringCollection();
                fileList.Add(path);

                var dataObject = new DataObject();
                dataObject.SetFileDropList(fileList);

                // Устанавливаем флаг для операции "Вырезать"
                MemoryStream dropEffect = new MemoryStream();
                byte[] effect = isCut ? new byte[] { 2, 0, 0, 0 } : new byte[] { 1, 0, 0, 0 };
                dropEffect.Write(effect, 0, effect.Length);
                dataObject.SetData("Preferred DropEffect", dropEffect);

                Clipboard.SetDataObject(dataObject, copy: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования в буфер обмена: {ex.Message}");
            }
        }

        private void PasteFromClipboard(string targetDirectory)
        {
            if (!Clipboard.ContainsFileDropList())
            {
                MessageBox.Show("Буфер обмена не содержит файлов для вставки");
                return;
            }

            try
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToList();
                bool isCut = IsCutOperation();
                int successCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        string destPath = Path.Combine(targetDirectory, fileName);

                        // Проверка на попытку вставки папки в саму себя
                        if (Directory.Exists(file) &&
                            Path.GetFullPath(destPath).StartsWith(Path.GetFullPath(file) + Path.DirectorySeparatorChar))
                        {
                            MessageBox.Show($"Невозможно скопировать папку '{fileName}' в саму себя",
                                          "Ошибка операции");
                            continue;
                        }

                        // Проверка на существование файла/папки с таким именем
                        if (File.Exists(destPath) || Directory.Exists(destPath))
                        {
                            var result = MessageBox.Show(
                                $"Элемент '{fileName}' уже существует. Заменить?",
                                "Подтверждение",
                                MessageBoxButton.YesNo);

                            if (result != MessageBoxResult.Yes)
                                continue;
                        }

                        if (isCut)
                        {
                            // Операция "Вырезать"
                            if (File.Exists(file))
                            {
                                File.Move(file, destPath, overwrite: true);
                                successCount++;
                            }
                            else if (Directory.Exists(file))
                            {
                                // Дополнительная проверка для перемещения папок
                                if (!Path.GetFullPath(destPath).Equals(Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase))
                                {
                                    Directory.Move(file, destPath);
                                    successCount++;
                                }
                            }
                        }
                        else
                        {
                            // Операция "Копировать"
                            if (File.Exists(file))
                            {
                                File.Copy(file, destPath, overwrite: true);
                                successCount++;
                            }
                            else if (Directory.Exists(file))
                            {
                                DirectoryCopy(file, destPath);
                                successCount++;
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show($"Нет прав доступа для операции с '{Path.GetFileName(file)}'");
                    }
                    catch (IOException ioEx)
                    {
                        MessageBox.Show($"Ошибка ввода-вывода: {ioEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обработке '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }

                if (successCount > 0)
                {
                    //LoadDirectory(targetDirectory);
                    string operation = isCut ? "перемещено" : "скопировано";
                    MessageBox.Show($"Успешно {operation} {successCount} элементов", "Готово");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вставки: {ex.Message}", "Ошибка");
            }
        }

        // Вспомогательный метод для проверки операции "Вырезать"
        private bool IsCutOperation()
        {
            if (Clipboard.ContainsData("Preferred DropEffect"))
            {
                try
                {
                    var dropEffect = Clipboard.GetData("Preferred DropEffect") as MemoryStream;
                    if (dropEffect != null)
                    {
                        byte[] effect = new byte[4];
                        dropEffect.Read(effect, 0, 4);
                        return BitConverter.ToInt32(effect, 0) == 2; // 2 = Move (Cut)
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private void DeleteItem(string path)
        {
            if (MessageBox.Show("Удалить выбранный элемент?", "Подтверждение",
                               MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    else if (Directory.Exists(path))
                        Directory.Delete(path, true);

                    LoadDirectory(CurrentDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void ShowProperties(string path)
        {
            var propertiesWindow = new FilePropertiesWindow(path)
            {
                Owner = this
            };

            if (propertiesWindow.ShowDialog() == true && propertiesWindow.NewPath != null)
            {
                // Если было выполнено переименование
                if (CurrentDirectory.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    // Если переименовали текущую директорию
                    CurrentDirectory = propertiesWindow.NewPath;
                    TextBoxCurrentPath.Text = CurrentDirectory;
                }

                // Обновляем содержимое текущей директории
                LoadDirectory(Path.GetDirectoryName(path) ?? CurrentDirectory);
            }
        }

        private ContentControl GetParentContainer(string path)
        {
            return wrapPanel.Children.OfType<Button>()
                .FirstOrDefault(b => b.Tag.ToString() == path) as ContentControl;
        }

        private void DirectoryCopy(string sourceDir, string destDir)
        {
            // Рекурсивное копирование директории
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                DirectoryCopy(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        private void DirectoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DirectoryItem selectedItem)
            {
                ChangeDirectory(selectedItem.FullPath, false);
            }
        }

        private void DirectoryTreeView_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem treeViewItem &&
                treeViewItem.Header is DirectoryItem directoryItem)
            {
                // Проверяем, есть ли фиктивный элемент для загрузки
                if (directoryItem.Children.Count == 1 &&
                    string.IsNullOrEmpty(directoryItem.Children[0].Name) ||
                    directoryItem.Children[0].Name == "Loading...")
                {
                    // Загружаем поддиректории
                    LoadSubdirectories(directoryItem);
                }
            }
        }

        private void LoadDirectoryTree()
        {
            DirectoryTreeView.Items.Clear();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                try
                {
                    string driveName = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                        ? $"{drive.Name}"
                        : $"{drive.Name} {drive.VolumeLabel}";

                    var driveItem = new DirectoryItem
                    {
                        Name = driveName,
                        FullPath = drive.RootDirectory.FullName,
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Images/disk.png"))
                    };

                    // Загружаем поддиректории сразу
                    LoadSubdirectories(driveItem);
                    DirectoryTreeView.Items.Add(driveItem);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading drive {drive.Name}: {ex.Message}");
                }
            }
        }

        private void LoadSubdirectories(DirectoryItem parentItem)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(parentItem.FullPath))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        var item = new DirectoryItem
                        {
                            Name = dirInfo.Name,
                            FullPath = dirInfo.FullName,
                            Icon = new BitmapImage(new Uri("pack://application:,,,/Images/folder.png")),
                        };

                        LoadSubdirectories(item);
                        parentItem.Children.Add(item);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Пропускаем папки без доступа
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Пропускаем если нет доступа к родительской папке
            }
        }
    }
}
