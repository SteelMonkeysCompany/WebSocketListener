﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable ConsiderUsingAsyncSuffix

namespace vtortola.WebSockets.Tools
{
    internal static class TaskHelper
    {
        public static readonly Task CanceledTask;
        public static readonly Task CompletedTask;
        public static readonly string DefaultAggregateExceptionMessage;
        public static readonly Task ExpiredTask;
        public static readonly Task<bool> TrueTask;
        public static readonly Task<bool> FalseTask;

        static TaskHelper()
        {
            CompletedTask = Task.FromResult<object>(null);
            TrueTask = Task.FromResult<bool>(true);
            FalseTask = Task.FromResult<bool>(false);

            var expired = new TaskCompletionSource<object>();
            expired.SetException(new TimeoutException());
            ExpiredTask = expired.Task;

            var canceled = new TaskCompletionSource<object>();
            canceled.SetCanceled();
            CanceledTask = canceled.Task;

            DefaultAggregateExceptionMessage = new AggregateException().Message;
        }

        public static Task FailedTask(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error), "error != null");

            return FailedTask<object>(error);
        }
        public static Task<T> FailedTask<T>(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error), "error != null");

            error = error.Unwrap();

            var tc = new TaskCompletionSource<T>();
            if (error is OperationCanceledException) tc.SetCanceled();
            else if (error is AggregateException && string.Equals(error.Message, DefaultAggregateExceptionMessage, StringComparison.Ordinal)) tc.SetException((error as AggregateException).InnerExceptions);
            else tc.SetException(error);
            return tc.Task;
        }

        public static Task FailedTask(IEnumerable<Exception> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors), "errors != null");

            return FailedTask<object>(errors);
        }
        public static Task<T> FailedTask<T>(IEnumerable<Exception> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors), "errors != null");

            var tc = new TaskCompletionSource<T>();
            tc.SetException(errors);
            return tc.Task;
        }

        public static void LogFault(this Task task, ILogger log, string message = null, [CallerMemberName] string memberName = "Task", [CallerFilePath] string sourceFilePath = "<no file>", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (task == null) throw new ArgumentNullException(nameof(task), "task != null");
            if (log == null) throw new ArgumentNullException(nameof(log), "log != null");

            var sourceFileName = sourceFilePath != null ? Path.GetFileName(sourceFilePath) : "<no file>";
            task.ContinueWith(faultedTask => log.Error($"[{sourceFileName}:{sourceLineNumber.ToString()}, {memberName}] {message ?? "An error occurred while performing task"}.", faultedTask.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default
            );
        }
    }
}
