using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace Weighter.Google
{
    public class GoogleAuth
    {
        private readonly IConfig config;

        static string[] SCOPES = new[] {
            SheetsService.Scope.Spreadsheets,
            FitnessService.Scope.FitnessBodyRead
        };

        public GoogleAuth(IConfig config)
        {
            this.config = config;
        }
        public async Task<ICredential> GetCredential()
        {
            var appDir = config.AppDir;

            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    SCOPES,
                    "user", CancellationToken.None, new FileDataStore(Path.Combine(config.AppDataDir, "Weighter.DataSources"), true));
            }
        }
    }
}