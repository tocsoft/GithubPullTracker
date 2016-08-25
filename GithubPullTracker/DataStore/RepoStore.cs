using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using GithubPullTracker.DataStore.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GithubPullTracker.DataStore
{
    public class RepoStore
    {
        private static readonly CloudTableClient client;
        private static readonly string fileViewsTableName;
        private static readonly string approvalsTableName;
        private static readonly string settingsTableName;
        static RepoStore()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(SettingsManager.Settings.GetSetting("Storage.ConnectionString"));
            fileViewsTableName = SettingsManager.Settings.GetSetting("Storage.TableNames.FileViews");
            approvalsTableName = SettingsManager.Settings.GetSetting("Storage.TableNames.Approvals");
            settingsTableName = SettingsManager.Settings.GetSetting("Storage.TableNames.Settings");
            client = storageAccount.CreateCloudTableClient();
        }

        Dictionary<string, List<TableOperation>> opperations = new Dictionary<string, List<TableOperation>>() {
            {fileViewsTableName, new List<TableOperation>() },
            {approvalsTableName, new List<TableOperation>() },
            {settingsTableName, new List<TableOperation>() }
        };

        internal async Task Flush(string tableName)
        {
            List<Task> writeTasks = new List<Task>();
            IEnumerable<string> tablesToFlush = null;
            if(tableName == null)
            {
                tablesToFlush = opperations.Keys;
            }else
            {
                tablesToFlush = new[] { tableName };
            }

            foreach(var t in tablesToFlush)
            {
                if (opperations[t].Any())
                {
                    var batch = new TableBatchOperation();
                    foreach (var opp in opperations[t])
                    {
                        batch.Add(opp);
                    }
                    opperations[t].Clear();
                    CloudTable table = client.GetTableReference(t);
                    writeTasks.Add(table.ExecuteBatchAsync(batch));
                }
            }
            if (writeTasks.Any())
            {
                await Task.WhenAll(writeTasks);
            }
        }
        public Task Flush()
        {
            return Flush(null);
        }

        public async Task<RepoSettings> GetRepoSettings(string owner, string repo)
        {
            await Flush(settingsTableName);

            CloudTable table = client.GetTableReference(settingsTableName);

            var targetKey = RepoSettings.GeneratePartitionKey(owner);

            var query = new TableQuery<RepoSettings>().Where(
                  TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, repo.ToLower())
                        ));

            var reposettings = await table.ExecuteQueryAsync(query);

            return reposettings.FirstOrDefault() ?? new Models.RepoSettings(owner, repo);
        }

        public async Task<OwnerSettings> GetOwnerSettings(string owner)
        {
            await Flush(settingsTableName);

            CloudTable table = client.GetTableReference(settingsTableName);

            var targetKey = OwnerSettings.GeneratePartitionKey(owner);

            var query = new TableQuery<OwnerSettings>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "@")
                    ));


            var results = await table.ExecuteQueryAsync(query);
            
            return results.FirstOrDefault() ?? new OwnerSettings(owner);
        }

        public async Task<OwnerConfig> GetOwnerConfig(string owner)
        {
            await Flush(settingsTableName);

            CloudTable table = client.GetTableReference(settingsTableName);

            var targetKey = OwnerSettings.GeneratePartitionKey(owner);

            var query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey));
            
            
            var results = await table.ExecuteQueryAsync(query);

            var ownerData = results.Where(x => x.RowKey == "@").FirstOrDefault();
            var repos = results.Where(x => x.RowKey != "@");


            return new OwnerConfig(owner, Convert<OwnerSettings>(ownerData), Convert<RepoSettings>(repos));
        }

        private IEnumerable<T> Convert<T>(IEnumerable<DynamicTableEntity> data) where T : TableEntity, new()
        {
            return data.Select(x => Convert<T>(x)).ToList();
        }
        private T Convert<T>(DynamicTableEntity data) where T : TableEntity, new()
        {
            if (data == null)
            {
                return null;
            }
            var entity = new T();
            entity.PartitionKey = data.PartitionKey;
            entity.RowKey = data.RowKey;
            entity.Timestamp = data.Timestamp;
            entity.ReadEntity(data.Properties, null);
            entity.ETag = data.ETag;
            return entity;
        }

        public void AddFileView(string owner, string repo, int number, string sha, string username)
        {
            var view = new Models.FileView(owner, repo, number, username, sha);

            TableOperation insertOperation = TableOperation.InsertOrReplace(view);
            //await table.ExecuteAsync();
            opperations[fileViewsTableName].Add(insertOperation);
        }
        
        internal void SetRepoSettings(RepoSettings repoSettings)
        {
            TableOperation insertOperation = TableOperation.InsertOrReplace(repoSettings);
            //await table.ExecuteAsync();
            opperations[settingsTableName].Add(insertOperation);
        }

        internal void SetOwnerSettings(OwnerSettings owner)
        {
            TableOperation insertOperation = TableOperation.InsertOrReplace(owner);
            //await table.ExecuteAsync();
            opperations[settingsTableName].Add(insertOperation);
        }


        public async Task<IEnumerable<string>> ListFileViews(string owner, string repo, int number, string username)
        {
            //flush first
            await Flush(fileViewsTableName);
            var targetKey = FileView.GeneratePartitionKey(owner, repo, number, username);

            TableQuery<FileView> query = new TableQuery<FileView>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey));

            CloudTable table = client.GetTableReference(fileViewsTableName);

            var pageViews = await table.ExecuteQueryAsync(query);

            return pageViews.Select(x => x.FileSha).ToList();
        }

        public void UpdateApproval(string owner, string repo, int number, string headsha, string username, bool approved)
        {
            var view = new Models.CommitApproval(owner, repo, number, headsha, username, approved);

            TableOperation insertOperation = TableOperation.InsertOrReplace(view);

            opperations[approvalsTableName].Add(insertOperation);
        }

        public async Task<IEnumerable<CommitApproval>> GetApprovals(string owner, string repo, int number)
        {
            await Flush(approvalsTableName);

            CloudTable table = client.GetTableReference(approvalsTableName);

            var targetKey = CommitApproval.GeneratePartitionKey(owner, repo, number);

            TableQuery<CommitApproval> query = new TableQuery<CommitApproval>().Where(
                 
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey)
                                             );
            try
            {
                var pageViews = await table.ExecuteQueryAsync(query);
                return pageViews.Where(x=>x.RowKey != "@").ToList();
            }
            catch(Exception ex)
            {
                throw;
            }

        }

        public void UpdatePullRequestSettings(PullRequestSettings settings)
        {
            TableOperation insertOperation = TableOperation.InsertOrReplace(settings);

            opperations[approvalsTableName].Add(insertOperation);
        }
        public async Task<PullRequestSettings> GetPullRequestSettings(string owner, string repo, int number)
        {
            await Flush(approvalsTableName);

            CloudTable table = client.GetTableReference(approvalsTableName);

            var targetKey = PullRequestSettings.GeneratePartitionKey(owner, repo, number);

            var query = new TableQuery<PullRequestSettings>().Where(
                 TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "@")
                        ));

            var pageViews = await table.ExecuteQueryAsync(query);

            return pageViews.FirstOrDefault() ?? new PullRequestSettings(owner, repo, number, false);
        }
    }
}