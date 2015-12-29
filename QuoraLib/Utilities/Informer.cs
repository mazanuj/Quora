﻿using System;

namespace QuoraLib.Utilities
{
    public static class Informer
    {
        public delegate void InformMethodInt(int result);

        public delegate void InformMethodStr(string str);

        public static event InformMethodStr OnResultStr;
        public static event InformMethodInt OnQueueChanged;

        public static void RaiseOnQueueChanged(int result)
        {
            var handler = OnQueueChanged;
            handler?.Invoke(result);
        }

        public static void RaiseOnResultReceived(string str)
        {
            var handler = OnResultStr;
            handler?.Invoke(str);
        }

        //public static void RaiseOnResultReceived(Exception ex)
        //{
        //    var handler = OnResultStr;
        //    handler?.Invoke(ex.Message);
        //    //handler?.Invoke(ex.StackTrace);
        //}
    }
}