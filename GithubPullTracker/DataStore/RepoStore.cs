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
        static RepoStore()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(SettingsManager.Settings.GetSetting("Storage.ConnectionString"));
            fileViewsTableName = SettingsManager.Settings.GetSetting("Storage.TableNames.FileViews");
            approvalsTableName = SettingsManager.Settings.GetSetting("Storage.TableNames.Approvals");
            client = storageAccount.CreateCloudTableClient();
        }

        Dictionary<string, List<TableOperation>> opperations = new Dictionary<string, List<TableOperation>>() {
            {fileViewsTableName, new List<TableOperation>() },
            {approvalsTableName, new List<TableOperation>() }
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
        
        public void AddFileView(string owner, string repo, int number, string sha, string username)
        {
            var view = new Models.FileView(owner, repo, number, username, sha);

            TableOperation insertOperation = TableOperation.InsertOrReplace(view);
            //await table.ExecuteAsync();
            opperations[fileViewsTableName].Add(insertOperation);
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
            CloudTable table = client.GetTableReference(approvalsTableName);
            var view = new Models.CommitApproval(owner, repo, number, headsha, username, approved);

            TableOperation insertOperation = TableOperation.InsertOrReplace(view);

            opperations[approvalsTableName].Add(insertOperation);
        }

        public async Task<IEnumerable<CommitApproval>> GetApprovals(string owner, string repo, int number)
        {
            await Flush(approvalsTableName);

            CloudTable table = client.GetTableReference(approvalsTableName);

            var targetKey = CommitApproval.GeneratePartitionKey(owner, repo, number);

            TableQuery<CommitApproval> query = new TableQuery<CommitApproval>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, targetKey));

            var pageViews = await table.ExecuteQueryAsync(query);

            return pageViews.Where(x=>x.Approved).ToList();
        }
    }
}