// Copyright (c) Interactive Media Lab Dresden, Technische Universität Dresden. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Utils
{
    public class Log : Singleton<Log>
    {
        [Tooltip("Determines if Unity's debug messages should be forwarded to the log.")]
        public bool PipeDebugLog;

        [Tooltip("Determines if log entries should be timestamped.")]
        public bool LogTime;

        [Tooltip("The minimal level of debug messages to log. Does nothing if Pipe Debug Log is set to false.")]
        public LogLevel LoggingLevel;

        [Tooltip("The maximum (approximate) buffer size in characters. If the size of a log buffer is larger, it gets flushed to disk.")]
        public int MaxBufferSize = 1000;

        [Tooltip("The maximum interval in seconds with which to flush all buffers to disk, regardless of their size.")]
        public float FlushInterval = 2.0f;

        [Tooltip("The path where logs should get saved. Needs to be writable by the application.")]
        public string LogPath;

        public enum LogLevel { All, Warning, Error };

        private Dictionary<string, StringBuilder> logBuffers = new Dictionary<string, StringBuilder>();
        private double flushTimer = 0.0f;
        private string timeString = "";
        private bool doFlush = false;

        // Update is called once per frame
        void Update()
        {
            if (Time.realtimeSinceStartupAsDouble - flushTimer >= FlushInterval || doFlush == true)
            {
                flushTimer = Time.realtimeSinceStartupAsDouble;
                FlushBuffer(MaxBufferSize);
            }
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            FlushBuffer();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
            FlushBuffer();
        }

        private void OnApplicationQuit()
        {
            Application.logMessageReceived -= HandleLog;
            FlushBuffer();
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (PipeDebugLog)
            {
                if (LoggingLevel == LogLevel.All ||
                    (LoggingLevel == LogLevel.Warning && type != LogType.Log) ||
                    (LoggingLevel == LogLevel.Error && type != LogType.Log && type != LogType.Warning))
                {
                    Write(type.ToString() + ": " + condition);
                }
            }
        }

        private void FlushBuffer(int flushSize = 0)
        {
            if (LogPath == "")
            {
                LogPath = Application.dataPath;
            }

            if (timeString == "")
            {
                timeString = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH.mm.ssK");
            }

            foreach (var buffer in logBuffers)
            {
                if (buffer.Value.Length <= flushSize)
                {
                    continue;
                }

                string path = LogPath + Path.DirectorySeparatorChar + buffer.Key + "_" + timeString + ".log";
                using (var fs = new FileStream(path, FileMode.Append))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.Write(buffer.Value.ToString());
                        sw.Flush();
                        buffer.Value.Clear();
                    }
                }
            }

            doFlush = false;
        }

        public void Write(string message, string file = "debug")
        {
            if (message == null)
            {
                return;
            }

            if (LogTime)
            {
                message = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffK ") + message;
            }

            StringBuilder buffer;
            if (logBuffers.TryGetValue(file, out buffer))
            {
                buffer.AppendLine(message);
            }
            else
            {
                buffer = new StringBuilder();
                buffer.AppendLine(message);
                logBuffers[file] = buffer;
            }

            if (buffer.Length > MaxBufferSize)
            {
                doFlush = true;
            }
        }
    }
}

