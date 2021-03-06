var Messages;
(function (Messages) {
    Messages.IS_BOTTOM_MARGIN = 15;
    Messages.IS_TOP_MARGIN = 4;
    Messages.LOAD_OLDER_MARGIN = 300;
    Messages.LOAD_NEWER_MARGIN = 300;
    Messages.HIDE_CONTENT_MARGIN = 60;
    Messages.LOG_VIEWPORT = false;
    Messages.IS_DESKTOP = document.documentElement.classList.contains("html-desktop");
    Messages.IS_TABLET = document.documentElement.classList.contains("html-tablet");
    if (!Messages.IS_DESKTOP && !Messages.IS_TABLET) {
        document.documentElement.classList.add("html-phone");
    }
})(Messages || (Messages = {}));
var Messages;
(function (Messages) {
    function _padLeft(i, l) {
        var offset = l == 1 ? 10 : l == 2 ? 100 : Math.pow(10, l);
        if (i > offset) {
            var s = i.toFixed(0);
            return s.substr(s.length - l, l);
        }
        return (i + offset).toFixed(0).substr(1, l);
    }
    function _createFAAnchor(url, faClass, disabled) {
        var a = document.createElement("a");
        if (disabled) {
            a.href = "javascript:void(0)";
            a.classList.add("disabled");
        }
        else {
            a.href = url;
        }
        a.appendChild(_createFA(faClass));
        return a;
    }
    function _createFA(className) {
        var r = document.createElement("i");
        r.classList.add("fa");
        r.classList.add(className);
        return r;
    }
    function createTaklElement(m) {
        var id = m.Id;
        var talk = document.createElement("div");
        talk.classList.add("talk");
        talk.classList.add(m.IsMerged ? "continued" : "not-continued");
        if (id) {
            talk.id = "talk" + id;
            talk.setAttribute("data-message-id", id.toString());
            if (!m.IsDeleted) {
                if (Messages.IS_DESKTOP || Messages.IS_TABLET) {
                    var control = document.createElement("div");
                    control.classList.add("message-menu");
                    control.appendChild(_createFAAnchor("http://kokoro.io/client/control?event=replyToMessage&id=" + m.Id, "fa-reply"));
                    control.appendChild(_createFAAnchor("http://kokoro.io/client/control?event=copyMessage&id=" + m.Id, "fa-clipboard"));
                    control.appendChild(_createFAAnchor("http://kokoro.io/client/control?event=deleteMessage&id=" + m.Id, "fa-trash", !m.CanDelete));
                    talk.appendChild(control);
                }
                else {
                    var control = document.createElement("div");
                    control.classList.add("message-menu");
                    control.appendChild(_createFAAnchor("http://kokoro.io/client/control?event=messageMenu&id=" + m.Id, "fa-chevron-down"));
                    talk.appendChild(control);
                }
            }
        }
        else {
            var idempotentKey = m.IdempotentKey;
            if (idempotentKey) {
                talk.setAttribute("data-idempotent-key", idempotentKey);
            }
        }
        try {
            var avatar = document.createElement("div");
            avatar.classList.add("avatar");
            talk.appendChild(avatar);
            var profUrl = "https://kokoro.io/profiles/" + m.ProfileId;
            var imgLink = document.createElement("a");
            imgLink.href = profUrl;
            imgLink.classList.add("img-rounded");
            avatar.appendChild(imgLink);
            var img = document.createElement("img");
            img.src = m.Avatar;
            imgLink.appendChild(img);
            var message = document.createElement("div");
            message.classList.add("message");
            talk.appendChild(message);
            var speaker = document.createElement("div");
            speaker.classList.add("speaker");
            message.appendChild(speaker);
            var name = document.createElement("a");
            name.innerText = m.DisplayName;
            name.href = profUrl;
            speaker.appendChild(name);
            if (m.IsBot) {
                var small = document.createElement("small");
                small.className = "label label-default";
                small.innerText = "bot";
                speaker.appendChild(small);
            }
            var small = document.createElement("small");
            small.classList.add("timeleft", "text-muted");
            if (m.PublishedAt) {
                try {
                    var d = new Date(Date.parse(m.PublishedAt));
                    small.innerText = (d.getFullYear() == new Date().getFullYear() ? '' : _padLeft(d.getFullYear(), 4) + '/')
                        + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2);
                    small.title = _padLeft(d.getFullYear(), 4)
                        + '/' + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2)
                        + ':' + _padLeft(d.getSeconds(), 2);
                }
                catch (ex) {
                    small.innerText = m.PublishedAt;
                }
            }
            else {
                small.innerText = "Now sending...";
            }
            speaker.appendChild(small);
            var filteredText = document.createElement("div");
            filteredText.classList.add(m.IsDeleted ? "deleted_text" : "filtered_text");
            filteredText.innerHTML = m.HtmlContent;
            message.appendChild(filteredText);
            if (m.EmbedContents && m.EmbedContents.length > 0) {
                var ecs = document.createElement("div");
                ecs.classList.add("embed_contents");
                message.appendChild(ecs);
                var d = void 0;
                try {
                    for (var i = 0; i < m.EmbedContents.length; i++) {
                        var e = m.EmbedContents[i];
                        if (!e) {
                            continue;
                        }
                        d = e.data;
                        if (!d) {
                            continue;
                        }
                        var ec = document.createElement("div");
                        ec.classList.add("embed_content");
                        ecs.appendChild(ec);
                        switch (d.type) {
                            case 'MixedContent':
                                ec.appendChild(_createEmbedContent(m, d, false));
                                break;
                            case 'SingleImage':
                            case 'SingleVideo':
                            case 'SingleAudio':
                                ec.appendChild(_createEmbedContent(m, d, true));
                                break;
                            default:
                                console.warn("Unknown embed data: ", d);
                                break;
                        }
                    }
                }
                catch (ex) {
                    ecs.innerHTML = "";
                    var err = document.createElement('p');
                    err.innerText = ex;
                    ecs.appendChild(err);
                    var json = document.createElement('pre');
                    json.innerText = JSON.stringify(d);
                    ecs.appendChild(json);
                }
            }
        }
        catch (ex) {
            talk.innerText = ex;
        }
        return talk;
    }
    Messages.createTaklElement = createTaklElement;
    function _createEmbedContent(message, d, hideInfo) {
        var r = document.createElement("div");
        r.classList.add("embed-" + d.type.toLowerCase());
        if (!hideInfo) {
            var meta = document.createElement("div");
            meta.classList.add("meta");
            r.appendChild(meta);
            if (d.metadata_image) {
                var m = d.metadata_image;
                var thd = _createMediaDiv(m, d, message, "embed-thumbnail");
                if (thd) {
                    var thumb = document.createElement("div");
                    thumb.classList.add("thumb");
                    meta.appendChild(thumb);
                    thumb.appendChild(thd);
                }
            }
            var info = document.createElement("div");
            info.classList.add("info");
            meta.appendChild(info);
            if (d.title) {
                var titleDiv = document.createElement("div");
                titleDiv.classList.add("title");
                info.appendChild(titleDiv);
                var titleLink = document.createElement("a");
                titleLink.href = d.url;
                titleDiv.appendChild(titleLink);
                var title = document.createElement("strong");
                title.innerText = d.title;
                titleLink.appendChild(title);
            }
            if (d.description) {
                var descriptionDiv = document.createElement("div");
                descriptionDiv.classList.add("description");
                info.appendChild(descriptionDiv);
                var description = document.createElement("p");
                var re = /(https?:\/\/[a-z0-9]+(?:[-.][a-z0-9]+)*(?:\/|[!$&-;=?-Z\\^_a-~]|%[A-F0-9]{2})*)/;
                var ary = d.description.split(re);
                if (ary && ary.length > 1) {
                    for (var i_1 = 0; i_1 < ary.length; i_1++) {
                        var t = ary[i_1];
                        if (i_1 % 2 == 0) {
                            description.appendChild(document.createTextNode(t));
                        }
                        else {
                            var a = document.createElement("a");
                            a.setAttribute("href", t);
                            a.appendChild(document.createTextNode(t));
                            description.appendChild(a);
                        }
                    }
                }
                else {
                    description.innerText = d.description;
                }
                descriptionDiv.appendChild(description);
            }
        }
        if (d.medias && d.medias.length > 0) {
            var medias = document.createElement("div");
            medias.classList.add("medias");
            for (var i = 0; i < d.medias.length; i++) {
                var m = d.medias[i];
                if (m) {
                    var md = _createMediaDiv(m, d, message);
                    if (md) {
                        medias.appendChild(md);
                    }
                }
            }
            if (medias.children.length > 0) {
                r.appendChild(medias);
            }
        }
        return r;
    }
    function _createMediaDiv(media, data, message, className) {
        var tu = (media.thumbnail ? media.thumbnail.url : null) || media.raw_url;
        if (!tu) {
            return null;
        }
        var em = document.createElement("div");
        em.classList.add(className || "embed_media");
        var a = document.createElement("a");
        a.href = media.location || media.raw_url || data.url;
        em.appendChild(a);
        var img = document.createElement("img");
        img.classList.add("img-rounded");
        img.src = tu;
        a.appendChild(img);
        var policies = [(message.IsNsfw ? "Restricted" : "Unknown"), media.restriction_policy, data.restriction_policy];
        if (policies.filter(function (p) { return p !== "Unknown"; })[0] === "Restricted") {
            em.classList.add("nsfw");
            var i = document.createElement("i");
            i.className = "nsfw-mark fa fa-exclamation-triangle";
            em.appendChild(i);
        }
        return em;
    }
})(Messages || (Messages = {}));
var Messages;
(function (Messages) {
    var _hasUnread = false;
    var _isUpdating = false;
    var displayed;
    function HOST() {
        return document.body;
    }
    window.setHasUnread = function (value) {
        _hasUnread = !!value;
    };
    window.setMessages = function (messages) {
        _isUpdating = true;
        try {
            var b = HOST();
            console.debug("Setting " + (messages ? messages.length : 0) + " messages");
            if (!messages || messages.length === 0) {
                displayed = null;
            }
            b.innerHTML = "";
            addMessagesCore(messages, null, false);
            window.scrollTo(0, b.scrollHeight - b.clientHeight);
            _reportVisibilities();
        }
        finally {
            _isUpdating = false;
        }
    };
    window.addMessages = function (messages, merged, showNewMessage) {
        _isUpdating = true;
        try {
            var b = HOST();
            console.debug("Adding " + (messages ? messages.length : 0) + " messages");
            var isEmpty = b.children.length === 0;
            showNewMessage = showNewMessage && !isEmpty;
            addMessagesCore(messages, merged, !showNewMessage && !isEmpty);
            if (isEmpty) {
                window.scrollTo(0, b.scrollHeight - b.clientHeight);
            }
            else if (showNewMessage && messages && messages.length > 0) {
                var minId = Number.MAX_VALUE;
                messages.forEach(function (v) { return minId = Math.min(minId, v.Id); });
                var talk = document.getElementById("talk" + minId);
                if (talk) {
                    _bringToTop(talk);
                }
            }
            _reportVisibilities();
        }
        finally {
            _isUpdating = false;
        }
    };
    var removeMessages = window.removeMessages = function (ids, idempotentKeys, merged) {
        _isUpdating = true;
        try {
            console.debug("Removing " + ((ids ? ids.length : 0) + (idempotentKeys ? idempotentKeys.length : 0)) + " messages");
            var b = HOST();
            if (ids) {
                for (var i = 0; i < ids.length; i++) {
                    var talk = document.getElementById('talk' + ids[i]);
                    if (talk) {
                        var nt = talk.offsetTop < window.scrollY ? window.scrollY - talk.clientHeight : window.scrollY;
                        talk.remove();
                        window.scrollTo(0, nt);
                    }
                }
            }
            if (idempotentKeys) {
                for (var i_2 = 0; i_2 < idempotentKeys.length; i_2++) {
                    var talk_1 = _talkByIdempotentKey(idempotentKeys[i_2]);
                    if (talk_1) {
                        var nt = talk_1.offsetTop < window.scrollY ? window.scrollY - talk_1.clientHeight : window.scrollY;
                        talk_1.remove();
                        window.scrollTo(0, nt);
                    }
                }
            }
            updateContinued(merged, true);
            _reportVisibilities();
        }
        finally {
            _isUpdating = false;
        }
    };
    function addMessagesCore(messages, merged, scroll) {
        var b = HOST();
        var lastTalk = scroll && window.scrollY + b.clientHeight + Messages.IS_BOTTOM_MARGIN > b.scrollHeight ? b.lastElementChild : null;
        scroll = scroll && !lastTalk;
        if (messages) {
            var j = 0;
            for (var i = 0; i < messages.length; i++) {
                var m = messages[i];
                var id = m.Id;
                if (!id) {
                    var talk = Messages.createTaklElement(m);
                    b.appendChild(talk);
                    _afterTalkInserted(talk);
                    continue;
                }
                else {
                    var cur = document.getElementById("talk" + id)
                        || (m.IdempotentKey ? _talkByIdempotentKey(m.IdempotentKey) : null);
                    if (cur) {
                        var shoudScroll = scroll && cur.offsetTop + cur.clientHeight - Messages.IS_TOP_MARGIN < window.scrollY;
                        var st = window.scrollY - cur.clientHeight;
                        var talk = Messages.createTaklElement(m);
                        b.insertBefore(talk, cur);
                        _afterTalkInserted(talk, cur.clientHeight);
                        cur.remove();
                        if (scroll) {
                            window.scrollTo(0, st + talk.clientHeight);
                        }
                        continue;
                    }
                }
                for (;;) {
                    var prev = b.children[j];
                    var aft = b.children[j + 1];
                    var pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    var aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;
                    if (!prev || (id != pid && !aft)) {
                        var talk = Messages.createTaklElement(m);
                        b.appendChild(talk);
                        _afterTalkInserted(talk);
                        j++;
                        break;
                    }
                    else if (id <= pid) {
                        var talk = Messages.createTaklElement(m);
                        if (id == pid) {
                            var shoudScroll = scroll && aft && aft.offsetTop - Messages.IS_TOP_MARGIN < window.scrollY;
                            var st = window.scrollY - prev.clientHeight;
                            b.insertBefore(talk, prev);
                            _afterTalkInserted(talk, prev.clientHeight);
                            prev.remove();
                            if (scroll) {
                                window.scrollTo(0, st + talk.clientHeight);
                            }
                        }
                        else {
                            _insertBefore(talk, prev, scroll);
                            j++;
                        }
                        break;
                    }
                    else if (id < aid) {
                        var talk = Messages.createTaklElement(m);
                        _insertBefore(talk, aft, scroll);
                        j++;
                        break;
                    }
                    else {
                        j++;
                    }
                }
            }
        }
        updateContinued(merged, scroll);
        if (lastTalk) {
            _bringToTop(lastTalk);
        }
    }
    window.showMessage = function (id, toTop) {
        _isUpdating = true;
        try {
            console.debug("showing message[" + id + "]");
            var talk = document.getElementById("talk" + id);
            if (talk) {
                var b = HOST();
                console.log("current scrollTo is " + window.scrollY + ", and offsetTop is " + talk.offsetTop);
                if (talk.offsetTop < window.scrollY || toTop) {
                    console.log("scrolling to " + talk.offsetTop);
                    window.scrollTo(0, talk.offsetTop);
                }
                else if (window.scrollY + b.clientHeight < talk.offsetTop - talk.clientHeight) {
                    console.log("scrolling to " + (talk.offsetTop - b.clientHeight));
                    window.scrollTo(0, talk.offsetTop - b.clientHeight);
                }
            }
        }
        finally {
            _isUpdating = false;
        }
    };
    function updateContinued(merged, scroll) {
        if (merged) {
            var b = HOST();
            for (var i = 0; i < merged.length; i++) {
                var m = merged[i];
                var id = m.Id;
                var isMerged = m.IsMerged;
                var talk = document.getElementById('talk' + id);
                if (talk) {
                    var shouldScroll = scroll && talk.offsetTop - Messages.IS_TOP_MARGIN < window.scrollY;
                    var bt = window.scrollY - talk.clientHeight;
                    talk.classList.remove(!isMerged ? "continued" : "not-continued");
                    talk.classList.add(isMerged ? "continued" : "not-continued");
                    if (shouldScroll) {
                        window.scrollTo(0, bt + talk.clientHeight);
                    }
                }
            }
        }
    }
    function _insertBefore(talk, aft, scroll) {
        var b = HOST();
        scroll = scroll && aft.offsetTop - Messages.IS_TOP_MARGIN < window.scrollY;
        var st = window.scrollY;
        b.insertBefore(talk, aft);
        _afterTalkInserted(talk);
        if (scroll) {
            window.scrollTo(0, st + talk.clientHeight);
        }
    }
    function _bringToTop(talk) {
        if (talk) {
            if (talk.offsetTop === 0) {
                if (talk.previousElementSibling) {
                    setTimeout(function () {
                        var b = HOST();
                        window.scrollTo(0, talk.offsetTop);
                    }, 1);
                }
            }
            else {
                var b = HOST();
                window.scrollTo(0, talk.offsetTop);
            }
        }
    }
    function _afterTalkInserted(talk, previousHeight) {
        var b = HOST();
        if (talk.offsetTop < window.scrollY) {
            var delta = talk.clientHeight - (previousHeight || 0);
            if (delta != 0) {
                window.scrollBy(0, delta);
                console.log("scolled " + delta);
            }
        }
        talk.setAttribute("data-height", talk.clientHeight.toString());
        var anchors = talk.getElementsByTagName("a");
        for (var i = 0; i < anchors.length; i++) {
            var a = anchors[i];
            if (/^javascript:/.test(a.href) && !/^javascript:void\(0?\);?/.test(a.href)) {
                console.warn("unsupported scheme: " + a.href);
                a.href = '#';
            }
            else if (/^https:\/\/kokoro\.io\/#\/channels\/([A-Za-z0-9]{9})$/.test(a.href)) {
                a.href = 'https://kokoro.io/channels/' + RegExp.$1;
            }
            a.removeAttribute("target");
        }
        var imgs = talk.getElementsByTagName("img");
        talk.setAttribute("data-loading-images", imgs.length.toString());
        var handler;
        handler = function (e) {
            var img = e.target;
            var talk = img.parentElement;
            while (talk) {
                if (talk.classList.contains("talk")) {
                    talk.setAttribute("data-loading-images", (Math.max(0, (parseInt(talk.getAttribute("data-loading-images"), 10) - 1) || 0)).toString());
                    var ph = parseInt(talk.getAttribute("data-height"), 10);
                    var delta = talk.clientHeight - ph;
                    var b_1 = HOST();
                    if (window.scrollY + b_1.clientHeight + Messages.IS_BOTTOM_MARGIN > b_1.scrollHeight - delta) {
                        window.scrollTo(0, b_1.scrollHeight - b_1.clientHeight);
                    }
                    else if (talk.offsetTop < window.scrollY) {
                        window.scrollBy(0, delta);
                    }
                    talk.setAttribute("data-height", talk.clientHeight.toString());
                    break;
                }
                else if (/^error$/i.test(e.type) && talk.classList.contains("embed_media")) {
                    var tp = talk.parentElement;
                    talk.remove();
                    if (tp.children.length === 0) {
                        tp.remove();
                    }
                    break;
                }
                else if (/^error$/i.test(e.type) && talk.classList.contains("thumb")) {
                    talk.remove();
                }
                talk = talk.parentElement;
            }
            img.removeEventListener("load", handler);
            img.removeEventListener("error", handler);
            _reportVisibilities();
        };
        for (var i = 0; i < imgs.length; i++) {
            imgs[i].addEventListener("load", handler);
            imgs[i].addEventListener("error", handler);
        }
    }
    function _talkByIdempotentKey(idempotentKey) {
        return document.querySelector('div.talk[data-idempotent-key=\"' + idempotentKey + "\"]");
    }
    var _visibleIds;
    function _reportVisibilities() {
        _determineDisplayedElements();
        var ids = displayed.map(function (e) { return e.getAttribute("data-message-id") || e.getAttribute("data-idempotent-key"); }).join(",");
        if (_visibleIds !== ids) {
            location.href = "http://kokoro.io/client/control?event=visibility&ids=" + ids;
            _visibleIds = ids;
            if (Messages.LOG_VIEWPORT) {
                var b = HOST();
                console.log("visibility changed: scrollY: " + window.scrollY
                    + (", clientHeight: " + b.clientHeight)
                    + (", lastElementChild.offsetTop: " + (b.lastElementChild ? b.lastElementChild.offsetTop : -1))
                    + (", lastElementChild.clientHeight: " + (b.lastElementChild ? b.lastElementChild.clientHeight : -1)));
            }
            _visibleIds = ids;
            return true;
        }
        return false;
    }
    function _isAbove(talk, b) {
        return talk.offsetTop + talk.clientHeight + Messages.HIDE_CONTENT_MARGIN < window.scrollY;
    }
    function _isBelow(talk, b) {
        return window.scrollY + b.clientHeight < talk.offsetTop - Messages.HIDE_CONTENT_MARGIN;
    }
    function _hideTalk(talk) {
        if (!talk.classList.contains("hidden")) {
            talk.style.height = talk.clientHeight.toString() + 'px';
            talk.classList.add("hidden");
        }
    }
    function _showTalk(talk) {
        if (talk.classList.contains("hidden")) {
            talk.style.height = null;
            talk.classList.remove("hidden");
        }
    }
    function _determineDisplayedElements() {
        var b = HOST();
        var displaying;
        if (displayed && displayed.length > 0) {
            for (var _i = 0, displayed_1 = displayed; _i < displayed_1.length; _i++) {
                var talk = displayed_1[_i];
                if (!talk.parentElement) {
                    continue;
                }
                if (_isAbove(talk, b)) {
                    continue;
                }
                else if (_isBelow(talk, b)) {
                    break;
                }
                else {
                    displaying = [];
                    if (displayed[0] === talk) {
                        for (var n = talk.previousSibling; n; n = n.previousSibling) {
                            var t = n;
                            if (t.nodeType === Node.ELEMENT_NODE) {
                                if (_isAbove(t, b)) {
                                    break;
                                }
                                displaying.unshift(t);
                            }
                        }
                    }
                    displaying.push(talk);
                    for (var n = talk.nextSibling; n; n = n.nextSibling) {
                        var t = n;
                        if (t.nodeType === Node.ELEMENT_NODE) {
                            if (_isBelow(t, b)) {
                                break;
                            }
                            displaying.push(t);
                        }
                    }
                    break;
                }
            }
        }
        if (displayed && displaying) {
            for (var _a = 0, displayed_2 = displayed; _a < displayed_2.length; _a++) {
                var talk = displayed_2[_a];
                if (displaying.indexOf(talk) < 0
                    && !(parseInt(talk.getAttribute("data-loading-images"), 10) > 0)) {
                    _hideTalk(talk);
                }
            }
            for (var _b = 0, displaying_1 = displaying; _b < displaying_1.length; _b++) {
                var talk = displaying_1[_b];
                _showTalk(talk);
            }
        }
        else {
            displaying = [];
            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                var visible = !_isAbove(talk, b) && !_isBelow(talk, b);
                var hidden = !visible && !(parseInt(talk.getAttribute("data-loading-images"), 10) > 0);
                if (hidden) {
                    _hideTalk(talk);
                }
                else {
                    _showTalk(talk);
                }
                if (visible) {
                    displaying.push(talk);
                }
            }
        }
        displayed = displaying;
    }
    document.addEventListener("DOMContentLoaded", function () {
        var windowWidth = window.innerWidth;
        window.addEventListener("resize", function () {
            if (window.innerWidth == windowWidth) {
                return;
            }
            windowWidth = window.innerWidth;
            var b = HOST();
            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                talk.setAttribute("data-height", talk.clientHeight.toString());
            }
            _reportVisibilities();
        });
        document.addEventListener("scroll", function () {
            var b = HOST();
            if (_reportVisibilities()) {
                if (b.scrollHeight < b.clientHeight) {
                    return;
                }
                if (window.scrollY < Messages.LOAD_OLDER_MARGIN) {
                    if (!_isUpdating) {
                        console.log("Loading older messages.");
                        location.href = "http://kokoro.io/client/control?event=prepend&count=" + b.children.length;
                    }
                }
                else {
                    var fromBottom = b.scrollHeight - window.scrollY - b.clientHeight;
                    if (fromBottom < 4 || (_hasUnread && fromBottom < Messages.LOAD_NEWER_MARGIN)) {
                        if (!_isUpdating) {
                            console.log("Loading newer messages.");
                            location.href = "http://kokoro.io/client/control?event=append&count=" + b.children.length;
                        }
                    }
                }
            }
        });
        var mouseDownStart = null;
        var hovered;
        document.body.addEventListener("mousedown", function (e) {
            if (e.button === 0) {
                if (hovered) {
                    hovered.classList.remove("message-hover");
                }
                hovered = e.currentTarget;
                while (hovered && !hovered.classList.contains("talk")) {
                    hovered = hovered.parentElement;
                }
                if (hovered) {
                    hovered.classList.add("message-hover");
                }
                var b = document.body;
                if (window.scrollY + b.clientHeight + 4 > b.scrollHeight) {
                    mouseDownStart = new Date().getTime();
                    setTimeout(function () {
                        if (mouseDownStart !== null
                            && mouseDownStart + 800 < new Date().getTime()) {
                            if (!_isUpdating) {
                                console.log("Loading newer messages.");
                                location.href = "http://kokoro.io/client/control?event=append&count=" + HOST().children.length;
                            }
                        }
                        mouseDownStart = null;
                    }, 1000);
                    return;
                }
            }
            mouseDownStart = null;
        });
        document.body.addEventListener("mouseup", function (e) {
            mouseDownStart = null;
        });
        document.body.addEventListener("mousemove", function (e) {
            if (hovered) {
                hovered.classList.remove("message-hover");
                hovered = null;
            }
        });
        document.body.addEventListener("wheel", function (e) {
            if (e.ctrlKey) {
                e.preventDefault();
                return;
            }
        });
        location.href = "http://kokoro.io/client/control?event=loaded";
    });
})(Messages || (Messages = {}));
