using System;
using System.IO;
using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class SaveFileStoreTests
    {
        string directory;
        SaveFileStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "vc-save-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new SaveFileStore(Path.Combine(directory, "village.json"));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, true);

        static string Village(int gold)
        {
            var state = SaveSystem.NewGame(42, 1000);
            state.Gold = gold;
            return SaveSystem.Serialize(state);
        }

        [Test]
        public void AtomicReplacementKeepsPreviousCommittedSave()
        {
            Assert.IsNull(store.Load());
            store.Save(Village(100));
            store.Save(Village(200));
            store.Save(Village(300));
            Assert.That(store.Load(), Is.EqualTo(Village(300)));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(Village(200)));
            Assert.IsFalse(File.Exists(store.Path + ".tmp"));
        }

        [Test]
        public void TruncatedPrimaryRecoversBackupAndPreservesOriginal()
        {
            store.Save(Village(100));
            store.Save(Village(200));
            File.WriteAllText(store.Path, "{\"version\":1,");
            Assert.That(store.Load(), Is.EqualTo(Village(100)));
            Assert.That(store.LoadNotice, Does.Contain("recovered"));
            Assert.That(Directory.GetFiles(directory, "*.unreadable-*"), Has.Length.EqualTo(1));
            store.Save(Village(100));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(Village(100)));
            Assert.That(store.Load(), Is.EqualTo(Village(100)));
        }

        [Test]
        public void MissingPrimaryRecoversBackupAndIgnoresUncommittedTemp()
        {
            File.WriteAllText(store.BackupPath, Village(100));
            File.WriteAllText(store.Path + ".tmp", Village(999));
            Assert.That(store.Load(), Is.EqualTo(Village(100)));
            store.Save(Village(200));
            Assert.That(store.Load(), Is.EqualTo(Village(200)));
        }

        [Test]
        public void InvalidCopiesArePreservedBeforeStartingOver()
        {
            File.WriteAllText(store.Path, "broken primary");
            File.WriteAllText(store.BackupPath, "broken backup");
            Assert.IsNull(store.Load());
            Assert.That(store.LoadNotice, Does.Contain("preserved"));
            var preserved = Directory.GetFiles(directory, "*.unreadable-*");
            Assert.That(preserved, Has.Length.EqualTo(2));
            Assert.That(Array.ConvertAll(preserved, File.ReadAllText), Is.EquivalentTo(new[] { "broken primary", "broken backup" }));
        }

        [Test]
        public void FailedTemporaryWriteLeavesBothCommittedCopiesUntouched()
        {
            store.Save(Village(100));
            store.Save(Village(200));
            Directory.CreateDirectory(store.Path + ".tmp");
            Assert.Catch<Exception>(() => store.Save(Village(300)));
            Assert.That(store.Load(), Is.EqualTo(Village(200)));
            Assert.That(File.ReadAllText(store.BackupPath), Is.EqualTo(Village(100)));
        }

        [Test]
        public void InvalidPayloadCannotReplaceHealthySave()
        {
            store.Save(Village(100));
            Assert.Throws<InvalidDataException>(() => store.Save("{}"));
            Assert.That(store.Load(), Is.EqualTo(Village(100)));
        }

        [Test]
        public void UnreadablePrimaryIsAnErrorNotANewVillage()
        {
            Directory.CreateDirectory(store.Path);
            Assert.Catch<Exception>(() => store.Load());
        }
    }
}
