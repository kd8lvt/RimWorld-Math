using System;

namespace CrunchyDuck.Math
{
	/// <summary>
	/// Stores information to uniquely identify an <see cref="Exception"/>.
	/// </summary>
	internal readonly struct ExceptionIdentifier
	{
#pragma warning disable IDE0052 // Suppress "Remove unread private members" messages. These fields allow a unique hashcode and equality check to be created from this struct.
		private readonly string _context;
		private readonly string _message;
		private readonly string _stackTrace;
		private readonly string _source;
		private readonly int _hResult;
#pragma warning restore IDE0052

		/// <summary>
		/// Creates an <see cref="ExceptionIdentifier"/> from an <see cref="Exception"/>.
		/// </summary>
		public ExceptionIdentifier(Exception e, in string context)
		{
			_context = context;
			_message = e.Message;
			_stackTrace = e.StackTrace;
			_source = e.Source;
			_hResult = e.HResult;
		}

		/// <summary>
		/// Creates an <see cref="ExceptionIdentifier"/> from an error message.
		/// </summary>
		public ExceptionIdentifier(in string message, in string context)
		{
			_context = context;
			_message = message;
			_stackTrace = string.Empty;
			_source = string.Empty;
			_hResult = -1;
		}
	}
}
