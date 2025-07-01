using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Text.Json.Serialization;

namespace NotesApp.Models
{
    public class Note : INotifyPropertyChanged
    {
        private string title;
        private string content;
        private string category;
        private DateTime createdAt;
        private string highlightColorHex;

        public string Title
        {
            get => title;
            set
            {
                if (value != title)
                {
                    title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Content
        {
            get => content;
            set
            {
                if (value != content)
                {
                    content = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Category
        {
            get => category;
            set
            {
                if (value != category)
                {
                    category = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> Attachments { get; set; } = new ObservableCollection<string>();

        public DateTime CreatedAt
        {
            get => createdAt;
            set
            {
                if (value != createdAt)
                {
                    createdAt = value;
                    OnPropertyChanged();
                }
            }
        }

        // Преобразованный цвет в HEX-строку для сериализации
        [JsonIgnore]
        public Brush HighlightColor
        {
            get => string.IsNullOrEmpty(highlightColorHex) ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(highlightColorHex));
            set
            {
                if (value != null)
                {
                    highlightColorHex = value.ToString();
                    OnPropertyChanged();
                }
            }
        }

        // Сериализуемое свойство для цвета в строковом формате HEX
        public string HighlightColorHex
        {
            get => highlightColorHex;
            set
            {
                if (highlightColorHex != value)
                {
                    highlightColorHex = value;
                    OnPropertyChanged();
                }
            }
        }

        // Новый метод для обновления цвета
        public void UpdateHighlightColor(Color color)
        {
            HighlightColor = new SolidColorBrush(color);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Note()
        {
            CreatedAt = DateTime.Now;
            HighlightColorHex = "#00000000";
        }
    }
}
