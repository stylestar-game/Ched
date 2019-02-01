using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public class Step : SteppableBase
    {
        public void SwapSides()
        {
            Side = Side == Side.Left ? Side.Right : Side.Left;
        }
    }
}
