﻿var chatSessions = new Object();

$(function () {
    var chatboxHtml = $(".initial");

    $('#chat-sessions').on({
        click: function () {
            chatboxHtml.remove();
            var connectionId = $(this).data('id');
            $('.chat-box').hide();
            var chatSession = $("#chat-box" + connectionId);

            //if not exist already
            if (chatSession.size() == 0) {
                var chatBoxClone = chatboxHtml.clone();
                var id = "chat-box" + connectionId;
                chatBoxClone.attr('id', id);
                chatBoxClone.attr('data-id', connectionId);
                $(".chat-container").append(chatBoxClone);
            }
            else {
                chatSession.show();
            }
            for (var i = 0; i < chatSessions[connectionId].messages.length; i++) {
                $("#chat-box" + connectionId).find('.discussion').append(chatSessions[connectionId].messages[i]);
                $('#chat-box' + connectionId).find('abbr.timeago').timeago();
            }
            $.each(chatSessions, function (key, value) {
                value.isActive = false;
            });
            chatSessions[connectionId].messages = [];
            chatSessions[connectionId].isActive = true;

            chatSession.find('.discussion').append()

            $('.chat-session').removeClass('active');
            $(this).addClass('active');

            var badge = $(this).find('.badge');
            if (badge != null && badge != undefined) {
                badge.removeClass('badge-warning');
                badge.text('0');
            }

            $('.send-button').off('click');

            sendMessage(true);
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
    $("#chat" + connectionId).find('.badge').text(chatSessions[connectionId].messages.length);
}

function registerClientFunctions() {
    hub.client.addMessage = function (from, message, connectionId) {
        var date = new Date();

        var conversationHtml = '<li class="other"> <div class="name"> <p>' +
            from + '</p> </div> <div class="messages"> <p>' +
            message + '</p><abbr class="timeago" title="' + date.toISOString() + '">' + date.toISOString() + '</abbr>'
        ;

        //if this is a message to the agent, then we have to take care of different sessions he is handling
        if (connectionId != null) {
            var messages = [];
            if (chatSessions[connectionId] != null) {
                messages = chatSessions[connectionId].messages;
            } else {
                chatSessions[connectionId] = new Object();
                chatSessions[connectionId].messages = [];
                chatSessions[connectionId].isActive = false;
            }

            if (chatSessions[connectionId].isActive == true) {
                $(".discussion:visible").append(conversationHtml);
                $(".discussion:visible").find('abbr.timeago').timeago();
            }
            else {
                messages.push(conversationHtml);
                chatSessions[connectionId].isActive = false;
                chatSessions[connectionId].messages = messages;
            }
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
                            '<abbr class="timeago" title="' + date.toISOString() + '">' + date.toISOString() + '</abbr>' +
                            '<p><strong>' + fullName + '</strong></p></div>' +
                            '<div class="col-md-6" style="text-align: right;"><a class="close-chat btn btn-mini" href="#"><span class="glyphicon glyphicon-remove"></span></a>' +
                            '<p>Message(s) <span class="badge badge-warning">0</span></p></div>' +
                            '</div>';
        $("#chat-sessions").prepend(notifyHtml);
        $('#chat' + connectionId).find('abbr.timeago').timeago();
    }
}