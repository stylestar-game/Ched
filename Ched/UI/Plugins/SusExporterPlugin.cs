using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;

using Ched.Components;
using Ched.Components.Exporter;

namespace Ched.UI.Plugins.Exporter
{
    public class SusExporterPlugin : IExportablePlugin
    {
        private SusExporter exporter = new SusExporter();

        public Version Version => new Version(1, 0, 0, 0);
        public string Filter => "Seaurchin Score File(*.sus)|*.sus";
        public string DisplayName => "susフォーマット";
        public IExporter Exporter => exporter;

        public Form GetForm(ScoreBook book)
        {
            return new SusExportForm(book, exporter);
        }

        public string GetCustomData()
        {
            return JsonConvert.SerializeObject(exporter.CustomArgs);
        }

        public void SetCustomData(string data, Version dataVersion)
        {
            exporter.CustomArgs = string.IsNullOrEmpty(data) ? new SusArgs() : JsonConvert.DeserializeObject<SusArgs>(data);
        }
    }
}
