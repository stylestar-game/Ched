using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class SlideStep : Slide
    {
        [Newtonsoft.Json.JsonProperty]
        private Side side;
        
        public Side Side
        {
            get { return side; }
            set
            {
                side = value;
            }
        }
    }
}
