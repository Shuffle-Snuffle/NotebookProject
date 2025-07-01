using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using NotesApp.Models;
using Newtonsoft.Json;

namespace NotesApp.Services
{
    public static class NoteManager
    {
        private static string filePath = "notes.json";

        public static List<Note> LoadNotes()
        {
            if (!File.Exists(filePath)) return new List<Note>();

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Note>>(json) ?? new List<Note>();
        }

        public static void SaveNotes(List<Note> notes)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(notes, settings);
            File.WriteAllText(filePath, json);
        }
    }
}
