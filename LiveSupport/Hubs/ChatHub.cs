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
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace LiveSupport.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, User> Agents;

        //visitor & agent?
        private static ConcurrentDictionary<User, User> ChatSessions;

        private const string FROM_ADMIN = "Administrator";
        private const string NO_AGENT = "No agent is currently online. Please try again later.";
        private const string AGENT_DISCONNECT = "The agent was disconnected from chat. Please reopen the page if you want to talk to another agent.";
        private const string USER_DISCONNECT = "The visitor was disconnected from chat.";
        private const string SESSION_NOT_FOUND = "Chat session not found, please reload the page.";

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

        private string getFullName()
        {
            var currentUser = HttpContext.Current.User;
            var identity = (ClaimsIdentity)currentUser.Identity;
            var fullName = identity.FindFirst("FullName").Value;
            return fullName;
        }

        public override Task OnConnected()
        {
            initAgents();
            initSessions();
            return base.OnConnected();
        }

        public void ConnectAgent()
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var fullName = getFullName();

            var agent = Agents.SingleOrDefault(x => x.Key == userName);
            if (agent.Key == null)
            {
                var newAgent = new User
                {
                    ConnectionID = Context.ConnectionId,
                    UserName = userName,
                    FullName = fullName,
                };
                Agents.TryAdd(userName, newAgent);
            }
            //this happens if the agent opens a new tab of the support page without closing the other
            //remove all sessions with previous connection id, and update the Agents dict
            else
            {
                RemoveAgentSession(Agents[userName].ConnectionID, false);
                Agents[userName].ConnectionID = Context.ConnectionId;
            }
        }
        private void RemoveAgentSession(string connectionId, bool removeAgent)
        {
            User temp = null;

            var agent = Agents.SingleOrDefault(x => x.Value.ConnectionID == connectionId).Value;
            if (agent != null)
            {
                var sessions = ChatSessions.Where(x => x.Value.UserName == agent.UserName);
                if (sessions != null)
                {
                    foreach (var session in sessions)
                    {
                        //Notify the user
                        Clients.Client(session.Key.ConnectionID).addMessage(FROM_ADMIN, AGENT_DISCONNECT);
                        ChatSessions.TryRemove(session.Key, out temp);
                    }
                }
                if (removeAgent)
                {
                    Agents.TryRemove(agent.UserName, out temp);
                }
            }
        }

        public void AgentCloseChat(string connectionId)
        {
            RemoveAgentSession(connectionId, false);
        }

        public void Leave(string connectionId)
        {
            //if it's an agent
            RemoveAgentSession(connectionId, true);
            var sessions = ChatSessions.Where(x => x.Key.ConnectionID == connectionId);
            foreach (var session in sessions)
            {
                User temp = null;
                //Notify the agent?
                Clients.Client(session.Value.ConnectionID).addMessage(FROM_ADMIN, USER_DISCONNECT, connectionId);
                ChatSessions.TryRemove(session.Key, out temp);
            }
        }

        public void StartChat()
        {
            //Let the user know if no agent is online
            if (Agents == null || Agents.Count == 0)
            {
                Clients.Caller.addMessage(FROM_ADMIN, NO_AGENT);
                return;
            }
            //get the agent with the fewest chat sessionss
            var inSessions = Agents.Select(session => new
            {
                AgentUserName = session.Value.UserName,
                SessionCount = ChatSessions.Count(x => x.Value == session.Value)
            }).OrderBy(x => x.SessionCount);

            if (inSessions != null)
            {
                var leastBusy = Agents.SingleOrDefault(x => x.Key == inSessions.FirstOrDefault().AgentUserName);
                var agent = leastBusy.Value;
                var visitor = new User
                {
                    ConnectionID = Context.ConnectionId,
                    UserName = HttpContext.Current.User.Identity.Name,
                    FullName = getFullName(),
                };
                ChatSessions.TryAdd(visitor, agent);

                //Notify the agent about the new chat request
                Clients.Client(agent.ConnectionID).newChat(visitor.ConnectionID, visitor.FullName);

                var agentMessage = string.Format("You are now chatting with {0}", visitor.FullName);
                var visitorMessage = string.Format("You are now chatting with {0}", agent.FullName);
                Clients.Client(agent.ConnectionID).addMessage(FROM_ADMIN, agentMessage, Context.ConnectionId);
                Clients.Caller.addMessage(FROM_ADMIN, visitorMessage);
            }
           
        }

        public void SendMessage(string message, bool isAgent, string connectionId)
        {
            KeyValuePair<User, User> session;
            if (!isAgent)
            {
                session = UserSendMessage(message);
            }
            else
            {
                session = AgentSendMessage(message, connectionId);
            }
            if (session.Key == null || session.Value == null)
            {
                Clients.Caller.addMessage(FROM_ADMIN, SESSION_NOT_FOUND);
            }
        }

        private KeyValuePair<User, User> AgentSendMessage(string message, string connectionId)
        {
            //send in the connectionId since the key of ChatSessions is the user's connection id, not the agent's
            //and the context connectionId here is the agent's
            var session = ChatSessions.SingleOrDefault(x => x.Key.ConnectionID == connectionId);
            if (session.Key != null && session.Value != null)
            {
                Clients.Client(session.Key.ConnectionID).addMessage(session.Value.FullName, message);
                Clients.Client(session.Value.ConnectionID).addMessage(session.Value.FullName, message, session.Key.ConnectionID);
            }
            return session;
        }

        private KeyValuePair<User, User> UserSendMessage(string message)
        {
            var session = ChatSessions.SingleOrDefault(x => x.Key.ConnectionID == Context.ConnectionId);
            if (session.Key != null && session.Value != null)
            {
                Clients.Client(session.Key.ConnectionID).addMessage(session.Key.FullName, message);
                Clients.Client(session.Value.ConnectionID).addMessage(session.Key.FullName, message, session.Key.ConnectionID);
            }
            return session;
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Leave(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }
    }
}