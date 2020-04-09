using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.Services;

namespace Weighter.Google
{
    public class GoogleFitness
    {
        private readonly IConfig config;
        private readonly GoogleAuth auth;

        public GoogleFitness(IConfig config, GoogleAuth auth)
        {
            this.config = config;
            this.auth = auth;
        }
        public async Task<IEnumerable<WeightValue>> GetWeightsBetween(DateTime from, DateTime to) 
        {
            var credential = await auth.GetCredential();

            var service = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = config.AppName
            });

            var timeSpan = string.Format("{0}-{1}", DateTimeToNanos(from), DateTimeToNanos(to));
            var weight = await service.Users.DataSources.Datasets.Get("me", "derived:com.google.weight:com.google.android.gms:merge_weight", timeSpan).ExecuteAsync();
            
            return weight.Point.Select(PointToWeightValue);
        }

        private static WeightValue PointToWeightValue(DataPoint p)
        {
            var weight = p.Value[0].FpVal;
            if (weight == null)
            {
                throw new ArgumentException("Weight data point cannot have a null FpValue");
            }
            if (p.StartTimeNanos == null)
            {
                throw new ArgumentNullException("Weight data point cannot have a null start time");
            }
            DateTime dateTime = NanosToDateTime(p.StartTimeNanos.Value);

            return new WeightValue(dateTime, weight.Value);

        }

        private static DateTime NanosToDateTime(long nanos)
        {
            return FromUnixTime(nanos/1000000);
        }

        private static long DateTimeToNanos(DateTime dateTime)
        {
            return ((long)dateTime.Subtract(epoch).TotalMilliseconds) * 1000000L;
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            return epoch.AddMilliseconds(unixTime);
        }
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public struct WeightValue
    {
        public double Weight { get; set; }
        public DateTime DateTime { get; set; }

        public WeightValue(DateTime dateTime, double weight)
        {
            this.Weight = weight;
            this.DateTime = dateTime;
        }
    }
}