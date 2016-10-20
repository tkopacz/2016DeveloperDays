using Microsoft.ServiceFabric.Data;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Fabric.Description;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TKStateful {
    public interface IBackupStore {
        long backupFrequencyInSeconds { get; }

        Task ArchiveBackupAsync(BackupInfo backupInfo, CancellationToken cancellationToken);

        Task<string> RestoreLatestBackupToTempLocation(CancellationToken cancellationToken);

        Task DeleteBackupsAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Backup to Azure
    /// </summary>
    public class AzureBlobBackupManager : IBackupStore {
        private readonly CloudBlobClient cloudBlobClient;
        private CloudBlobContainer backupBlobContainer;
        private int MaxBackupsToKeep;

        private string PartitionTempDirectory;
        private string partitionId;

        private long backupFrequencyInSeconds;
        private long keyMin;
        private long keyMax;

        public AzureBlobBackupManager(ConfigurationSection configSection, string partitionId, long keymin, long keymax, string codePackageTempDirectory) {
            this.keyMin = keymin;
            this.keyMax = keymax;

            string backupAccountName = configSection.Parameters["BackupAccountName"].Value;
            string backupAccountKey = configSection.Parameters["PrimaryKeyForBackupTestAccount"].Value;
            string blobEndpointAddress = configSection.Parameters["BlobServiceEndpointAddress"].Value;

            this.backupFrequencyInSeconds = long.Parse(configSection.Parameters["BackupFrequencyInSeconds"].Value);
            this.MaxBackupsToKeep = int.Parse(configSection.Parameters["MaxBackupsToKeep"].Value);
            this.partitionId = partitionId;
            this.PartitionTempDirectory = Path.Combine(codePackageTempDirectory, partitionId);

            StorageCredentials storageCredentials = new StorageCredentials(backupAccountName, backupAccountKey);
            this.cloudBlobClient = new CloudBlobClient(new Uri(blobEndpointAddress), storageCredentials);
            this.backupBlobContainer = this.cloudBlobClient.GetContainerReference(this.partitionId);
            this.backupBlobContainer.CreateIfNotExists();
        }

        long IBackupStore.backupFrequencyInSeconds {
            get { return this.backupFrequencyInSeconds; }
        }

        public async Task ArchiveBackupAsync(BackupInfo backupInfo, CancellationToken cancellationToken) {
            ServiceEventSource.Current.Message("AzureBlobBackupManager: Archive Called.");

            string fullArchiveDirectory = Path.Combine(this.PartitionTempDirectory, Guid.NewGuid().ToString("N"));

            DirectoryInfo fullArchiveDirectoryInfo = new DirectoryInfo(fullArchiveDirectory);
            fullArchiveDirectoryInfo.Create();

            string blobName = string.Format("{0}_{1}_{2}_{3}", Guid.NewGuid().ToString("N"), this.keyMin, this.keyMax, "Backup.zip");
            string fullArchivePath = Path.Combine(fullArchiveDirectory, "Backup.zip");

            ZipFile.CreateFromDirectory(backupInfo.Directory, fullArchivePath, CompressionLevel.Fastest, false);

            DirectoryInfo backupDirectory = new DirectoryInfo(backupInfo.Directory);
            backupDirectory.Delete(true);

            CloudBlockBlob blob = this.backupBlobContainer.GetBlockBlobReference(blobName);
            await blob.UploadFromFileAsync(fullArchivePath, CancellationToken.None);

            DirectoryInfo tempDirectory = new DirectoryInfo(fullArchiveDirectory);
            tempDirectory.Delete(true);

            ServiceEventSource.Current.Message("AzureBlobBackupManager: UploadBackupFolderAsync: success.");
        }

        public async Task<string> RestoreLatestBackupToTempLocation(CancellationToken cancellationToken) {
            ServiceEventSource.Current.Message("AzureBlobBackupManager: Download backup async called.");

            CloudBlockBlob lastBackupBlob = (await this.GetBackupBlobs(true)).First();

            ServiceEventSource.Current.Message("AzureBlobBackupManager: Downloading {0}", lastBackupBlob.Name);

            string downloadId = Guid.NewGuid().ToString("N");

            string zipPath = Path.Combine(this.PartitionTempDirectory, string.Format("{0}_Backup.zip", downloadId));

            lastBackupBlob.DownloadToFile(zipPath, FileMode.CreateNew);

            string restorePath = Path.Combine(this.PartitionTempDirectory, downloadId);

            ZipFile.ExtractToDirectory(zipPath, restorePath);

            FileInfo zipInfo = new FileInfo(zipPath);
            zipInfo.Delete();

            ServiceEventSource.Current.Message("AzureBlobBackupManager: Downloaded {0} in to {1}", lastBackupBlob.Name, restorePath);

            return restorePath;
        }

        public async Task DeleteBackupsAsync(CancellationToken cancellationToken) {
            if (this.backupBlobContainer.Exists()) {
                ServiceEventSource.Current.Message("AzureBlobBackupManager: Deleting old backups");

                IEnumerable<CloudBlockBlob> oldBackups = (await this.GetBackupBlobs(true)).Skip(this.MaxBackupsToKeep);

                foreach (CloudBlockBlob backup in oldBackups) {
                    ServiceEventSource.Current.Message("AzureBlobBackupManager: Deleting {0}", backup.Name);
                    await backup.DeleteAsync(cancellationToken);
                }
            }
        }

        private async Task<IEnumerable<CloudBlockBlob>> GetBackupBlobs(bool sorted) {
            IEnumerable<IListBlobItem> blobs = this.backupBlobContainer.ListBlobs();

            ServiceEventSource.Current.Message("AzureBlobBackupManager: Got {0} blobs", blobs.Count());

            List<CloudBlockBlob> itemizedBlobs = new List<CloudBlockBlob>();

            foreach (CloudBlockBlob cbb in blobs) {
                await cbb.FetchAttributesAsync();
                itemizedBlobs.Add(cbb);
            }

            if (sorted) {
                return itemizedBlobs.OrderByDescending(x => x.Properties.LastModified);
            } else {
                return itemizedBlobs;
            }
        }
    }


    /// <summary>
    /// Backup on disk
    /// </summary>
    public class DiskBackupManager : IBackupStore {
        private string PartitionArchiveFolder;
        private string PartitionTempDirectory;
        private long backupFrequencyInSeconds;
        private int MaxBackupsToKeep;
        private long keyMin;
        private long keyMax;

        public DiskBackupManager(ConfigurationSection configSection, string partitionId, long keymin, long keymax, string codePackageTempDirectory) {
            this.keyMin = keymin;
            this.keyMax = keymax;

            string BackupArchivalPath = configSection.Parameters["BackupArchivalPath"].Value;
            this.backupFrequencyInSeconds = long.Parse(configSection.Parameters["BackupFrequencyInSeconds"].Value);
            this.MaxBackupsToKeep = int.Parse(configSection.Parameters["MaxBackupsToKeep"].Value);

            this.PartitionArchiveFolder = Path.Combine(BackupArchivalPath, "Backups", partitionId);
            this.PartitionTempDirectory = Path.Combine(codePackageTempDirectory, partitionId);

            ServiceEventSource.Current.Message(
                "DiskBackupManager constructed IntervalinSec:{0}, archivePath:{1}, tempPath:{2}, backupsToKeep:{3}",
                this.backupFrequencyInSeconds,
                this.PartitionArchiveFolder,
                this.PartitionTempDirectory,
                this.MaxBackupsToKeep);
        }

        long IBackupStore.backupFrequencyInSeconds {
            get { return this.backupFrequencyInSeconds; }
        }

        public Task ArchiveBackupAsync(BackupInfo backupInfo, CancellationToken cancellationToken) {
            string fullArchiveDirectory = Path.Combine(
                this.PartitionArchiveFolder,
                string.Format("{0}_{1}_{2}", Guid.NewGuid().ToString("N"), this.keyMin, this.keyMax));

            DirectoryInfo dirInfo = new DirectoryInfo(fullArchiveDirectory);
            dirInfo.Create();

            string fullArchivePath = Path.Combine(fullArchiveDirectory, "Backup.zip");

            ZipFile.CreateFromDirectory(backupInfo.Directory, fullArchivePath, CompressionLevel.Fastest, false);

            DirectoryInfo backupDirectory = new DirectoryInfo(backupInfo.Directory);
            backupDirectory.Delete(true);

            return Task.FromResult(true);
        }

        public Task<string> RestoreLatestBackupToTempLocation(CancellationToken cancellationToken) {
            ServiceEventSource.Current.Message("Restoring backup to temp source:{0} destination:{1}", this.PartitionArchiveFolder, this.PartitionTempDirectory);

            DirectoryInfo dirInfo = new DirectoryInfo(this.PartitionArchiveFolder);

            string backupZip = dirInfo.GetDirectories().OrderByDescending(x => x.LastWriteTime).First().FullName;

            string zipPath = Path.Combine(backupZip, "Backup.zip");

            ServiceEventSource.Current.Message("latest zip backup is {0}", zipPath);

            DirectoryInfo directoryInfo = new DirectoryInfo(this.PartitionTempDirectory);
            if (directoryInfo.Exists) {
                directoryInfo.Delete(true);
            }

            directoryInfo.Create();

            ZipFile.ExtractToDirectory(zipPath, this.PartitionTempDirectory);

            ServiceEventSource.Current.Message("Zip backup {0} extracted to {1}", zipPath, this.PartitionTempDirectory);

            return Task.FromResult<string>(this.PartitionTempDirectory);
        }

        public async Task DeleteBackupsAsync(CancellationToken cancellationToken) {
            await Task.Run(
                () => {
                    ServiceEventSource.Current.Message("deleting old backups");

                    if (!Directory.Exists(this.PartitionArchiveFolder)) {
                        //Nothing to delete; Backups may not even have been created for the partition
                        return;
                    }

                    DirectoryInfo dirInfo = new DirectoryInfo(this.PartitionArchiveFolder);

                    IEnumerable<DirectoryInfo> oldBackups = dirInfo.GetDirectories().OrderByDescending(x => x.LastWriteTime).Skip(this.MaxBackupsToKeep);

                    foreach (DirectoryInfo oldBackup in oldBackups) {
                        ServiceEventSource.Current.Message("Deleting old backup {0}", oldBackup.FullName);
                        oldBackup.Delete(true);
                    }

                    ServiceEventSource.Current.Message("Old backups deleted");

                    return;
                },
                cancellationToken);
        }
    }

}
