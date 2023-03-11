using System;
using System.Collections.Generic;
using System.Text;

namespace VedioEditor
{
    public struct MediaTrack
    {
        public MediaTrack(int startFrame, int endFrame, MediaFrameFlags flags)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
            Flags = flags;
        }

        public MediaFrameFlags Flags { get; }

        public bool HasAudio => Flags.HasFlag(MediaFrameFlags.Audio);

        public bool HasVedio => Flags.HasFlag(MediaFrameFlags.Vedio);

        public bool HasSubtitles => Flags.HasFlag(MediaFrameFlags.Subtitles);

        public int StartFrame { get; }

        public int EndFrame { get; }
    }
}
