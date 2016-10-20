using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using TKInterfaces;
using Microsoft.ServiceFabric.Data;
using System.Fabric.Description;
using System.IO;

namespace TKStateful {
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class TKStateful : StatefulService, ITKStateful {
        public TKStateful(StatefulServiceContext context)
            : base(context) { }

        public async Task<int> GetAsync(string name) {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("myDictionary");
            int val = 0;
            using (var tx = this.StateManager.CreateTransaction()) {
                var result = await myDictionary.TryGetValueAsync(tx, name);
                if (result.HasValue)
                    val = result.Value;
            }
            return val;
        }

        public async Task SetAsync(string name,int value) {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("myDictionary");

            using (var tx = this.StateManager.CreateTransaction()) {
                var result = await myDictionary.TryGetValueAsync(tx, name);
                if (result.HasValue)
                    await myDictionary.TryUpdateAsync(tx, name, value, result.Value);
                else
                    await myDictionary.TryAddAsync(tx, name, value);
                await tx.CommitAsync();
            }
        }

        public Task<string> GetInfoAsync() {
            return Task.FromResult($"Context.ReplicaId: {this.Context.ReplicaId}, Context.NodeContext.NodeName: {this.Context.NodeContext.NodeName}, Context.PartitionId:{this.Context.PartitionId}, Context.ServiceName: {this.Context.ServiceName}");
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners() {
            return new[] { new ServiceReplicaListener(context =>
            this.CreateServiceRemotingListener(context)) };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken) {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("myDictionary");

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction()) {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        #region Backup
        //private IReliableStateManager stateManager;
        private IBackupStore backupManager;

        //Set local or cloud backup, or none. Disabled is the default. Overridden by config.
        private BackupManagerType backupStorageType;

        private const string BackupCountDictionaryName = "BackupCountingDictionary";

        public async Task DoBackup() {
            long backupsTaken = 0;
            this.SetupBackupManager();
            if (this.backupStorageType == BackupManagerType.None) {
                return;
            } else {
                BackupDescription backupDescription = new BackupDescription(BackupOption.Full, this.BackupCallbackAsync);

                await this.BackupAsync(backupDescription);

                backupsTaken++;

                ServiceEventSource.Current.ServiceMessage(this, "Backup {0} taken", backupsTaken);
            }
        }
        private async Task<bool> BackupCallbackAsync(BackupInfo backupInfo, CancellationToken cancellationToken) {
            ServiceEventSource.Current.ServiceMessage(this, "Inside backup callback for replica {0}|{1}", this.Context.PartitionId, this.Context.ReplicaId);
            long totalBackupCount;

            IReliableDictionary<string, long> backupCountDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(BackupCountDictionaryName);
            using (ITransaction tx = this.StateManager.CreateTransaction()) {
                ConditionalValue<long> value = await backupCountDictionary.TryGetValueAsync(tx, "backupCount");

                if (!value.HasValue) {
                    totalBackupCount = 0;
                } else {
                    totalBackupCount = value.Value;
                }

                await backupCountDictionary.SetAsync(tx, "backupCount", ++totalBackupCount);

                await tx.CommitAsync();
            }

            ServiceEventSource.Current.Message("Backup count dictionary updated, total backup count is {0}", totalBackupCount);

            try {
                ServiceEventSource.Current.ServiceMessage(this, "Archiving backup");
                await this.backupManager.ArchiveBackupAsync(backupInfo, cancellationToken);
                ServiceEventSource.Current.ServiceMessage(this, "Backup archived");
            } catch (Exception e) {
                ServiceEventSource.Current.ServiceMessage(this, "Archive of backup failed: Source: {0} Exception: {1}", backupInfo.Directory, e.Message);
            }

            await this.backupManager.DeleteBackupsAsync(cancellationToken);

            ServiceEventSource.Current.Message("Backups deleted");

            return true;
        }

        private void SetupBackupManager() {
            string partitionId = this.Context.PartitionId.ToString("N");
            long minKey = ((Int64RangePartitionInformation)this.Partition.PartitionInfo).LowKey;
            long maxKey = ((Int64RangePartitionInformation)this.Partition.PartitionInfo).HighKey;

            if (this.Context.CodePackageActivationContext != null) {
                ICodePackageActivationContext codePackageContext = this.Context.CodePackageActivationContext;
                ConfigurationPackage configPackage = codePackageContext.GetConfigurationPackageObject("Config");
                ConfigurationSection configSection = configPackage.Settings.Sections["Settings"];

                string backupSettingValue = configSection.Parameters["BackupMode"].Value;

                if (string.Equals(backupSettingValue, "none", StringComparison.InvariantCultureIgnoreCase)) {
                    this.backupStorageType = BackupManagerType.None;
                } else if (string.Equals(backupSettingValue, "azure", StringComparison.InvariantCultureIgnoreCase)) {
                    this.backupStorageType = BackupManagerType.Azure;

                    ConfigurationSection azureBackupConfigSection = configPackage.Settings.Sections["Azure"];

                    this.backupManager = new AzureBlobBackupManager(azureBackupConfigSection, partitionId, minKey, maxKey, codePackageContext.TempDirectory);
                } else if (string.Equals(backupSettingValue, "local", StringComparison.InvariantCultureIgnoreCase)) {
                    this.backupStorageType = BackupManagerType.Local;

                    ConfigurationSection localBackupConfigSection = configPackage.Settings.Sections["Local"];

                    this.backupManager = new DiskBackupManager(localBackupConfigSection, partitionId, minKey, maxKey, codePackageContext.TempDirectory);
                } else {
                    throw new ArgumentException("Unknown backup type");
                }

                ServiceEventSource.Current.ServiceMessage(this, "Backup Manager Set Up");
            }
        }

        private enum BackupManagerType {
            Azure,
            Local,
            None
        };

        protected async override Task<bool> OnDataLossAsync(RestoreContext restoreCtx, CancellationToken cancellationToken) {
            ServiceEventSource.Current.ServiceMessage(this, "OnDataLoss Invoked!");
            this.SetupBackupManager();

            try {
                string backupFolder;

                if (this.backupStorageType == BackupManagerType.None) {
                    //since we have no backup configured, we return false to indicate
                    //that state has not changed. This replica will become the basis
                    //for future replica builds
                    return false;
                } else {
                    backupFolder = await this.backupManager.RestoreLatestBackupToTempLocation(cancellationToken);
                }

                ServiceEventSource.Current.ServiceMessage(this, "Restoration Folder Path " + backupFolder);

                RestoreDescription restoreRescription = new RestoreDescription(backupFolder, RestorePolicy.Force);

                await restoreCtx.RestoreAsync(restoreRescription, cancellationToken);

                ServiceEventSource.Current.ServiceMessage(this, "Restore completed");

                DirectoryInfo tempRestoreDirectory = new DirectoryInfo(backupFolder);
                tempRestoreDirectory.Delete(true);

                return true;
            } catch (Exception e) {
                ServiceEventSource.Current.ServiceMessage(this, "Restoration failed: " + "{0} {1}" + e.GetType() + e.Message);

                throw;
            }
        }
        #endregion
    }
}
