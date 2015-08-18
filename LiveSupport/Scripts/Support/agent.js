var chatSessions = new Object();
var chatboxHtml = $(".initial");

$(function () {

    $('#chat-sessions').on({
        click: function () {
            chatboxHtml.remove();
            var connectionId = $(this).data('id');
            $('.chat-box').hide();
            var chatSession = $("#chat-box" + connectionId);
            chatSession.show();

            $('.chat-session').removeClass('active');
            $(this).addClass('active');
            $('.new-mes').removeClass('new-mes');

            var badge = $(this).find('.badge');
            if (badge != null && badge != undefined) {
                badge.removeClass('badge-warning');
                badge.text('0');
            }

            $('.send-button').off('click');

            sendMessage(true, connectionId);
        }
    }, '.chat-session');
    registerClientFunctions();
    startHub();
})
hub = $.connection.chatHub;

var chatMessages = [];

function startHub() {
    $.connection.hub.start()
    .done(function () {
        hub.server.connectAgent();
    })
    .fail(function () {
        alert("Unable to connect. Please reload the page.")
    })
}


function updateQuantity(connectionId) {
    var newChats = $("#chat-box" + connectionId).find('.discussion').children('.new-mes').length;
    var chatPanel = $("#chat" + connectionId);
    if (!chatPanel.hasClass('active')) {
        $("#chat" + connectionId).find('.badge').text(newChats);
    }
}

function addToChat(connectionId, html) {
    var chatSession = $("#chat-box" + connectionId);

    if (chatSession.size() == 0) {
        var chatBoxClone = chatboxHtml.clone();
        var id = "chat-box" + connectionId;
        chatBoxClone.attr('id', id);
        chatBoxClone.attr('data-id', connectionId);
        $(".chat-container").append(chatBoxClone);
        chatBoxClone.hide();
    }
    var discussionDom = $("#chat-box" + connectionId).find('.discussion')
    discussionDom.append(html);
    discussionDom.find('abbr.timeago').timeago();
}

function registerClientFunctions() {
    hub.client.addMessage = function (from, message, connectionId) {
        var date = new Date();

        var conversationHtml = '<li class="person new-mes"> <div class="name"> <p>' +
            from + '</p> </div> <div class="messages"> <p>' +
            message + '</p><abbr class="timeago" title="' + date.toISOString() + '">' + date.toISOString() + '</abbr>'
        ;

        //if this is a message to the agent, then we have to take care of different sessions he is handling
        if (connectionId != null) {
            addToChat(connectionId, conversationHtml);
        } else {
            $(".discussion").append(conversationHtml);
            $(".discussion").find('abbr.timeago').timeago();
        }
        updateQuantity(connectionId);
    }
    hub.client.newChat = function (connectionId, fullName) {
        var date = new Date();

        var snd = new Audio('../Content/Sounds/newchat.mp3');
        snd.play();

        var notifyHtml = '<div id="chat' + connectionId + '" class="row chat-session" data-id="' + connectionId + '">' +
                            '<div class="col-md-6">' + 
                            '<p><strong>' + fullName + '</strong></p>' +
                            '<abbr class="timeago" title="' + date.toISOString() + '">' + date.toISOString() + '</abbr></div>' +
                            '<div class="col-md-6" style="text-align: right;"><a class="close-chat btn btn-mini" href="#"><span class="glyphicon glyphicon-remove"></span></a>' +
                            '<p>Message(s) <span class="badge badge-warning">0</span></p></div>' +
                            '</div>';
        $("#chat-sessions").prepend(notifyHtml);
        $('#chat' + connectionId).find('abbr.timeago').timeago();
    }
}