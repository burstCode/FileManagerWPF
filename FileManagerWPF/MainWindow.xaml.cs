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
        private string CurrentDirectory;

        public MainWindow()
        {
            InitializeComponent();

            // TODO: При первом запуске устанавливать HOMEPATH, потом читать и писать в конфиг
            CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            ChangeDirectory(CurrentDirectory);
        }

        // Смена директории
        public void ChangeDirectory(string directoryPath)
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
                    dirButton.Click += (s, e) => ChangeDirectory(dir);

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
    }
}