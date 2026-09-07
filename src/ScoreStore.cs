using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace NeonStack
{
    [DataContract]
    public sealed class ScoreEntry
    {
        [DataMember] public string Id;
        [DataMember] public string Initials;
        [DataMember] public int Score;
        [DataMember] public int Level;
        [DataMember] public int Lines;
        [DataMember] public DateTime Date;
    }

    [DataContract]
    public sealed class ScoreData
    {
        [DataMember] public string LastInitials = "AAA";
        [DataMember] public List<ScoreEntry> Entries = new List<ScoreEntry>();
        [DataMember] public int? Volume;
        [DataMember] public bool Muted;
        [DataMember] public bool Compact;
        [DataMember] public bool OnTop;
    }

    public sealed class ScoreStore
    {
        public string FilePath { get; private set; }
        public string Notice { get; private set; }
        public ScoreData Data { get; private set; }
        public int Best { get { return Data.Entries.Count == 0 ? 0 : Data.Entries[0].Score; } }
        private bool corruptPrimary;

        public ScoreStore(string directory)
        {
            FilePath = Path.Combine(directory, "scores.json"); Data = new ScoreData();
            if (!File.Exists(FilePath)) return;
            try { Data = Read(FilePath); }
            catch (Exception ex)
            {
                if (!IsStorageError(ex)) throw;
                corruptPrimary = true;
                Notice = "Archivo dañado. Récords nuevos disponibles.";
                try { Data = Read(FilePath + ".bak"); Notice = "Récords recuperados de la copia de seguridad."; }
                catch (Exception backupEx) { if (!IsStorageError(backupEx)) throw; }
            }
        }

        private static bool IsStorageError(Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException || ex is SerializationException || ex is System.Xml.XmlException || ex is ArgumentException;
        }

        public static string NormalizeInitials(string value)
        {
            string clean = Regex.Replace((value ?? "").ToUpperInvariant(), "[^A-Z0-9]", "");
            return clean.Length == 0 ? "AAA" : clean.Substring(0, Math.Min(3, clean.Length));
        }

        private static ScoreData Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Length > 1024 * 1024) throw new SerializationException("Score file too large.");
                ScoreData data = (ScoreData)new DataContractJsonSerializer(typeof(ScoreData)).ReadObject(stream);
                if (data == null || data.Entries == null) throw new SerializationException("Invalid score data.");
                data.LastInitials = NormalizeInitials(data.LastInitials);
                data.Entries = data.Entries.Where(e => e != null && e.Score >= 0 && e.Level >= 1 && e.Lines >= 0)
                    .OrderByDescending(e => e.Score).ThenBy(e => e.Date).Take(20).ToList();
                foreach (ScoreEntry entry in data.Entries) entry.Initials = NormalizeInitials(entry.Initials);
                return data;
            }
        }

        public ScoreEntry Add(int score, int level, int lines)
        {
            ScoreEntry entry = new ScoreEntry { Id = Guid.NewGuid().ToString("N"), Initials = Data.LastInitials,
                Score = score, Level = level, Lines = lines, Date = DateTime.Now };
            Data.Entries.Add(entry);
            Data.Entries = Data.Entries.OrderByDescending(e => e.Score).ThenBy(e => e.Date).Take(20).ToList();
            Save(); return entry;
        }

        public bool Rename(ScoreEntry entry, string initials)
        {
            entry.Initials = NormalizeInitials(initials); Data.LastInitials = entry.Initials; return Save();
        }

        public bool Save()
        {
            string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(ScoreData)).WriteObject(stream, Data);
                    stream.Flush(true);
                }
                if (File.Exists(FilePath))
                {
                    if (corruptPrimary)
                    {
                        File.Copy(FilePath, FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), true);
                        File.Replace(temporary, FilePath, null);
                    }
                    else File.Replace(temporary, FilePath, FilePath + ".bak");
                }
                else File.Move(temporary, FilePath);
                corruptPrimary = false; Notice = null; return true;
            }
            catch (Exception ex)
            {
                if (!IsStorageError(ex)) throw;
                Notice = "No se pudo guardar. Récords en memoria."; return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
