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

        public void SwapSides()
        {
            Side = Side == Side.Left ? Side.Right : Side.Left;
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class ShuffleStep : Slide.StepTap
    {
        [Newtonsoft.Json.JsonProperty]
        private int endLaneIndexOffset = int.MaxValue;
        [Newtonsoft.Json.JsonProperty]
        private int endWidthChange;
        [Newtonsoft.Json.JsonProperty]
        private ShuffleType shuffleType;

        public ShuffleType ShuffleType
        {
            get { return shuffleType; }
            set { 
                shuffleType = value;
                // Set default shuffle values if complex is selected
                if (shuffleType == ShuffleType.Complex)
                {
                    if (EndLaneIndexOffset == int.MaxValue)
                        EndLaneIndexOffset = ParentNote.StartLaneIndex < 8 ? 4 : -4;
                }
            }
        }

        public int EndLaneIndex { get { return ParentNote.StartLaneIndex + EndLaneIndexOffset; } }

        public int EndLaneIndexOffset
        {
            get { return endLaneIndexOffset; }
            set
            {
                CheckPosition(value, endWidthChange);
                endLaneIndexOffset = value;
            }
        }

        public int EndWidthChange
        {
            get { return endWidthChange; }
            set
            {
                CheckPosition(endLaneIndexOffset, value);
                endWidthChange = value;
            }
        }

        public int EndWidth { get { return ParentNote.StartWidth + EndWidthChange; } }

        public ShuffleStep(Slide parent) : base(parent)
        { }

        public void SetEndPosition(int laneIndexOffset, int widthChange)
        {
            CheckPosition(laneIndexOffset, widthChange);
            this.endLaneIndexOffset = laneIndexOffset;
            this.endWidthChange = widthChange;
        }
    }

    public enum ShuffleType
    {
        None,
        Simple,
        Complex
    }
}
