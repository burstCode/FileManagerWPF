using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FileManagerWPF
{
    /// <summary>
    /// Логика взаимодействия для FilePropertiesWindow.xaml
    /// </summary>
    public partial class FilePropertiesWindow : Window
    {
        private readonly string _originalPath;
        private readonly bool _isDirectory;

        public string NewPath { get; private set; }

        public FilePropertiesWindow(string path)
        {
            InitializeComponent();
            _originalPath = path;
            _isDirectory = Directory.Exists(path);

            LoadProperties();
        }

        private void LoadProperties()
        {
            // Устанавливаем иконку
            ItemIcon.Source = _isDirectory
                ? new BitmapImage(new Uri($"pack://application:,,,/Images/folder.png", UriKind.Absolute))
                : new BitmapImage(new Uri($"pack://application:,,,/Images/file.png", UriKind.Absolute));

            // Заполняем информацию
            NameTextBox.Text = System.IO.Path.GetFileName(_originalPath);
            TypeText.Text = _isDirectory ? "Папка" : "Файл";
            LocationText.Text = System.IO.Path.GetDirectoryName(_originalPath);

            if (_isDirectory)
            {
                SizeText.Text = "Папка с файлами";
            }
            else
            {
                var fileInfo = new FileInfo(_originalPath);
                SizeText.Text = $"{fileInfo.Length / 1024} KB";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Имя не может быть пустым");
                return;
            }

            if (newName == System.IO.Path.GetFileName(_originalPath))
            {
                DialogResult = false;
                Close();
                return;
            }

            try
            {
                string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_originalPath), newName);

                if (File.Exists(newPath) || Directory.Exists(newPath))
                {
                    MessageBox.Show("Элемент с таким именем уже существует");
                    return;
                }

                if (_isDirectory)
                {
                    Directory.Move(_originalPath, newPath);
                }
                else
                {
                    File.Move(_originalPath, newPath);
                }

                NewPath = newPath;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переименования: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
