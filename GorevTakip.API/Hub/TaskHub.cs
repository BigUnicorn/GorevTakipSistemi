using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GorevTakip.API.Hubs
{
    public class TaskHub : Hub
    {
        // İstemciler (Frontend) doğrudan bu sınıfa bağlanacak. 
        // Mesajları Controller üzerinden tetikleyeceğimiz için burası şimdilik boş kalabilir.
    }
}