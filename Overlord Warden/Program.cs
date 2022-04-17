using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Overlord_Warden {
    internal class Program {
        static void Main() {
            SpeechRecognizer speechRecognizer = new SpeechRecognizer();
            Task.Run(() => speechRecognizer.AsyncStart());
            
            while (true) {
                Console.ReadLine();
            }
        }
    }
}
