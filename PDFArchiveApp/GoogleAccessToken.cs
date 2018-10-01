using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace PDFArchiveApp
{
    [DataContract]
    public class GoogleAccessToken
    {
        [DataMember]
        public string access_token { get; set; }

        [DataMember]
        public string token_type { get; set; }

        [DataMember]
        public int expires_in { get; set; }

        [DataMember]
        public string refresh_token { get; set; }

        [DataMember]
        public string id_token { get; set; }

        [JsonIgnore]
        [IgnoreDataMember]
        public string RawJsonString {
            get
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        [DataMember]
        public DateTimeOffset? IssueDateTime { get; set; }

        [DataMember]
        public DateTimeOffset? IssueDateTimeUTC { get; set; }

        public GoogleAccessToken() { }

        public GoogleAccessToken(String json)
        {
            if (String.IsNullOrEmpty(json))
            {
                return;
            }

            //DataContractJsonSerializer tJsonSerial = new DataContractJsonSerializer(typeof(GoogleAccessToken));
            //MemoryStream tMS = new MemoryStream(Encoding.UTF8.GetBytes(json));
            //var self = tJsonSerial.ReadObject(tMS) as GoogleAccessToken;
            var self = JsonConvert.DeserializeObject<GoogleAccessToken>(json);
            access_token = self.access_token;
            token_type = self.token_type;
            expires_in = self.expires_in;
            refresh_token = self.refresh_token;
            id_token = self.id_token;

            IssueDateTime = self.IssueDateTime;
            IssueDateTimeUTC = self.IssueDateTimeUTC;
            
        }

    }
}
