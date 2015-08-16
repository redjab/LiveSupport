using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LiveSupport.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, User> Agents;

        //visitor & agent?
        private static ConcurrentDictionary<User, User> ChatSessions;
        private const string FROM_ADMIN = "Administrator";
        private const string AGENT_DISCONNECT = "The agent was disconnected from chat. Please reopen the page if you want to talk to another agent.";
        private const string USER_DISCONNECT = "The visitor was disconnected from chat.";

        private void initAgents(){
            if (Agents == null){
                Agents = new ConcurrentDictionary<string,User>();
            }
        }

        private void initSessions()
        {
            if (ChatSessions == null)
            {
                ChatSessions = new ConcurrentDictionary<User, User>();
            }
        }

        public void ConnectAgent()
        {
            initAgents();
            var currentUser = HttpContext.Current.User;
            var userName = currentUser.Identity.Name;
            if (!Agents.Any(x => x.Key == userName))
            {
                var User = new User{
                    ConnectionID = Context.ConnectionId,
                    UserName = userName,
                };
                Agents.TryAdd(userName, User);
            }
        }
        private User RemoveAgentSession(string connectionId, string message, bool removeAgent)
        {
            User temp = null;

            var agent = Agents.SingleOrDefault(x => x.Value.ConnectionID == connectionId).Value;
            if (agent != null)
            {
                var sessions = ChatSessions.Where(x => x.Value == agent);
                if (sessions != null)
                {
                    foreach (var session in sessions)
                    {
                        //TODO: Notify the user?
                        Clients.Client(session.Key.ConnectionID).addMessage(FROM_ADMIN, AGENT_DISCONNECT);
                        ChatSessions.TryRemove(session.Key, out temp);
                    }
                }
                if (removeAgent)
                {
                    Agents.TryRemove(agent.UserName, out temp);
                }
            }
            return agent;
        }

        public void AgentCloseChat(string connectionId, string message)
        {
            RemoveAgentSession(connectionId, message, false);
        }

        public void Leave(string connectionId, string message)
        {
            initAgents();
            initSessions();
            //if it's an agent
            var agent = RemoveAgentSession(connectionId, message, true);
            var sessions = ChatSessions.Where(x => x.Key.ConnectionID == connectionId);
            foreach (var session in sessions)
            {
                User temp = null;
                //TODO: Notify the agent?
                Clients.Client(session.Key.ConnectionID).addMessage(FROM_ADMIN, USER_DISCONNECT);
                ChatSessions.TryRemove(session.Key, out temp);
            }
        }

        public void StartChat(string message)
        {
            //get the agent with the fewest chat sessionss
            var inSessions = Agents.Select(session => new
            {
                AgentUserName = session.Value.UserName,
                SessionCount = ChatSessions.Count(x => x.Value == session.Value)
            }).OrderBy(x => x.SessionCount);

            if (inSessions != null)
            {
                var leastBusy = Agents.Where(x => x.Key == inSessions.FirstOrDefault().AgentUserName).FirstOrDefault();
                var agent = leastBusy.Value;
                var visitor = new User
                {
                    ConnectionID = Context.ConnectionId,
                    UserName = HttpContext.Current.User.Identity.Name
                };
                ChatSessions.TryAdd(visitor, agent);
                //TODO: Notify the agent about the new chat request
            }
           
        }

    }
}