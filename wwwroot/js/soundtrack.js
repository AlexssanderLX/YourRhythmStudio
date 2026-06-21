/* =========================================================
   YourRhythm Studio — floating landing soundtrack player
   - No autoplay. Appears only after the user scrolls.
   - First click picks a random track; then toggles play/pause.
   - Auto-advances to another random track when one ends.
   - Starts at 20% volume.
   - Degrades gracefully: if no audio files exist, stays hidden
     and logs nothing worse than a console.info (no errors).
   ========================================================= */
(function () {
    "use strict";

    // Faixas da trilha. Adicione/remova caminhos conforme os arquivos em
    // wwwroot/audio/landing-soundtrack/  (veja o README dessa pasta).
    var PLAYLIST = [
        "/audio/landing-soundtrack/track-1.mp3",
        "/audio/landing-soundtrack/track-2.mp3",
        "/audio/landing-soundtrack/track-3.mp3"
        // "/audio/landing-soundtrack/track-4.mp3",
    ];

    var INITIAL_VOLUME = 0.2;   // 20%
    var SCROLL_REVEAL = 500;    // px de rolagem para revelar o player

    var root = document.getElementById("soundtrack");
    var btn = document.getElementById("soundtrackBtn");
    var label = document.getElementById("stLabel");
    var audio = document.getElementById("soundtrackAudio");
    if (!root || !btn || !label || !audio) return;

    var available = [];      // faixas que realmente existem
    var current = -1;        // índice da faixa atual em `available`
    var revealed = false;    // já fez o probe / tentou revelar?
    var activated = false;   // usuário já clicou em "Ativar"?

    function setState(state) {
        root.classList.toggle("playing", state === "playing");
        if (state === "playing") {
            label.textContent = "⏸ Pausar música";
            btn.setAttribute("aria-pressed", "true");
        } else if (state === "paused") {
            label.textContent = "▶ Continuar música";
            btn.setAttribute("aria-pressed", "false");
        } else { // idle
            label.textContent = "🎧 Ativar trilha sonora";
            btn.setAttribute("aria-pressed", "false");
        }
    }

    // Verifica se um arquivo existe (HEAD). Retorna o caminho ou null.
    function probe(url) {
        return fetch(url, { method: "HEAD" })
            .then(function (r) { return r.ok ? url : null; })
            .catch(function () { return null; });
    }

    function reveal() {
        if (revealed) return;
        revealed = true;
        Promise.all(PLAYLIST.map(probe)).then(function (results) {
            available = results.filter(Boolean);
            if (available.length === 0) {
                // Nenhuma faixa disponível: manter oculto, sem erro.
                console.info("[soundtrack] Nenhuma faixa encontrada em /audio/landing-soundtrack/. Player oculto.");
                return;
            }
            root.hidden = false;
            // força reflow antes de animar a entrada
            void root.offsetWidth;
            root.classList.add("show");
        });
    }

    function pickRandom() {
        if (available.length === 0) return -1;
        if (available.length === 1) return 0;
        var idx;
        do { idx = Math.floor(Math.random() * available.length); } while (idx === current);
        return idx;
    }

    function playIndex(idx) {
        if (idx < 0) return;
        current = idx;
        audio.src = available[idx];
        audio.volume = INITIAL_VOLUME;
        var p = audio.play();
        if (p && typeof p.then === "function") {
            p.then(function () { setState("playing"); }).catch(function () { handleTrackError(); });
        } else {
            setState("playing");
        }
    }

    // Falha ao tocar/abrir uma faixa: descarta e tenta a próxima.
    function handleTrackError() {
        if (current >= 0 && current < available.length) {
            available.splice(current, 1);
            current = -1;
        }
        if (available.length > 0) {
            playIndex(pickRandom());
        } else {
            btn.disabled = true;
            root.classList.remove("playing");
            label.textContent = "🎧 Trilha indisponível";
        }
    }

    btn.addEventListener("click", function () {
        if (available.length === 0) return;
        if (!activated) {
            activated = true;
            playIndex(pickRandom());
            return;
        }
        if (audio.paused) {
            var p = audio.play();
            if (p && typeof p.then === "function") {
                p.then(function () { setState("playing"); }).catch(handleTrackError);
            } else {
                setState("playing");
            }
        } else {
            audio.pause();
            setState("paused");
        }
    });

    // Próxima faixa aleatória ao terminar.
    audio.addEventListener("ended", function () { playIndex(pickRandom()); });
    // Erro de carregamento da faixa (já ativado).
    audio.addEventListener("error", function () { if (activated) handleTrackError(); });
    // Mantém o rótulo coerente se play/pause vier de teclas de mídia.
    audio.addEventListener("pause", function () { if (activated && !audio.ended) setState("paused"); });
    audio.addEventListener("play", function () { setState("playing"); });

    // Revela o player após o usuário rolar a página.
    function onScroll() {
        if (window.scrollY > SCROLL_REVEAL) {
            reveal();
            window.removeEventListener("scroll", onScroll);
        }
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    if (window.scrollY > SCROLL_REVEAL) reveal(); // caso a página já abra rolada

    setState("idle");
})();
