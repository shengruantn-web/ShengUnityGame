using System;
using System.IO;
using System.Text;

namespace VillageClash.Core
{
    /// <summary>Single-writer save store. Replacements stay on the same filesystem.</summary>
    public sealed class SaveFileStore
    {
        public string Path { get; }
        public string BackupPath => Path + ".bak";
        public string LoadNotice { get; private set; }

        public SaveFileStore(string path) { Path = System.IO.Path.GetFullPath(path); }

        public string Load()
        {
            LoadNotice = null;
            // IO failures must propagate: an unreadable file is not an empty village.
            var primary = Read(Path);
            if (Valid(primary)) return primary;
            var backup = Read(BackupPath);
            if (primary != null) Preserve(Path);
            if (Valid(backup))
            {
                LoadNotice = "Your village was recovered from its backup.";
                return backup;
            }
            if (backup != null) Preserve(BackupPath);
            if (primary != null || backup != null)
                LoadNotice = "The saves could not be loaded. Original files were preserved; a new village was started.";
            return null;
        }

        public void Save(string json)
        {
            if (!Valid(json)) throw new InvalidDataException("Refusing to write invalid village data.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temp = Path + ".tmp";
            // Flush the complete payload before publishing it. A stale temp from a crash
            // is never considered committed and is overwritten by the next attempt.
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            string previous = Read(Path);
            if (previous == null) File.Move(temp, Path);
            else
            {
                bool valid = Valid(previous);
                if (!valid) Preserve(Path);
                // Never replace a healthy backup with a damaged primary.
                File.Replace(temp, Path, valid ? BackupPath : null);
            }
        }

        static bool Valid(string json) => json != null && SaveSystem.TryDeserialize(json, out _);

        static string Read(string path)
        {
            try { return File.ReadAllText(path); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        static void Preserve(string path) => File.Copy(path, path + ".unreadable-" + Guid.NewGuid().ToString("N"));
    }
}
