using Common.Logging;
using Android.Util.Log;

using System;

namespace AndroidAgent
{
	class AndroidLogger : ILog {
		private static string appname = "benchmarker";

		#region ILog implementation
		public void Trace (object message)
		{
			throw new NotImplementedException ();
		}
		public void Trace (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Trace (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Trace (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Trace (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Trace (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Debug (object message)
		{
			Android.Util.Log.Debug (appname, message);
		}
		public void Debug (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void DebugFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Android.Util.Log.Debug (appname, format);
				break;
			case 1:
				Android.Util.Log.Debug (appname, String.Format (format, args[0]));
				break;
			case 2:
				Android.Util.Log.Debug (appname, String.Format (format, args[0], args[1]));
				break;
			case 3:
				Android.Util.Log.Debug (appname, String.Format (format, args[0], args[1], args[2]));
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void DebugFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void DebugFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void DebugFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Debug (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Debug (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Debug (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Debug (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Info (object message)
		{
			Android.Util.Log.Info (appname, message);
		}
		public void Info (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void InfoFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Android.Util.Log.Info (appname, format);
				break;
			case 1:
				Android.Util.Log.Info (appname, String.Format (format, args[0]));
				break;
			case 2:
				Android.Util.Log.Info (appname, String.Format (format, args[0], args[1]));
				break;
			case 3:
				Android.Util.Log.Info (appname, String.Format (format, args[0], args[1], args[2]));
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void InfoFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void InfoFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void InfoFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Info (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Info (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Info (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Info (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Warn (object message)
		{
			Android.Util.Log.Warn (appname, message);
		}
		public void Warn (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void WarnFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Android.Util.Log.Warn (appname, format);
				break;
			case 1:
				Android.Util.Log.Warn (appname, String.Format (format, args[0]));
				break;
			case 2:
				Android.Util.Log.Warn (appname, String.Format (format, args[0], args[1]));
				break;
			case 3:
				Android.Util.Log.Warn (appname, String.Format (format, args[0], args[1], args[2]));
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void WarnFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void WarnFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void WarnFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Warn (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Warn (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Warn (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Warn (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Error (object message)
		{
			Android.Util.Log.Error (appname, message);
		}
		public void Error (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void ErrorFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Android.Util.Log.Error (appname, format);
				break;
			case 1:
				Android.Util.Log.Error (appname, String.Format (format, args[0]));
				break;
			case 2:
				Android.Util.Log.Error (appname, String.Format (format, args[0], args[1]));
				break;
			case 3:
				Android.Util.Log.Error (appname, String.Format (format, args[0], args[1], args[2]));
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void ErrorFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void ErrorFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void ErrorFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Error (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Error (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Error (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Error (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (object message)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public bool IsTraceEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsDebugEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsErrorEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsFatalEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsInfoEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsWarnEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public IVariablesContext GlobalVariablesContext {
			get {
				throw new NotImplementedException ();
			}
		}
		public IVariablesContext ThreadVariablesContext {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}

