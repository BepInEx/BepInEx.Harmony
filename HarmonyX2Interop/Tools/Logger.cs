using System;
using System.IO;
using HarmonyXInterop;

namespace HarmonyLib.Tools
{
    /// <summary>
    /// Default Harmony logger that writes to a file
    /// </summary>
    public static class HarmonyFileLog
    {
        private static bool _enabled;
        private static TextWriter _textWriter;

        /// <summary>
        /// Whether or not to enable writing the log.
        /// </summary>
        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                ToggleDebug();
            }
        }

        /// <summary>
        /// Text writer to write the logs to. If not set, defaults to a file log.
        /// </summary>
        public static TextWriter Writer
        {
            get => _textWriter;
            set
            {
                _textWriter?.Flush();
                _textWriter = value;
            }
        }

        /// <summary>
        /// File path of the log.
        /// </summary>
        public static string FileWriterPath { get; set; } = "HarmonyLog.txt";

        private static void ToggleDebug()
        {
            if (Enabled)
            {
                if (Writer == null)
                    Writer = new StreamWriter(File.Create(Path.GetFullPath(FileWriterPath)));
                Logger.MessageReceived += OnMessage;
            }
            else
                Logger.MessageReceived -= OnMessage;
        }

        private static void OnMessage(object sender, Logger.LogEventArgs e)
        {
            Writer.WriteLine($"[{e.LogChannel}] {e.Message}");
        }
    }

    /// <summary>
    /// Main logger class that exposes log events.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// A single log event that represents a single log message.
        /// </summary>
        public class LogEventArgs : EventArgs
        {
            /// <summary>
            /// Log channel of the message.
            /// </summary>
            public LogChannel LogChannel { get; internal set; }

            /// <summary>
            /// The log message.
            /// </summary>
            public string Message { get; internal set; }
        }

        /// <summary>
        /// Log channel for the messages.
        /// </summary>
        [Flags]
        public enum LogChannel
        {
            /// <summary>
            /// No channels (or an empty channel).
            /// </summary>
            None = 0,

            /// <summary>
            /// Basic information.
            /// </summary>
            Info = 1 << 1,

            /// <summary>
            /// Full IL dumps of the generated dynamic methods.
            /// </summary>
            IL = 1 << 2,

            /// <summary>
            /// Channel for warnings.
            /// </summary>
            Warn = 1 << 3,

            /// <summary>
            /// Channel for errors.
            /// </summary>
            Error = 1 << 4,

            /// <summary>
            /// All channels.
            /// </summary>
            All = Info | IL | Warn | Error
        }

        /// <summary>
        /// Filter for which channels should be listened to.
        /// If the channel is in the filter, all log messages from that channel get propagated into <see cref="MessageReceived"/> event.
        /// </summary>
        public static LogChannel ChannelFilter { get; set; } = LogChannel.None;

        /// <summary>
        /// Event fired on any incoming message that passes the channel filter.
        /// </summary>
#pragma warning disable 67
        public static event EventHandler<LogEventArgs> MessageReceived;
#pragma warning restore 67

        internal static void Log(LogChannel channel, Func<string> message)
        {
            HarmonyInterop.Log((int) channel, message);
        }

        internal static void LogText(LogChannel channel, string message)
        {
            HarmonyInterop.LogText((int) channel, message);
        }
    }
}