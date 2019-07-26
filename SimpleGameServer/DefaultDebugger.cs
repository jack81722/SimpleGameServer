using GameSystem.GameCore.Debugger;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleGameServer
{
    public class DefaultDebugger : LazySingleton<DefaultDebugger>, IDebugger
    {
        public void Log(object obj)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(obj);
        }

        public void LogError(object obj)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(obj);
        }

        public void LogWarning(object obj)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(obj);
        }
    }
}
