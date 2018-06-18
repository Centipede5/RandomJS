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
using System.Text;
using Tevador.RandomJS.Miner.Blake;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tevador.RandomJS.Run;

namespace Tevador.RandomJS.Miner
{
    class Miner
    {
        const int N = 6; //asymmetry: solving requires 2^N times more effort than verifying
        const int _bound = (1 << (8 - N));
        const byte _clearMask = (_bound - 1);
        const int _nonceOffset = 39;
        Blake2B256 _blake = new Blake2B256();
        Blake2B256 _blakeKeyed;
        ProgramFactory _factory = new ProgramFactory();
        byte[] _blockTemplate;
        ProgramRunner _runner = new ProgramRunner("http://localhost:18111");

        public void Reset(byte[] blockTemplate)
        {
            _blockTemplate = blockTemplate;
        }

        public unsafe Solution Solve()
        {
            byte[] result = null;
            byte[] auxiliary = null;
            uint nonce;
            fixed (byte* block = _blockTemplate)
            {
                uint* noncePtr = (uint*)(block + _nonceOffset);
                do
                {
                    (*noncePtr)++;
                    byte[] key = _blake.ComputeHash(_blockTemplate);
                    var program = _factory.GenProgram(key);
                    _runner.WriteProgram(program);
                    _blakeKeyed = new Blake2B256(key);
                    auxiliary = _blakeKeyed.ComputeHash(_runner.Buffer, 0, _runner.ProgramLength);
                    var ri = _runner.ExecuteProgram();
                    if(!ri.Success)
                    {
                        throw new Exception(string.Format($"Program execution failed. Nonce value: {(*noncePtr)}. Seed: {BinaryUtils.ByteArrayToString(key)}, {ri.Output}"));
                    }
                    result = _blakeKeyed.ComputeHash(Encoding.ASCII.GetBytes(ri.Output));
                }
                while ((result[0] ^ auxiliary[0]) >= _bound);
                nonce = *noncePtr;
            }
            result[0] &= _clearMask;
            for(int i = 0; i < result.Length; ++i)
            {
                result[i] ^= auxiliary[i];
            }
            return new Solution()
            {
                Nonce = nonce,
                Result = result,
                ProofOfWork = _blakeKeyed.ComputeHash(result)
            };
        }

        public bool Verify(Solution sol)
        {
            for (int i = 0; i < 4; ++i)
            {
                _blockTemplate[_nonceOffset + i] = (byte)(sol.Nonce >> (8 * i));
            }
            byte[] key = _blake.ComputeHash(_blockTemplate);
            _blakeKeyed = new Blake2B256(key);
            var pow = _blakeKeyed.ComputeHash(sol.Result);
            if(!BinaryUtils.ArraysEqual(pow, sol.ProofOfWork))
            {
                Console.WriteLine("Invalid PoW");
                return false;
            }
            var program = _factory.GenProgram(key);
            _runner.WriteProgram(program);
            var auxiliary = _blakeKeyed.ComputeHash(_runner.Buffer, 0, _runner.ProgramLength);
            if ((auxiliary[0] ^ sol.Result[0]) >= _bound)
            {
                Console.WriteLine("Invalid Auxiliary");
                return false;
            }
            auxiliary[0] &= _clearMask;
            var ri = _runner.ExecuteProgram();
            if (!ri.Success)
            {
                throw new Exception(string.Format($"Program execution failed. Nonce value: {(sol.Nonce)}. Seed: {BinaryUtils.ByteArrayToString(key)}, {ri.Output}"));
            }
            var result = _blakeKeyed.ComputeHash(Encoding.ASCII.GetBytes(ri.Output));
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] ^= auxiliary[i];
            }
            if (!BinaryUtils.ArraysEqual(sol.Result, result))
            {
                Console.WriteLine("Invalid Result");
                return false;
            }
            return true;
        }

        

        static void Main(string[] args)
        {
            string blockTemplateHex = "0707f7a4f0d605b303260816ba3f10902e1a145ac5fad3aa3af6ea44c11869dc4f853f002b2eea0000000077b206a02ca5b1d4ce6bbfdf0acac38bded34d2dcdeef95cd20cefc12f61d56109";
            if (args.Length > 0)
            {
                blockTemplateHex = args[0];
            }
            if (blockTemplateHex.Length != 152 || blockTemplateHex.Any(c => !"0123456789abcdef".Contains(c)))
            {
                Console.WriteLine("Invalid block template (152 hex characters expected).");
            }
            else
            {
                try
                {
                    var blockTemplate = BinaryUtils.StringToByteArray(blockTemplateHex);
                    var miner = new Miner();
                    miner.Reset(blockTemplate);
                    TimeSpan period = TimeSpan.FromMinutes(1);
                    List<Solution> solutions = new List<Solution>(100);
                    Stopwatch sw = Stopwatch.StartNew();
                    while (sw.Elapsed < period)
                    {
                        var solution = miner.Solve();
                        Console.WriteLine($"Nonce = {solution.Nonce}; PoW = {BinaryUtils.ByteArrayToString(solution.ProofOfWork)}");
                        solutions.Add(solution);
                    }
                    sw.Stop();
                    var seconds = sw.Elapsed.TotalSeconds;
                    Console.WriteLine();
                    Console.WriteLine($"Solving nonces: {string.Join(", ", solutions.Select(s => s.Nonce))}");
                    Console.WriteLine();
                    Console.WriteLine($"Found {solutions.Count} solutions in {seconds} seconds. Performance = {solutions.Count / seconds} Sols./s.");
                    sw.Restart();
                    foreach (var sol in solutions)
                    {
                        if (!miner.Verify(sol))
                        {
                            Console.WriteLine($"Nonce {sol.Nonce} - verification failed");
                            return;
                        }
                    }
                    sw.Stop();
                    Console.WriteLine($"All {solutions.Count} solutions were verified in {sw.Elapsed.TotalSeconds} seconds");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e}");
                }
            }
        }
    }
}
