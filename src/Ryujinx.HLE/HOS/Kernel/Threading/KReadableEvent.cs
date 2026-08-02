using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.Horizon.Common;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KReadableEvent : KSynchronizationObject
    {
        private bool _signaled;

        public KReadableEvent(KernelContext context) : base(context)
        {
        }

        public override void Signal()
        {
            bool wasSignaled;
            int waiters;

            KernelContext.CriticalSection.Enter();

            wasSignaled = _signaled;
            waiters = WaitingThreads.Count;

            if (!_signaled)
            {
                _signaled = true;

                base.Signal();
            }

            KernelContext.CriticalSection.Leave();

            if (DebugTrace)
            {
                Logger.Info?.Print(LogClass.Kernel, $"SYNCDBG kevent-signal wasSignaled={wasSignaled} waiters={waiters}");
            }
        }

        public Result Clear()
        {
            if (DebugTrace)
            {
                Logger.Info?.Print(LogClass.Kernel, $"SYNCDBG kevent-clear wasSignaled={_signaled}");
            }

            _signaled = false;

            return Result.Success;
        }

        public Result ClearIfSignaled()
        {
            Result result;

            KernelContext.CriticalSection.Enter();

            if (_signaled)
            {
                _signaled = false;

                result = Result.Success;
            }
            else
            {
                result = KernelResult.InvalidState;
            }

            KernelContext.CriticalSection.Leave();

            if (DebugTrace)
            {
                Logger.Info?.Print(LogClass.Kernel, $"SYNCDBG kevent-clear-if-signaled result={result}");
            }

            return result;
        }

        public override bool IsSignaled()
        {
            return _signaled;
        }
    }
}
