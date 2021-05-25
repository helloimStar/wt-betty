using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    class RitaProcessor : IVoiceMessageProcessor
    {
        RitaProcessor()
        {
            MsgAoAMaximum = new SoundMessage() { Sound = new SoundPlayer() }
        }
    }
}
