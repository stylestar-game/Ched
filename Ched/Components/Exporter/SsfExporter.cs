﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using ConcurrentPriorityQueue;
using Ched.Core;
using Ched.Core.Notes;
using Ched.Core.Events;

namespace Ched.Components.Exporter
{
    public class SsfExporter : IExtendedExpoerter<SsfArgs>
    {
        public string FormatName
        {
            get { return "StyleStar File(*.ssf)"; }
        }

        public SsfArgs CustomArgs { get; set; }

        public void Export(string path, ScoreBook book)
        {
            SsfArgs args = CustomArgs;
            var notes = book.Score.Notes;
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("This file was generated by Ched {0}.", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());

                writer.WriteLine("#TITLE \"{0}\"", book.Title);
                writer.WriteLine("#ARTIST \"{0}\"", book.ArtistName);
                writer.WriteLine("#DESIGNER \"{0}\"", book.NotesDesignerName);
                writer.WriteLine("#DIFFICULTY {0}", (int)args.PlayDifficulty + (string.IsNullOrEmpty(args.ExtendedDifficulty) ? "" : ":" + args.ExtendedDifficulty));
                writer.WriteLine("#PLAYLEVEL {0}", args.PlayLevel);
                writer.WriteLine("#SONGID \"{0}\"", args.SongId);
                writer.WriteLine("#WAVE \"{0}\"", args.SoundFileName);
                writer.WriteLine("#WAVEOFFSET {0}", args.SoundOffset);
                writer.WriteLine("#JACKET \"{0}\"", args.JacketFilePath);

                writer.WriteLine();

                writer.WriteLine("#REQUEST \"ticks_per_beat {0}\"", book.Score.TicksPerBeat);

                writer.WriteLine();

                int barTick = book.Score.TicksPerBeat * 4;
                var barIndexCalculator = new BarIndexCalculator(barTick, book.Score.Events.TimeSignatureChangeEvents, args.HasPaddingBar);

                foreach (var item in barIndexCalculator.TimeSignatures)
                {
                    writer.WriteLine("#{0:000}02: {1}", item.StartBarIndex + (args.HasPaddingBar && item.StartBarIndex == 1 ? -1 : 0), 4f * item.TimeSignature.Numerator / item.TimeSignature.Denominator);
                }

                writer.WriteLine();

                var bpmlist = book.Score.Events.BPMChangeEvents
                    .GroupBy(p => p.BPM)
                    .SelectMany((p, i) => p.Select(q => new { Index = i, Value = q, BarPosition = barIndexCalculator.GetBarPositionFromTick(q.Tick) }))
                    .ToList();

                if (bpmlist.Count >= 36 * 36) throw new ArgumentException("BPM定義数が上限を超えました。");

                var bpmIdentifiers = EnumerateIdentifiers(2).Skip(1).Take(bpmlist.Count).ToList();
                foreach (var item in bpmlist)
                {
                    writer.WriteLine("#BPM{0}: {1}", bpmIdentifiers[item.Index], item.Value.BPM);
                }

                if (args.HasPaddingBar)
                    writer.WriteLine("#{0:000}08: {1:x2}", 0, bpmIdentifiers[bpmlist.OrderBy(p => p.Value.Tick).First().Index]);

                foreach (var eventInBar in bpmlist.GroupBy(p => p.BarPosition.BarIndex))
                {
                    var sig = barIndexCalculator.GetTimeSignatureFromBarIndex(eventInBar.Key);
                    int barLength = barTick * sig.Numerator / sig.Denominator;
                    var dic = eventInBar.ToDictionary(p => p.BarPosition.TickOffset, p => p);
                    int gcd = eventInBar.Select(p => p.BarPosition.TickOffset).Aggregate(barLength, (p, q) => GetGcd(p, q));
                    writer.Write("#{0:000}08: ", eventInBar.Key);
                    for (int i = 0; i * gcd < barLength; i++)
                    {
                        int tickOffset = i * gcd;
                        writer.Write(dic.ContainsKey(tickOffset) ? bpmIdentifiers[dic[tickOffset].Index] : "00");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine();
                var speeds = book.Score.Events.HighSpeedChangeEvents.Select(p =>
                {
                    var barPos = barIndexCalculator.GetBarPositionFromTick(p.Tick);
                    return string.Format("{0}'{1}:{2}", args.HasPaddingBar && barPos.BarIndex == 1 && barPos.TickOffset == 0 ? 0 : barPos.BarIndex, barPos.TickOffset, p.SpeedRatio);
                });
                writer.WriteLine("#TIL00: \"{0}\"", string.Join(", ", speeds));
                writer.WriteLine("#HISPEED 00");

                writer.WriteLine();

                var shortNotes = notes.Steps.Cast<TappableBase>().Select(p => new { Type = ((SteppableBase)p).Side == Side.Left ? '1' : '2', Note = p })
                    .Concat(notes.Motions.Cast<TappableBase>().Select(p => new {  Type = ((MotionableBase)p).Direction == Direction.Up ? '3' : '4', Note = p}))
                    .Select(p => new
                    {
                        BarPosition = barIndexCalculator.GetBarPositionFromTick(p.Note.Tick),
                        LaneIndex = p.Note.LaneIndex,
                        Width = p.Note.Width,
                        Type = p.Type
                    });

                foreach (var notesInBar in shortNotes.GroupBy(p => p.BarPosition.BarIndex))
                {
                    foreach (var notesInLane in notesInBar.GroupBy(p => p.LaneIndex))
                    {
                        var sig = barIndexCalculator.GetTimeSignatureFromBarIndex(notesInBar.Key);
                        int barLength = barTick * sig.Numerator / sig.Denominator;

                        var offsetList = notesInLane.GroupBy(p => p.BarPosition.TickOffset).Select(p => p.ToList());
                        var separatedNotes = Enumerable.Range(0, offsetList.Max(p => p.Count)).Select(p => offsetList.Where(q => q.Count >= p + 1).Select(q => q[p]));

                        foreach (var dic in separatedNotes.Select(p => p.ToDictionary(q => q.BarPosition.TickOffset, q => q)))
                        {
                            int gcd = dic.Values.Select(p => p.BarPosition.TickOffset).Aggregate(barLength, (p, q) => GetGcd(p, q));
                            writer.Write("#{0:000}1{1}:", notesInBar.Key, notesInLane.Key.ToString("x"));
                            for (int i = 0; i * gcd < barLength; i++)
                            {
                                int tickOffset = i * gcd;
                                writer.Write(dic.ContainsKey(tickOffset) ? dic[tickOffset].Type + ToLaneWidthString(dic[tickOffset].Width) : "00");
                            }
                            writer.WriteLine();
                        }
                    }
                }

                var identifier = new IdentifierAllocationManager();

                var slideSteps = notes.SlideSteps
                    .OrderBy(p => p.StartTick)
                    .Select(p => new
                    {
                        Identifier = identifier.Allocate(p.StartTick, p.GetDuration()),
                        Note = p
                    });

                foreach (var slide in slideSteps)
                {
                    var start = new[] { new
                    {
                        TickOffset = 0,
                        BarPosition = barIndexCalculator.GetBarPositionFromTick(slide.Note.StartTick),
                        LaneIndex = slide.Note.StartLaneIndex,
                        Width = slide.Note.StartWidth,
                        Type = "1"
                    } };
                    var steps = slide.Note.StepNotes.OrderBy(p => p.TickOffset).Select(p => new
                    {
                        TickOffset = p.TickOffset,
                        BarPosition = barIndexCalculator.GetBarPositionFromTick(p.Tick),
                        LaneIndex = p.LaneIndex,
                        Width = p.Width,
                        Type = p.IsVisible ? "5" : "4"
                    }).Take(slide.Note.StepNotes.Count - 1);
                    var endNote = slide.Note.StepNotes.OrderBy(p => p.TickOffset).Last();
                    var end = new[] { new
                    {
                        TickOffset = endNote.TickOffset,
                        BarPosition= barIndexCalculator.GetBarPositionFromTick(endNote.Tick),
                        LaneIndex = endNote.LaneIndex,
                        Width = endNote.Width,
                        Type = endNote.IsVisible ? "3" : "2"
                    } };
                    var slideNotes = start.Concat(steps).Concat(end);
                    foreach (var notesInBar in slideNotes.GroupBy(p => p.BarPosition.BarIndex))
                    {
                        foreach (var notesInLane in notesInBar.GroupBy(p => p.LaneIndex))
                        {
                            var sig = barIndexCalculator.GetTimeSignatureFromBarIndex(notesInBar.Key);
                            int barLength = barTick * sig.Numerator / sig.Denominator;
                            int gcd = notesInLane.Select(p => p.BarPosition.TickOffset).Aggregate(barLength, (p, q) => GetGcd(p, q));
                            var dic = notesInLane.ToDictionary(p => p.BarPosition.TickOffset, p => p);
                            writer.Write("#{0:000}{1}{2}{3}:", notesInBar.Key, slide.Note.Side == Side.Left ? "2" : "3", notesInLane.Key.ToString("x"), slide.Identifier);
                            for (int i = 0; i * gcd < barLength; i++)
                            {
                                int tickOffset = i * gcd;
                                writer.Write(dic.ContainsKey(tickOffset) ? dic[tickOffset].Type + ToLaneWidthString(dic[tickOffset].Width) : "00");
                            }
                            writer.WriteLine();
                        }
                    }
                }
            }
        }

        public static int GetGcd(int a, int b)
        {
            if (a < b) return GetGcd(b, a);
            if (b == 0) return a;
            return GetGcd(b, a % b);
        }

        public static string ToLaneWidthString(int width)
        {
            return width == 16 ? "g" : width.ToString("x");
        }

        public static IEnumerable<string> EnumerateIdentifiers(int digits)
        {
            var num = Enumerable.Range(0, 10).Select(p => (char)('0' + p));
            var alpha = Enumerable.Range(0, 26).Select(p => (char)('A' + p));
            var seq = num.Concat(alpha).Select(p => p.ToString()).ToList();

            return EnumerateIdentifiers(digits, seq);
        }

        private static IEnumerable<string> EnumerateIdentifiers(int digits, List<string> seq)
        {
            if (digits < 1) throw new ArgumentOutOfRangeException("digits");
            if (digits == 1) return seq;
            return EnumerateIdentifiers(digits - 1, seq).SelectMany(p => seq.Select(q => p + q));
        }

        public class IdentifierAllocationManager
        {
            private int lastStartTick;
            private Stack<char> IdentifierStack;
            private ConcurrentPriorityQueue<Tuple<int, char>, int> UsedIdentifiers;

            public IdentifierAllocationManager()
            {
                Clear();
            }

            public void Clear()
            {
                lastStartTick = 0;
                IdentifierStack = new Stack<char>(Enumerable.Range(0, 26).Select(p => (char)('A' + p)).Reverse());
                UsedIdentifiers = new ConcurrentPriorityQueue<Tuple<int, char>, int>();
            }

            public char Allocate(int startTick, int duration)
            {
                if (startTick < lastStartTick) throw new InvalidOperationException("startTick must not be less than last called value.");
                while (UsedIdentifiers.Count > 0 && UsedIdentifiers.Peek().Item1 < startTick)
                {
                    IdentifierStack.Push(UsedIdentifiers.Dequeue().Item2);
                }
                char c = IdentifierStack.Pop();
                int endTick = startTick + duration;
                UsedIdentifiers.Enqueue(Tuple.Create(endTick, c), -endTick);
                lastStartTick = startTick;
                return c;
            }
        }

        public class BarIndexCalculator
        {
            private bool hasPaddingBar;
            private int barTick;
            private SortedDictionary<int, TimeSignatureItem> timeSignatures;

            /// <summary>
            /// 時間順にソートされた有効な拍子変更イベントのコレクションを取得します。
            /// </summary>
            public IEnumerable<TimeSignatureItem> TimeSignatures
            {
                get { return timeSignatures.Select(p => p.Value).Reverse(); }
            }

            public BarIndexCalculator(int barTick, IEnumerable<TimeSignatureChangeEvent> events, bool hasPaddingBar)
            {
                this.hasPaddingBar = hasPaddingBar;
                this.barTick = barTick;
                var ordered = events.OrderBy(p => p.Tick).ToList();
                var dic = new SortedDictionary<int, TimeSignatureItem>();
                int pos = 0;
                int barIndex = hasPaddingBar ? 1 : 0;
                for (int i = 0; i < ordered.Count; i++)
                {
                    var item = new TimeSignatureItem()
                    {
                        StartTick = pos,
                        StartBarIndex = barIndex,
                        TimeSignature = ordered[i]
                    };

                    // 時間逆順で追加
                    if (dic.ContainsKey(pos)) dic[-pos] = item;
                    else dic.Add(-pos, item);

                    if (i < ordered.Count - 1)
                    {
                        int barLength = barTick * ordered[i].Numerator / ordered[i].Denominator;
                        int duration = ordered[i + 1].Tick - pos;
                        pos += duration / barLength * barLength;
                        barIndex += duration / barLength;
                    }
                }

                timeSignatures = dic;
            }

            public BarPosition GetBarPositionFromTick(int tick)
            {
                foreach (var item in timeSignatures)
                {
                    if (tick < item.Value.StartTick) continue;
                    var sig = item.Value.TimeSignature;
                    int barLength = barTick * sig.Numerator / sig.Denominator;
                    int tickOffset = tick - item.Value.StartTick;
                    int barOffset = tickOffset / barLength;
                    return new BarPosition()
                    {
                        BarIndex = item.Value.StartBarIndex + barOffset,
                        TickOffset = tickOffset - barOffset * barLength,
                        TimeSignature = item.Value.TimeSignature
                    };
                }

                throw new InvalidOperationException();
            }

            public TimeSignatureChangeEvent GetTimeSignatureFromBarIndex(int barIndex)
            {
                foreach (var item in timeSignatures)
                {
                    if (barIndex < item.Value.StartBarIndex) continue;
                    return item.Value.TimeSignature;
                }

                throw new InvalidOperationException();
            }

            public struct BarPosition
            {
                public int BarIndex { get; set; }
                public int TickOffset { get; set; }
                public TimeSignatureChangeEvent TimeSignature { get; set; }
            }

            public class TimeSignatureItem
            {
                public int StartTick { get; set; }
                public int StartBarIndex { get; set; }
                public TimeSignatureChangeEvent TimeSignature { get; set; }
            }
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class SsfArgs
    {
        [Newtonsoft.Json.JsonProperty]
        private string playLevel;
        [Newtonsoft.Json.JsonProperty]
        private Difficulty playDificulty;
        [Newtonsoft.Json.JsonProperty]
        private string extendedDifficulty;
        [Newtonsoft.Json.JsonProperty]
        private string songId;
        [Newtonsoft.Json.JsonProperty]
        private string soundFileName;
        [Newtonsoft.Json.JsonProperty]
        private decimal soundOffset;
        [Newtonsoft.Json.JsonProperty]
        private string jacketFilePath;
        [Newtonsoft.Json.JsonProperty]
        private bool hasPaddingBar;

        public string PlayLevel
        {
            get { return playLevel; }
            set { playLevel = value; }
        }

        public Difficulty PlayDifficulty
        {
            get { return playDificulty; }
            set { playDificulty = value; }
        }

        public string ExtendedDifficulty
        {
            get { return extendedDifficulty; }
            set { extendedDifficulty = value; }
        }

        public string SongId
        {
            get { return songId; }
            set { songId = value; }
        }

        public string SoundFileName
        {
            get { return soundFileName; }
            set { soundFileName = value; }
        }

        public decimal SoundOffset
        {
            get { return soundOffset; }
            set { soundOffset = value; }
        }

        public string JacketFilePath
        {
            get { return jacketFilePath; }
            set { jacketFilePath = value; }
        }

        public bool HasPaddingBar
        {
            get { return hasPaddingBar; }
            set { hasPaddingBar = value; }
        }

        public enum Difficulty
        {
            Easy,
            Normal
        }
    }
}
