﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Alex.MoLang.Parser;
using Alex.MoLang.Parser.Tokenizer;
using Alex.MoLang.Runtime;
using Alex.MoLang.Runtime.Struct;
using Alex.MoLang.Runtime.Value;

namespace Alex.MoLang.TestApp
{
	class Program
	{
		static void Main(string[] args)
		{
			TokenIterator tokenIterator = new TokenIterator(@"return 3 * (2 + 2);");
			MoLangParser  parser        = new MoLangParser(tokenIterator);
			var           expressions   = parser.Parse();

			Stopwatch     sw      = Stopwatch.StartNew();
			MoLangRuntime runtime = new MoLangRuntime();
			runtime.Environment.Structs.TryAdd("query", new QueryStruct(new KeyValuePair<string, Func<MoParams, object>>[]
			{
				new ("life_time", moParams =>
				{
					return new DoubleValue(sw.Elapsed.TotalSeconds);
				})
			}));

			int _frames = 0;
			while (sw.Elapsed < TimeSpan.FromSeconds(10))
			{
				Console.WriteLine($"[{_frames}]: " + runtime.Execute(expressions).AsDouble());
				_frames++;
				
				Thread.Sleep(13);
			}
		}
	}
}