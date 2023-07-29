using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Services
{
    public record RunningProcess(CancellationToken Cancellation)
    {
    }

    public class ProcessService : DisposableBase
    {
        public ProcessService()
        {
            Disposables.Add(
                Observable.Interval(TimeSpan.FromSeconds(10))
                          .Subscribe(_ => RefreshState())
            );
        }

        public ConcurrentDictionary<CancellationTokenSource, Process> Running { get; } = new();
        private SemaphoreSlim UpdateLock { get; } = new(1, 1);

        public CancellationTokenSource? IsRunning(string path)
        {
            if (Running.FirstOrDefault(p => p.Value.StartInfo.FileName == path)
                    is KeyValuePair<CancellationTokenSource, Process> existing &&
                    existing.Key is not null
            )
            {
                return existing.Key;
            }
            return null;
        }

        public CancellationToken Start(string path, bool hidden = false, string[]? verb = null)
        {
            if (IsRunning(path) is CancellationTokenSource source and not null)
            {
                return source.Token;
            }

            var directory = Path.GetDirectoryName(path);

            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
                    WorkingDirectory = directory,
                    Verb = verb is not null ? string.Join(" ", verb) : string.Empty,
                    UseShellExecute = true
                }
            );

            if (process is null)
                return new(true);

            var tokenSource = new CancellationTokenSource();
            Running[tokenSource] = process;
            return tokenSource.Token;
        }

        private void RefreshState()
        {
            lock (UpdateLock)
            {
                foreach (var running in Running.ToArray())
                {
                    running.Value.Refresh();
                    if (!running.Value.HasExited)
                        continue;

                    Running.Remove(running.Key, out var _);
                    running.Key.Cancel();
                    running.Value.Dispose();
                    running.Key.Dispose();
                }
            }
        }
    }
}