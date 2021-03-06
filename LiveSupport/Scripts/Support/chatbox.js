﻿var entityMap = {
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': '&quot;',
    "'": '&#39;',
    "/": '&#x2F;'
};

function escapeHtml(string) {
    return String(string).replace(/[&<>"'\/]/g, function (s) {
        return entityMap[s];
    });
}

function scrollToBottom() {
    var target = $('html,body');
    target.animate({ scrollTop: target.height() }, 500);
}

function sendMessage(isAgent, connectionId) {

    function send() {
        var message = $('.post-message:visible').val();
        $('.post-message:visible').val('').focus();
        if (message != "") {
            $.connection.chatHub.server.sendMessage(escapeHtml(message), isAgent, connectionId);
        }
    }

    $('.send-button:visible').click(function () {
        send();
    });

    $('.post-message:visible').keypress(function (e) {
        if (e.keyCode == 13) {
            e.preventDefault();
            e.stopPropagation();
            send();
        }
    })
}
