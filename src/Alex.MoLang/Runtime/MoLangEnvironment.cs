using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Alex.MoLang.Runtime.Exceptions;
using Alex.MoLang.Runtime.Struct;
using Alex.MoLang.Runtime.Value;

namespace Alex.MoLang.Runtime
{
	public class MoLangEnvironment : IMoValue
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger(typeof(MoLangEnvironment));
		/// <inheritdoc />
		public object Value => Structs;

		public Dictionary<string, IMoStruct> Structs { get; } = new(StringComparer.OrdinalIgnoreCase);

		public IMoValue GetValue(string name) {
			return GetValue(name, MoParams.Empty);
		}

		public IMoValue GetValue(string name, MoParams param) {
			try
			{
				string[] segments = name.Split(".");
				string main = segments[0]; //.Dequeue();

				//if (!Structs.ContainsKey(main))
				//{
				//	throw new MoLangRuntimeException($"Cannot retrieve struct: {name}", null);
				//}

				return Structs[main].Get(string.Join(".", segments.Skip(1)), param);
			}
			catch (Exception ex)
			{
				throw new MoLangRuntimeException($"Cannot retrieve struct: {name}", ex);
			}
		}

		public void SetValue(String name, IMoValue value)
		{
			try
			{
				string[] segments = name.Split(".");
				string main = segments[0]; //.Dequeue();

				//if (!Structs.ContainsKey(main)) {
				//	throw new MoLangRuntimeException($"Cannot set value on struct: {name}", null);
				//}

				Structs[main].Set(string.Join(".", segments.Skip(1)), value);
			}
			catch (Exception ex)
			{
				throw new MoLangRuntimeException($"Cannot set value on struct: {name}", ex);
			}
		}
		
		/// <inheritdoc />
		public bool Equals(IMoValue b)
		{
			return Equals((object)b);
		}
	}
}