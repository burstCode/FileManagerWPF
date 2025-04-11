using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FileManagerWPF
{
    public class DirectoryItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public ImageSource Icon { get; set; }
        public bool HasSubdirectories { get; set; }
        public ObservableCollection<DirectoryItem> Children { get; } = new ObservableCollection<DirectoryItem>();
    }
}
