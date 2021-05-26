using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wt_betty.Utils
{
    public sealed class RepeatableTask
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            bool firstRun = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!firstRun)
                    await Task.Delay(period, cancellationToken);
                firstRun = false;

                if (!cancellationToken.IsCancellationRequested)
                    action();
            }
        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}
