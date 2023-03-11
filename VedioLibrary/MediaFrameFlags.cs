using System;
using System.Collections.Generic;
using System.Text;

namespace VedioEditor
{
    [Flags]
    public enum MediaFrameFlags
    {
        None = 0,
        Vedio = 1,
        Audio = 2,
        Subtitles = 4,
        VedioAudio = Vedio | Audio,
        SubtitlesVedio = Subtitles | Vedio,
        SubtitlesAudio = Subtitles | Audio,
        All = Vedio | Audio | Subtitles
    }
}
