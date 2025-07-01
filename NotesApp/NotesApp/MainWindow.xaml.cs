using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using NotesApp.Models;
using NotesApp.Services;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;

namespace NotesApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Note> notes;
        private CollectionViewSource notesViewSource;

        private Note selectedNote;

        private List<string> categories = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            var loadedNotes = NoteManager.LoadNotes() ?? new List<Note>();
            notes = new ObservableCollection<Note>(loadedNotes);

            UpdateCategories();

            notesViewSource = new CollectionViewSource { Source = notes };
            notesViewSource.Filter += NotesViewSource_Filter;

            NotesList.ItemsSource = notesViewSource.View;

            CategoryFilterComboBox.SelectionChanged += CategoryFilterComboBox_SelectionChanged;
            SearchBox.TextChanged += SearchBox_TextChanged;
            NotesList.SelectionChanged += NotesList_SelectionChanged;

            if (notes.Count > 0)
                NotesList.SelectedIndex = 0;

            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NoteColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SelectedNote != null && e.NewValue.HasValue)
            {
                SelectedNote.HighlightColor = new SolidColorBrush(e.NewValue.Value);
                NotesList.Items.Refresh();
            }
        }

        protected void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Note SelectedNote
        {
            get => selectedNote;
            set
            {
                if (selectedNote != value)
                {
                    selectedNote = value;
                    OnPropertyChanged(nameof(SelectedNote));

                    if (selectedNote != null)
                    {
                        TitleBox.Text = selectedNote.Title;
                        ContentBox.Text = selectedNote.Content;
                        CategoryBox.Text = selectedNote.Category;
                        AttachmentsList.ItemsSource = selectedNote.Attachments;
                    }
                    else
                    {
                        TitleBox.Text = ContentBox.Text = CategoryBox.Text = "";
                        AttachmentsList.ItemsSource = null;
                    }
                }
            }
        }

        private void NotesViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is Note note)
            {
                var query = SearchBox.Text.ToLower();
                var selectedCategory = CategoryFilterComboBox.SelectedItem as string;

                bool categoryMatches = selectedCategory == "Все категории" ||
                                       (!string.IsNullOrEmpty(note.Category) && note.Category.Equals(selectedCategory));

                bool textMatches = string.IsNullOrWhiteSpace(query) ||
                                   (note.Title?.ToLower().Contains(query) == true) ||
                                   (note.Content?.ToLower().Contains(query) == true) ||
                                   (note.Category?.ToLower().Contains(query) == true);

                e.Accepted = categoryMatches && textMatches;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedNote = NotesList.SelectedItem as Note;
        }

        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            var newNote = new Note
            {
                Title = "Новая заметка",
                Attachments = new ObservableCollection<string>()
            };
            notes.Add(newNote);
            notesViewSource.View.Refresh();
            NotesList.SelectedItem = newNote;

            UpdateCategories();
        }
        private void CleanUpBrokenLinks()
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string attachmentsDirectory = Path.Combine(projectDirectory, "Attachments");

            if (Directory.Exists(attachmentsDirectory))
            {
                var allFiles = Directory.GetFiles(attachmentsDirectory, "*.*", SearchOption.AllDirectories);

                foreach (var note in notes)
                {
                    for (int i = note.Attachments.Count - 1; i >= 0; i--)
                    {
                        var attachment = note.Attachments[i];
                        string attachmentPath = Path.Combine(attachmentsDirectory, SanitizeFileName(attachment));

                        if (!File.Exists(attachmentPath))
                        {
                            note.Attachments.RemoveAt(i);
                        }
                    }
                }

                foreach (var file in allFiles)
                {
                    bool fileReferenced = false;

                    foreach (var note in notes)
                    {
                        if (note.Attachments.Contains(Path.GetFileName(file)))
                        {
                            fileReferenced = true;
                            break;
                        }
                    }

                    if (!fileReferenced)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (UnauthorizedAccessException)
                        {

                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
        }
        private void OpenAttachmentFolder_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNote == null || string.IsNullOrWhiteSpace(SelectedNote.Title))
            {
                MessageBox.Show("Сначала выберите заметку.");
                return;
            }

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments", SanitizeFileName(SelectedNote.Title));

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("Папка вложений не найдена.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть папку.");
            }
        }

        private void LogMessage(string message)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_log.txt");

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNote != null)
            {
                SelectedNote.Title = TitleBox.Text;
                SelectedNote.Content = ContentBox.Text;
                SelectedNote.Category = CategoryBox.Text;

                notesViewSource.View.Refresh();
                NoteManager.SaveNotes(notes.ToList());

                UpdateCategories();
            }
        }

        private void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNote != null)
            {
                notes.Remove(SelectedNote);
                notesViewSource.View.Refresh();
                NoteManager.SaveNotes(notes.ToList());

                SelectedNote = null;

                UpdateCategories();
            }
        }

        private void AddAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNote == null) return;

            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                string noteDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments", SanitizeFileName(SelectedNote.Title));
                Directory.CreateDirectory(noteDir);

                foreach (var filePath in dialog.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationPath = Path.Combine(noteDir, fileName);

                    try
                    {
                        File.Copy(filePath, destinationPath, overwrite: true);

                        string relativePath = Path.Combine("Attachments", SanitizeFileName(SelectedNote.Title), fileName);

                        if (!SelectedNote.Attachments.Contains(relativePath))
                            SelectedNote.Attachments.Add(relativePath);
                    }
                    catch
                    {
                        MessageBox.Show($"Не удалось добавить вложение: {fileName}");
                    }
                }

                AttachmentsList.Items.Refresh();
            }
        }

        private void Attachment_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is string path)
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = fullPath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        MessageBox.Show("Не удалось открыть файл.");
                    }
                }
                else
                {
                    MessageBox.Show("Файл не найден. Возможно, он был удалён. Удалите ссылку.");
                }
            }
        }

        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is string filePath && SelectedNote?.Attachments != null)
            {
                SelectedNote.Attachments.Remove(filePath);
                AttachmentsList.Items.Refresh();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            notesViewSource.View.Refresh();
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            notesViewSource.View.Refresh();
        }

        private void UpdateCategories()
        {
            var newCategories = notes.Select(n => n.Category)
                                     .Where(c => !string.IsNullOrWhiteSpace(c))
                                     .Distinct()
                                     .OrderBy(c => c)
                                     .ToList();

            newCategories.Insert(0, "Все категории");

            var prevSelection = CategoryFilterComboBox.SelectedItem as string;

            categories = newCategories;
            CategoryFilterComboBox.ItemsSource = categories;

            if (prevSelection != null && categories.Contains(prevSelection))
                CategoryFilterComboBox.SelectedItem = prevSelection;
            else
                CategoryFilterComboBox.SelectedIndex = 0;
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return fileName;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void DeleteAllNotes_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить все заметки?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                notes.Clear();

                notesViewSource.View.Refresh();

                NoteManager.SaveNotes(notes.ToList());

                UpdateCategories();
            }
        }

        private void MaxRestore_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                MaxRestoreButton.Content = "🗗";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                MaxRestoreButton.Content = "🗖";
            }
        }
    }
}
