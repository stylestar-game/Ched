using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public enum Direction
    {
        Up,
        Down
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class MotionableBase : TappableBase
    {
        [Newtonsoft.Json.JsonProperty]
        private Direction direction;

        /// <summary>
        /// Whether the note is a left note or a right note
        /// </summary>
        public Direction Direction
        {
            get { return direction; }
            set { direction = value; }
        }
    }
}
