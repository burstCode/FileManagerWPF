using System.Windows.Controls.Primitives;

namespace FileManagerWPF
{
    public class LimitedArray<T>
    {
        private List<T> _items;
        private readonly int _size;

        public LimitedArray(int size)
        {
            _size = size;
            _items = new List<T> ();
        }

        public void Add(T item)
        {
            if (_items.Count >= _size)
            {
                _items.RemoveAt(0);
            }
            _items.Add(item);
        }

        public T Get(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Индекс вышел из диапазона.");
            }
            return _items[index];
        }
        
        public int Count => _items.Count;

        public bool IsEmpty => _items.Count == 0;

        public bool IsFull => _items.Count >= _size;

        // Очистка элементов после индекса
        public void ClearAfterIndex(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int itemsToRemove = _items.Count - index - 1;
            if (itemsToRemove > 0)
            {
                _items.RemoveRange(index + 1, itemsToRemove);
            }
        }
    }
}
