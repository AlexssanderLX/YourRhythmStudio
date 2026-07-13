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
    var current = -1;
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
            })
            .catch(function () {
                tracks = [];
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

    function pickRandom() {
        if (tracks.length === 0) return -1;
        if (tracks.length === 1) return 0;

        var index;
        do {
            index = Math.floor(Math.random() * tracks.length);
        } while (index === current);
        return index;
    }

    function playIndex(index) {
        if (index < 0 || !tracks[index]) return;
        current = index;
        audio.src = tracks[index].url;
        audio.volume = INITIAL_VOLUME;

        var promise = audio.play();
        if (promise && typeof promise.then === "function") {
            promise.then(function () { setState("playing"); }).catch(handleTrackError);
        } else {
            setState("playing");
        }
    }

    function handleTrackError() {
        if (current >= 0 && current < tracks.length) {
            tracks.splice(current, 1);
            current = -1;
        }

        if (tracks.length > 0) {
            playIndex(pickRandom());
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
            playIndex(pickRandom());
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

    audio.addEventListener("ended", function () { playIndex(pickRandom()); });
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
