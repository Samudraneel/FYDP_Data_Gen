using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace driver
{
    class USBThread
    {
        public int waitTime = 20000;
        public bool waiting = true;

        public USBThread() { }

        //public void test()
        //{
        //    USB glove = new USB();

        //    glove.Open(115200, 1000, 1000);
        //    glove.Read();

        //    string path = "C:\\Users\\sam_n\\Documents\\lmao.txt";

        //    using (StreamWriter w = File.AppendText(path))
        //    {
        //        while(GlobalQueue.GetSize() > 0)
        //        {
        //            w.Write(GlobalQueue.Get());
        //        }
        //    }
        //}

        public void doWhile()
        {
            USB glove = new USB();

            glove.Open(115200, 1000, 1000);

            string path = "C:\\Users\\sam_n\\Documents\\lmao.txt";

            while (waiting)
            {
                using (StreamWriter w = File.AppendText(path))
                {
                    //w.Write(GlobalQueue.Get());
                }
            }
        }

        public void waitThread()
        {
            Thread.Sleep(waitTime);
            waiting = false;
        }
    }
}
