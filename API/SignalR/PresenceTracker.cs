using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace API.SignalR
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> _onlineUsers = new();

        public Task<bool> UserConnected(string username, string connectionId)
        {
            var isOnline = false;

            lock (_onlineUsers)
            {
                if (_onlineUsers.ContainsKey(username))
                {
                    _onlineUsers[username].Add(connectionId);
                }
                else
                {
                    _onlineUsers.Add(username, new List<string> { connectionId });
                    isOnline = true;
                }
            }

            return Task.FromResult(isOnline);
        }

        public Task<bool> UserDisconnected(string username, string connectionId)
        {
            var isOffline = false;

            lock (_onlineUsers)
            {
                if (_onlineUsers.ContainsKey(username))
                {
                    _onlineUsers[username].Remove(connectionId);

                    if (_onlineUsers[username].Count == 0)
                    {
                        _onlineUsers.Remove(username);
                        isOffline = true;
                    }
                }
            }

            return Task.FromResult(isOffline);
        }

        public Task<string[]> GetOnlineUsers()
        {
            List<string> onlineUsers = new();

            lock (_onlineUsers)
            {
                onlineUsers = _onlineUsers.Keys.OrderBy(k => k).ToList();
            }

            return Task.FromResult(onlineUsers.ToArray());
        }

        public Task<List<string>> GetConnectionForUser(string username)
        {
            List<string> connectionIds;

            lock (_onlineUsers)
            {
                connectionIds = _onlineUsers.GetValueOrDefault(username);
            }

            return Task.FromResult(connectionIds);
        }
    }
}