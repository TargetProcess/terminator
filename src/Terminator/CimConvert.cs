using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace Terminator
{
	public static class CimConvert
	{
		private const string Cim2DateTimeFormat = "yyyyMMddHHmmss.ffffff";

		private static readonly IDictionary<CimType, Type> Cim2TypeTable =
			new Dictionary<CimType, Type>
				{
					{CimType.Boolean, typeof (bool)},
					{CimType.Char16, typeof (string)},
					{CimType.DateTime, typeof (DateTime)},
					{CimType.Object, typeof (object)},
					{CimType.Real32, typeof (decimal)},
					{CimType.Real64, typeof (decimal)},
					{CimType.Reference, typeof (object)},
					{CimType.SInt16, typeof (short)},
					{CimType.SInt32, typeof (int)},
					{CimType.SInt8, typeof (sbyte)},
					{CimType.String, typeof (string)},
					{CimType.UInt8, typeof (byte)},
					{CimType.UInt16, typeof (ushort)},
					{CimType.UInt32, typeof (uint)},
					{CimType.UInt64, typeof (ulong)}
				};

		public static Type Cim2SystemType(this PropertyData data)
		{
			Type type = Cim2TypeTable[data.Type];
			if (data.IsArray)
				type = type.MakeArrayType();
			return type;
		}

		public static object Cim2SystemValue(this PropertyData data)
		{
			Type type = Cim2SystemType(data);
			return data.Type == CimType.DateTime
				       ? DateTime.ParseExact(data.Value.ToString().Substring(0, Cim2DateTimeFormat.Length), Cim2DateTimeFormat, CultureInfo.InvariantCulture)
				       : Convert.ChangeType(data.Value, type);
		}
	}
}