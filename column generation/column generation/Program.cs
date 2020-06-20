using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace column_generation
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;
            str += "input_file";
            read_file r = null ;
            try
            {
                r = new read_file(str);
            }
            catch (Exception)
            {
                Console.WriteLine("请关闭输入文件！！！");
            }
            CG c = new CG(r);
            Console.WriteLine("正在计算。。。。。。。。。。。。。。。");
            c.main();
            Console.WriteLine("*****************************************");
            Console.WriteLine("计算完毕，请打开NEXTA.exe查看");
            Console.ReadLine();
        }
    }
}
