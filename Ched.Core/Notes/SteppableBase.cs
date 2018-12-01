using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public enum Side
    {
        Left,
        Right
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class SteppableBase : TappableBase
    {
        [Newtonsoft.Json.JsonProperty]
        private Side side;

        /// <summary>
        /// Whether the note is a left note or a right note
        /// </summary>
        public Side Side
        {
            get { return side; }
            set { side = value; }
        }
    }
}
