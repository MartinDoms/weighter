using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Weighter.Google
{
    public class GoogleSheets
    {
        private readonly IConfig config;
        private readonly GoogleAuth auth;

        public GoogleSheets(IConfig config, GoogleAuth auth)
        {
            this.config = config;
            this.auth = auth;
        }
        public async Task<IList<string>> GetValuesFromColumn(string spreadsheetId, string sheetName, string column)
        {
            var service = await GetSheetsService();

            var range = string.Format("'{0}'!{1}1:{1}", sheetName, column);

            SpreadsheetsResource.ValuesResource.GetRequest request =
                service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            
            return values.Select(val => 
                {
                    if (val.Count > 0)
                    {
                        return val[0].ToString();   
                    }
                    return "";
                }
            ).ToList();
        }

        public async Task PostValueToCell(double value, string spreadsheetId, string sheetName, string column, int row)
        {
            var service = await GetSheetsService();

            var range = string.Format("'{0}'!{1}{2}:{1}{2}", sheetName, column, row);

            var valueRange = new ValueRange();
            valueRange.MajorDimension = "COLUMNS";
            var valueList = new List<object>() { value };
            valueRange.Values = new List<IList<object>> { valueList };

            SpreadsheetsResource.ValuesResource.UpdateRequest request =
                service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            request.Execute();
        }

        private async Task<SheetsService> GetSheetsService()
        {
            var ApplicationName = config.AppName;
            var credential = await auth.GetCredential();

            SheetsService service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }
    }
}