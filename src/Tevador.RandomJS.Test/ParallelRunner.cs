﻿/*
    (c) 2018 tevador <tevador@gmail.com>

    This file is part of Tevador.RandomJS.

    Tevador.RandomJS is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Tevador.RandomJS is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Tevador.RandomJS.  If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using Tevador.RandomJS.Run;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Tevador.RandomJS.Test
{
    class ParallelRunner
    {
        RuntimeStats _stats;
        ProgramOptions _options;
        long _seed;

        public ParallelRunner(long seed, ProgramOptions options)
        {
            _seed = seed;
            _options = options;
        }

        public event EventHandler Progress;

        public RuntimeStats Run(int threadCount, int programCount, ProgramOptions options = null)
        {
            _stats = new RuntimeStats(programCount);
            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                CancellationToken token = source.Token;
                Task[] runners = new Task[threadCount];
                for (int i = 0; i < threadCount; ++i)
                {
                    runners[i] = Task.Factory.StartNew(() => _run(token), token);
                }
                while (runners.Any(r => !r.IsCompleted))
                {
                    var id = Task.WaitAny(runners);
                    if (runners[id].IsFaulted)
                    {
                        try
                        {
                            source.Cancel();
                        }
                        catch { }
                    }
                }
            }
            return _stats;
        }

        public double Percent
        {
            get { return _stats.Percent; }
        }

        private void _run(CancellationToken token)
        {
            var factory = new ProgramFactory(_options);
            var runner = new ProgramRunner();
            RuntimeInfo ri;

            while ((ri = _stats.Add()) != null)
            {
                if (token.IsCancellationRequested) throw new OperationCanceledException();
                var smallSeed = Interlocked.Increment(ref _seed);
                var bigSeed = BinaryUtils.GenerateSeed(smallSeed);
                ri.Seed = BinaryUtils.ByteArrayToString(bigSeed);
                var p = factory.GenProgram(bigSeed);
                runner.WriteProgram(p);
                runner.ExecuteProgram(ri);
                if (!ri.Success)
                {
                    throw new InvalidProgramException();
                }
                Progress?.Invoke(this, EventArgs.Empty);
            }            
        }
    }
}
