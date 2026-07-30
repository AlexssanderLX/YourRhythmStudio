/* Floating landing soundtrack player. Audio is controlled by Root/Soundtrack. */
(function () {
    "use strict";

    var INITIAL_VOLUME = 0.2;
    var SCROLL_REVEAL = 500;

    var root = document.getElementById("soundtrack");
    var btn = document.getElementById("soundtrackBtn");
    var label = document.getElementById("stLabel");
    var audio = document.getElementById("soundtrackAudio");
    if (!root || !btn || !label || !audio) return;

    var tracks = [];
    var queue = [];
    var queueIndex = 0;
    var queueReady = false;
    var current = -1;
    var lastPlayed = null;
    var revealed = false;
    var activated = false;

    function setState(state) {
        root.classList.toggle("playing", state === "playing");
        if (state === "playing") {
            label.textContent = "Pausar musica";
            btn.setAttribute("aria-pressed", "true");
        } else if (state === "paused") {
            label.textContent = "Continuar musica";
            btn.setAttribute("aria-pressed", "false");
        } else {
            label.textContent = "Ativar trilha sonora";
            btn.setAttribute("aria-pressed", "false");
        }
    }

    function loadTracks() {
        return fetch("/api/landing/tracks", { headers: { "Accept": "application/json" } })
            .then(function (response) { return response.ok ? response.json() : []; })
            .then(function (data) {
                tracks = Array.isArray(data)
                    ? data.filter(function (track) { return track && track.url && track.title; })
                    : [];
                tracks = uniqueTracks(tracks);
                queue = [];
                queueIndex = 0;
                queueReady = false;
            })
            .catch(function () {
                tracks = [];
                queue = [];
                queueIndex = 0;
                queueReady = false;
            });
    }

    function reveal() {
        if (revealed) return;
        revealed = true;

        loadTracks().then(function () {
            if (tracks.length === 0) return;
            root.hidden = false;
            void root.offsetWidth;
            root.classList.add("show");
        });
    }

    function sameTrack(a, b) {
        if (!a || !b) return false;
        if (a.id != null && b.id != null) return a.id === b.id;
        return a.url === b.url;
    }

    function uniqueTracks(list) {
        return list.filter(function (track, index) {
            return list.findIndex(function (candidate) { return sameTrack(candidate, track); }) === index;
        });
    }

    function shuffle(list) {
        var copy = list.slice();
        for (var i = copy.length - 1; i > 0; i--) {
            var j = Math.floor(Math.random() * (i + 1));
            var tmp = copy[i];
            copy[i] = copy[j];
            copy[j] = tmp;
        }

        return copy;
    }

    function buildQueue() {
        queue = shuffle(tracks);
        queueIndex = 0;
        queueReady = true;

        if (queue.length > 1 && sameTrack(queue[0], lastPlayed)) {
            var first = queue.shift();
            queue.push(first);
        }
    }

    function nextTrack() {
        if (tracks.length === 0) return null;
        if (tracks.length === 1) return tracks[0];
        if (!queueReady) buildQueue();
        if (queue.length === 0) return null;

        if (queueIndex >= queue.length) {
            queueIndex = 0;
        }

        return queue[queueIndex++] || null;
    }

    function playTrack(track) {
        if (!track) return;
        current = tracks.findIndex(function (item) { return sameTrack(item, track); });
        lastPlayed = track;
        audio.src = track.url;
        audio.volume = INITIAL_VOLUME;

        var promise = audio.play();
        if (promise && typeof promise.then === "function") {
            promise.then(function () { setState("playing"); }).catch(handleTrackError);
        } else {
            setState("playing");
        }
    }

    function handleTrackError() {
        var failed = current >= 0 && current < tracks.length ? tracks[current] : lastPlayed;
        if (failed) {
            tracks = tracks.filter(function (track) { return !sameTrack(track, failed); });
            var previousQueueLength = queue.length;
            queue = queue.filter(function (track) { return !sameTrack(track, failed); });
            if (queue.length < previousQueueLength && queueIndex > 0) {
                queueIndex--;
            }
        }
        current = -1;
        lastPlayed = null;

        if (tracks.length > 0) {
            playTrack(nextTrack());
            return;
        }

        btn.disabled = true;
        root.classList.remove("playing");
        label.textContent = "Trilha indisponivel";
    }

    btn.addEventListener("click", function () {
        if (tracks.length === 0) return;

        if (!activated) {
            activated = true;
            playTrack(nextTrack());
            return;
        }

        if (audio.paused) {
            var promise = audio.play();
            if (promise && typeof promise.then === "function") {
                promise.then(function () { setState("playing"); }).catch(handleTrackError);
            } else {
                setState("playing");
            }
        } else {
            audio.pause();
            setState("paused");
        }
    });

    audio.addEventListener("ended", function () { playTrack(nextTrack()); });
    audio.addEventListener("error", function () { if (activated) handleTrackError(); });
    audio.addEventListener("pause", function () { if (activated && !audio.ended) setState("paused"); });
    audio.addEventListener("play", function () { setState("playing"); });

    function onScroll() {
        if (window.scrollY > SCROLL_REVEAL) {
            reveal();
            window.removeEventListener("scroll", onScroll);
        }
    }

    window.addEventListener("scroll", onScroll, { passive: true });
    if (window.scrollY > SCROLL_REVEAL) reveal();

    setState("idle");
})();
