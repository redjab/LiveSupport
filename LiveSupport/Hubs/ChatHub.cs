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

        public void ConnectAgent()
        {
            initAgents();
            var userName = HttpContext.Current.User.Identity.Name;
            var fullName = getFullName();

            var agent = Agents.Where(x => x.Key == userName).FirstOrDefault();
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
            initAgents();
            initSessions();
            //if it's an agent
            RemoveAgentSession(connectionId, true);
            var sessions = ChatSessions.Where(x => x.Key.ConnectionID == connectionId);
            foreach (var session in sessions)
            {
                User temp = null;
                //Notify the agent?
                Clients.Client(session.Value.ConnectionID).addMessage(FROM_ADMIN, USER_DISCONNECT);
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
            initSessions();
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
                    UserName = HttpContext.Current.User.Identity.Name,
                    FullName = getFullName(),
                };
                ChatSessions.TryAdd(visitor, agent);

                //Notify the agent about the new chat request
                Clients.Client(agent.ConnectionID).newChat(Context.ConnectionId, visitor.FullName);

                var agentMessage = string.Format("You are now chatting with {0}", visitor.FullName);
                var visitorMessage = string.Format("You are now chatting with {0}", agent.FullName);
                Clients.Client(agent.ConnectionID).addMessage(FROM_ADMIN, agentMessage);
                Clients.Caller.addMessage(FROM_ADMIN, visitorMessage);
            }
           
        }

        public void SendMessage(string message)
        {
            //snatch any url using regex pattern
            message = Regex.Replace(message, @"(\b(?:(?:(?:https?|ftp|file)://|www\.|ftp\.)[-A-Z0-9+&@#/%?=~_|$!:,.;]*[-A-Z0-9+&@#/%=~_|$]|((?:mailto:)?[A-Z0-9._%+-]+@[A-Z0-9._%-]+\.[A-Z]{2,6})\b)|""(?:(?:https?|ftp|file)://|www\.|ftp\.)[^""\r\n]+""|'(?:(?:https?|ftp|file)://|www\.|ftp\.)[^'\r\n]+')", "<a target='_blank' href='$1'>$1</a>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var session = ChatSessions.Where(x => x.Key.ConnectionID == Context.ConnectionId).FirstOrDefault();
            if (session.Key != null && session.Value != null)
            {
                Clients.Client(session.Key.ConnectionID).addMessage(session.Key.FullName, message);
                Clients.Client(session.Value.ConnectionID).addMessage(session.Value.FullName, message);
            }
            else
            {
                Clients.Caller.addMessage(FROM_ADMIN, SESSION_NOT_FOUND);
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Leave(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }
    }
}