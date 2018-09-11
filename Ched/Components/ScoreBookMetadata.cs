using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Ched.Components
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class ScoreBookMetadata
    {
        [Newtonsoft.Json.JsonProperty]
        private Dictionary<string, ExporterArgsMetadata> exporterArgs = new Dictionary<string, ExporterArgsMetadata>();

        /// <summary>
        /// エクスポート用の設定を格納します。
        /// </summary>
        public Dictionary<string, ExporterArgsMetadata> ExporterArgs
        {
            get { return exporterArgs; }
            set { exporterArgs = value; }
        }
    }

    public class ExporterArgsMetadata
    {
        public Version Version { get; set; }
        public string Value { get; set; }
    }
}
