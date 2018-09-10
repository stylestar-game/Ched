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
        private Dictionary<string, string> exporterArgs = new Dictionary<string, string>();

        /// <summary>
        /// エクスポート用の設定を格納します。
        /// </summary>
        public Dictionary<string, string> ExporterArgs
        {
            get { return exporterArgs; }
            set { exporterArgs = value; }
        }
    }
}
