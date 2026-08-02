using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.Horizon.Common;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KSynchronization
    {
        private readonly KernelContext _context;

        public KSynchronization(KernelContext context)
        {
            _context = context;
        }

        public Result WaitFor(Span<KSynchronizationObject> syncObjs, long timeout, out int handleIndex)
        {
            handleIndex = 0;

            Result result = KernelResult.TimedOut;

            bool trace = false;

            for (int index = 0; index < syncObjs.Length; index++)
            {
                if (syncObjs[index].DebugTrace)
                {
                    trace = true;
                    break;
                }
            }

            long traceStart = trace ? System.Environment.TickCount64 : 0;

            _context.CriticalSection.Enter();

            // Check if objects are already signaled before waiting.
            for (int index = 0; index < syncObjs.Length; index++)
            {
                if (!syncObjs[index].IsSignaled())
                {
                    continue;
                }

                handleIndex = index;

                _context.CriticalSection.Leave();

                if (trace)
                {
                    Logger.Info?.Print(LogClass.Kernel, $"SYNCDBG svc-wait immediate handle={index} timeout={timeout}");
                }

                return Result.Success;
            }

            if (trace)
            {
                Logger.Info?.Print(LogClass.Kernel, $"SYNCDBG svc-wait sleeping count={syncObjs.Length} timeout={timeout}");
            }

            if (timeout == 0)
            {
                _context.CriticalSection.Leave();

                return result;
            }

            KThread currentThread = KernelStatic.GetCurrentThread();

            if (currentThread.TerminationRequested)
            {
                result = KernelResult.ThreadTerminating;
            }
            else if (currentThread.SyncCancelled)
            {
                currentThread.SyncCancelled = false;

                result = KernelResult.Cancelled;
            }
            else
            {
                LinkedListNode<KThread>[] syncNodesArray = ArrayPool<LinkedListNode<KThread>>.Shared.Rent(syncObjs.Length);

                Span<LinkedListNode<KThread>> syncNodes = syncNodesArray.AsSpan(0, syncObjs.Length);

                for (int index = 0; index < syncObjs.Length; index++)
                {
                    syncNodes[index] = syncObjs[index].AddWaitingThread(currentThread);
                }

                currentThread.WaitingSync = true;
                currentThread.SignaledObj = null;
                currentThread.ObjSyncResult = result;

                currentThread.Reschedule(ThreadSchedState.Paused);

                if (timeout > 0)
                {
                    _context.TimeManager.ScheduleFutureInvocation(currentThread, timeout);
                }

                _context.CriticalSection.Leave();

                currentThread.WaitingSync = false;

                if (timeout > 0)
                {
                    _context.TimeManager.UnscheduleFutureInvocation(currentThread);
                }

                _context.CriticalSection.Enter();

                result = currentThread.ObjSyncResult;

                handleIndex = -1;

                for (int index = 0; index < syncObjs.Length; index++)
                {
                    syncObjs[index].RemoveWaitingThread(syncNodes[index]);

                    if (syncObjs[index] == currentThread.SignaledObj)
                    {
                        handleIndex = index;
                    }
                }

                ArrayPool<LinkedListNode<KThread>>.Shared.Return(syncNodesArray, true);
            }

            _context.CriticalSection.Leave();

            if (trace)
            {
                Logger.Info?.Print(LogClass.Kernel, $"SYNCDBG svc-wait exit result={result} handle={handleIndex} elapsed={System.Environment.TickCount64 - traceStart}ms");
            }

            return result;
        }

        public void SignalObject(KSynchronizationObject syncObj)
        {
            _context.CriticalSection.Enter();

            if (syncObj.IsSignaled())
            {
                LinkedListNode<KThread> node = syncObj.WaitingThreads.First;

                while (node != null)
                {
                    KThread thread = node.Value;

                    if ((thread.SchedFlags & ThreadSchedState.LowMask) == ThreadSchedState.Paused)
                    {
                        thread.SignaledObj = syncObj;
                        thread.ObjSyncResult = Result.Success;

                        thread.Reschedule(ThreadSchedState.Running);
                    }

                    node = node.Next;
                }
            }

            _context.CriticalSection.Leave();
        }
    }
}
