using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Smartstore.IO;

namespace Smartstore.Tests
{
    [TestFixture]
    public class FileSystemStorageProviderTests
    {
        private string _filePath;
        private string _folderPath;
        private IFileSystem _fileSystem;

        [OneTimeSetUp]
        public void Init()
        {
            _folderPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Media"), "Default");
            _filePath = _folderPath + "\\testfile.txt";

            Directory.CreateDirectory(_folderPath);
            File.WriteAllText(_filePath, "testfile contents");

            var subfolder1 = Path.Combine(_folderPath, "Subfolder1");
            Directory.CreateDirectory(subfolder1);
            File.WriteAllText(Path.Combine(subfolder1, "one.txt"), "one contents");
            File.WriteAllText(Path.Combine(subfolder1, "two.txt"), "two contents");

            var subsubfolder1 = Path.Combine(subfolder1, "SubSubfolder1");
            Directory.CreateDirectory(subsubfolder1);

            _fileSystem = new LocalFileSystem(_folderPath);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            Directory.Delete(_folderPath, true);
        }

        [Test]
        public void GetFileThatDoesNotExistShouldThrow()
        {
            var file = _fileSystem.GetFile("notexisting");
            Assert.That(file.Exists, Is.EqualTo(false));
        }

        [Test]
        public void ListFilesShouldReturnFilesFromFilesystem()
        {
            IEnumerable<IFile> files = _fileSystem.EnumerateFiles("");
            Assert.That(files.Count(), Is.EqualTo(1));
        }

        [Test]
        public void ExistingFileIsReturnedWithShortPath()
        {
            var file = _fileSystem.GetFile("testfile.txt");
            Assert.That(file, Is.Not.Null);
            Assert.That(file.SubPath, Is.EqualTo("testfile.txt"));
            Assert.That(file.Name, Is.EqualTo("testfile.txt"));
        }


        [Test]
        public void ListFilesReturnsItemsWithShortPathAndEnvironmentSlashes()
        {
            var files = _fileSystem.EnumerateFiles("Subfolder1");
            Assert.That(files, Is.Not.Null);
            Assert.That(files.Count(), Is.EqualTo(2));
            var one = files.Single(x => x.Name == "one.txt");
            var two = files.Single(x => x.Name == "two.txt");

            Assert.That(one.SubPath, Is.EqualTo($"Subfolder1{PathUtility.PathSeparators[0]}one.txt"));
            Assert.That(two.SubPath, Is.EqualTo($"Subfolder1{PathUtility.PathSeparators[0]}two.txt"));
        }


        [Test]
        public void AnySlashInGetFileBecomesEnvironmentAppropriate()
        {
            var file1 = _fileSystem.GetFile(@"Subfolder1/one.txt");
            var file2 = _fileSystem.GetFile(@"Subfolder1\one.txt");
            Assert.That(file1.SubPath, Is.EqualTo("Subfolder1/one.txt"));
            Assert.That(file2.SubPath, Is.EqualTo("Subfolder1/one.txt"));
        }

        [Test]
        public void ListFoldersReturnsItemsWithShortPathAndEnvironmentSlashes()
        {
            var folders = _fileSystem.EnumerateDirectories(@"Subfolder1").ToArray();
            Assert.That(folders, Is.Not.Null);
            Assert.That(folders.Length, Is.EqualTo(1));
            Assert.That(folders.Single().Name, Is.EqualTo("SubSubfolder1"));
            Assert.That(folders.Single().SubPath, Is.EqualTo("Subfolder1/SubSubfolder1"));
        }

        [Test]
        public void ParentFolderPathIsStillShort()
        {
            var subsubfolder = _fileSystem.EnumerateDirectories(@"Subfolder1").Single();
            var subfolder = subsubfolder.Parent;
            Assert.That(subsubfolder.Name, Is.EqualTo("SubSubfolder1"));
            Assert.That(subsubfolder.SubPath, Is.EqualTo("Subfolder1/SubSubfolder1"));
            Assert.That(subfolder.Name, Is.EqualTo("Subfolder1"));
            Assert.That(subfolder.SubPath, Is.EqualTo("Subfolder1"));
        }

        [Test]
        public void CreateFolderAndDeleteFolderTakesAnySlash()
        {
            Assert.That(_fileSystem.EnumerateDirectories(@"Subfolder1").Count(), Is.EqualTo(1));

            _fileSystem.TryCreateDirectory(@"SubFolder1/SubSubFolder2");
            _fileSystem.TryCreateDirectory(@"SubFolder1\SubSubFolder3");
            Assert.That(_fileSystem.EnumerateDirectories(@"Subfolder1").Count(), Is.EqualTo(3));

            _fileSystem.TryDeleteDirectory(@"SubFolder1/SubSubFolder2");
            _fileSystem.TryDeleteDirectory(@"SubFolder1\SubSubFolder3");
            Assert.That(_fileSystem.EnumerateDirectories(@"Subfolder1").Count(), Is.EqualTo(1));
        }

        private IDirectory GetDirectory(string path)
        {
            return _fileSystem.EnumerateDirectories(Path.GetDirectoryName(path))
                .SingleOrDefault(x => string.Equals(x.Name, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase));
        }
        private IFile GetFile(string path)
        {
            try
            {
                return _fileSystem.GetFile(path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        [Test]
        public void RenameFolderTakesShortPathWithAnyKindOfSlash()
        {
            Assert.That(GetDirectory(@"SubFolder1/SubSubFolder1"), Is.Not.Null);
            _fileSystem.MoveEntry(@"SubFolder1\SubSubFolder1", @"SubFolder1/SubSubFolder2");
            _fileSystem.MoveEntry(@"SubFolder1\SubSubFolder2", @"SubFolder1\SubSubFolder3");
            _fileSystem.MoveEntry(@"SubFolder1/SubSubFolder3", @"SubFolder1\SubSubFolder4");
            _fileSystem.MoveEntry(@"SubFolder1/SubSubFolder4", @"SubFolder1/SubSubFolder5");
            Assert.That(GetDirectory(Path.Combine("SubFolder1", "SubSubFolder1")), Is.Null);
            Assert.That(GetDirectory(Path.Combine("SubFolder1", "SubSubFolder2")), Is.Null);
            Assert.That(GetDirectory(Path.Combine("SubFolder1", "SubSubFolder3")), Is.Null);
            Assert.That(GetDirectory(Path.Combine("SubFolder1", "SubSubFolder4")), Is.Null);
            Assert.That(GetDirectory(Path.Combine("SubFolder1", "SubSubFolder5")), Is.Not.Null);
        }


        [Test]
        public void CreateFileAndDeleteFileTakesAnySlash()
        {
            Assert.That(_fileSystem.EnumerateFiles(@"Subfolder1").Count(), Is.EqualTo(2));
            var alpha = _fileSystem.CreateFile(@"SubFolder1/alpha.txt");
            var beta = _fileSystem.CreateFile(@"SubFolder1\beta.txt");
            Assert.That(_fileSystem.EnumerateFiles(@"Subfolder1").Count(), Is.EqualTo(4));
            Assert.That(alpha.SubPath, Is.EqualTo("SubFolder1/alpha.txt"));
            Assert.That(beta.SubPath, Is.EqualTo("SubFolder1/beta.txt"));
            _fileSystem.TryDeleteFile(@"SubFolder1\alpha.txt");
            _fileSystem.TryDeleteFile(@"SubFolder1/beta.txt");
            Assert.That(_fileSystem.EnumerateFiles(@"Subfolder1").Count(), Is.EqualTo(2));
        }

        [Test]
        public void RenameFileTakesShortPathWithAnyKindOfSlash()
        {
            Assert.That(GetFile(@"Subfolder1/one.txt"), Is.Not.Null);

            _fileSystem.MoveEntry(@"SubFolder1\one.txt", @"SubFolder1/testfile2.txt");
            _fileSystem.MoveEntry(@"SubFolder1\testfile2.txt", @"SubFolder1\testfile3.txt");
            _fileSystem.MoveEntry(@"SubFolder1/testfile3.txt", @"SubFolder1\testfile4.txt");
            _fileSystem.MoveEntry(@"SubFolder1/testfile4.txt", @"SubFolder1/testfile5.txt");
            Assert.That(GetFile(Path.Combine("SubFolder1", "one.txt")).Exists, Is.EqualTo(false));
            Assert.That(GetFile(Path.Combine("SubFolder1", "testfile2.txt")).Exists, Is.EqualTo(false));
            Assert.That(GetFile(Path.Combine("SubFolder1", "testfile3.txt")).Exists, Is.EqualTo(false));
            Assert.That(GetFile(Path.Combine("SubFolder1", "testfile4.txt")).Exists, Is.EqualTo(false));
            Assert.That(GetFile(Path.Combine("SubFolder1", "testfile5.txt")).Exists, Is.EqualTo(true));
        }
    }
}



