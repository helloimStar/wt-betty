using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wt_betty.Entities
{
    public class VoiceProcessorFactory
    {
        public static VoiceProcessor GetProcessor(VoiceTemplate voiceTemplate)
        {
            switch (voiceTemplate)
            {
                case VoiceTemplate.RU_Rita:
                    return RitaVoiceProcessor.Instance;
                default:
                    return BettyVoiceProcessor.Instance;
            }
        }
    }
}
