using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Services;
using System.Collections.Concurrent;





public class WebSocketService {
     private readonly ConcurrentDictionary<Guid, StreamWriter> _clients = new();

     public Guid RegisterClient(StreamWriter writer) {
         var clientId = Guid.NewGuid();
         _clients.TryAdd(clientId, writer);
         return clientId;
     }

     public void UnregisterClient(Guid clientId) => _clients.TryRemove(clientId, out _);

     public async Task NotifyClientsAsync(object data) {
         var message = $"data: {System.Text.Json.JsonSerializer.Serialize(data)}\n\n";

         foreach (var client in _clients.Values) {
             try {
                 await client.WriteAsync(message);
                 await client.FlushAsync();
             } catch {
                 //
             }
         }
     }

     public int GetConnectedClientsCount() => _clients.Count;
}