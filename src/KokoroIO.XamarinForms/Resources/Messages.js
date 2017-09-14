﻿function setMessages(messages) {
    document.body.innerHTML = "";

    if (!messages) {
        return;
    }

    for (var i = 0; i < messages.length; i++) {
        var m = messages[i];

        var id = m.Id;
        var avatarUrl = m.Avatar;
        var displayName = m.DisplayName;
        var publishedAt = m.PublishedAt;
        var content = m.Content;
        var isMerged = m.IsMerged;

        var talk = document.createElement("div");
        talk.classList.add("talk");
        talk.classList.add(isMerged ? "continued" : "not-continued");
        talk.setAttribute("data-message-id", id);

        var avatar = document.createElement("div");
        avatar.classList.add("avatar");
        talk.appendChild(avatar);

        var imgLink = document.createElement("a");
        imgLink.classList.add("img-rounded");
        avatar.appendChild(imgLink);

        var img = document.createElement("img");
        img.src = avatarUrl;
        imgLink.appendChild(img);

        var message = document.createElement("div");
        message.classList.add("message");
        talk.appendChild(message);

        var speaker = document.createElement("div");
        speaker.classList.add("speaker");
        message.appendChild(speaker);

        var name = document.createElement("a");
        name.innerText = displayName;
        speaker.appendChild(name);

        var small = document.createElement("small");
        small.classList.add("timeleft", "text-muted");
        small.innerText = publishedAt;
        speaker.appendChild(small);

        var filteredText = document.createElement("div");
        filteredText.classList.add("filtered_text");
        filteredText.innerHTML = content;
        message.appendChild(filteredText);

        document.body.appendChild(talk);
    }
}