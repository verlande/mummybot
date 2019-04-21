using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mummybot.Services
{
    public interface INService
    {
    }

    public interface IUnloadableService
    {
        Task Unload();
    }
}
