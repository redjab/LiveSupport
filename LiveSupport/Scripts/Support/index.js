$(function () {
    registerClientFunctions();
    startHub();
})
hub = $.connection.chatHub;
function startHub() {
    $.connection.hub.start()
    .done(function () {
        hub.server.startChat();
    })
    .fail(function () {
        alert("Unable to connect. Please reload the page.")
    })

    sendMessage(false);
}

function registerClientFunctions() {
    hub.client.addMessage = function (from, message) {
        var date = new Date();

        var conversationHtml = '<li class="other"> <div class="name"> <p>' +
            from + '</p> </div> <div class="messages"> <p>' +
            message + '</p><abbr class="timeago" title="' + date.toISOString() + '">' + date.toISOString() + '</abbr>'
        ;

        $(".discussion").append(conversationHtml);
        $(".discussion").find('abbr.timeago').timeago();
    }
}
