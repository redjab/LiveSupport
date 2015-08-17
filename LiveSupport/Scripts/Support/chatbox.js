var entityMap = {
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


function sendMessage(isAgent) {
    function send() {
        var message = $('.post-message:visible').val();
        $('.post-message:visible').val('').focus();
        if (message != "") {
            $.connection.chatHub.server.sendMessage(escapeHtml(message), isAgent);
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
