using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Components;
using Ched.Components.Exporter;

namespace Ched.UI.Plugins.Exporter
{
    /// <summary>
    /// 譜面エクスポートを行うプラグインを表します。
    /// </summary>
    public interface IExportablePlugin : IPlugin
    {
        /// <summary>
        /// 出力ファイル選択時に適用するフィルタ文字列を取得します。
        /// </summary>
        string Filter { get; }

        /// <summary>
        /// エクスポートに利用する<see cref="IExporter"/>を取得します。
        /// </summary>
        IExporter Exporter { get; }

        /// <summary>
        /// エクスポート時の設定を入力する<see cref="Form"/>を取得します。
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        Form GetForm(ScoreBook book);

        /// <summary>
        /// エクスポート時に利用する拡張データを保存する文字列を取得します。
        /// </summary>
        /// <returns>独自データを表す文字列</returns>
        string GetCustomData();

        /// <summary>
        /// エクスポート時に利用する拡張データを設定します。
        /// </summary>
        /// <param name="data">独自データを表す文字列</param>
        void SetCustomData(string data);
    }
}
