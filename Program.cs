using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessLoad
{
    public class Program
    {
        [STAThread]
        public static void Main() => new App().Run();
    }
}
                       