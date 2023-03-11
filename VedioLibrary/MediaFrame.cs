using System;
using System.Collections.Generic;
using System.Text;

namespace VedioEditor
{
    public struct MediaFrame
    {
        public MediaFrame(TimeSpan vedioTime, int vedioFrame, int audioFrame, IAccuracy accuracy, MediaFrameFlags flags)
        {
            TimeOffset = vedioTime;
            VedioFrame = vedioFrame;
            AudioFrame = audioFrame;
            Accuracy = accuracy;
            Flags = flags;
        }

        public TimeSpan TimeOffset { get; }

        public int VedioFrame { get; }

        public int AudioFrame { get; }

        public IAccuracy Accuracy { get; }

        public MediaFrameFlags Flags { get; }
    }
}
