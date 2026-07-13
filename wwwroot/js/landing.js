/* YourRhythm Studio — landing motion
   Subtle, premium reveals. Heavy lifting via GSAP + ScrollTrigger.
   Everything degrades gracefully without JS and honors reduced-motion.

   Capability tiers (simple, no benchmark):
   - reduce  : prefers-reduced-motion → skip all decorative animation
   - lowCap  : slow connection / low memory / small viewport → skip heavy bg
   - default : full experience */
(function () {
    "use strict";

    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var hasGSAP = typeof window.gsap !== "undefined";

    var lowCap = (function () {
        try {
            var conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
            if (conn) {
                if (conn.saveData) return true;
                if (conn.effectiveType === "slow-2g" || conn.effectiveType === "2g") return true;
            }
            if (navigator.deviceMemory && navigator.deviceMemory < 2) return true;
            if (navigator.hardwareConcurrency && navigator.hardwareConcurrency < 4) return true;
        } catch (e) {}
        return window.innerWidth < 640;
    })();

    // --- Nav: subtle border once you scroll ---
    var nav = document.getElementById("nav");
    function onScroll() {
        if (nav) nav.classList.toggle("scrolled", window.scrollY > 12);
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    // --- Mobile nav drawer ---
    (function () {
        var hamburger = document.getElementById("navHamburger");
        var mobileNav  = document.getElementById("navMobile");
        var backdrop   = document.getElementById("navMobileBackdrop");
        var closeBtn   = document.getElementById("navMobileClose");
        if (!hamburger || !mobileNav) return;

        var isOpen = false;

        function focusable(container) {
            return Array.from(container.querySelectorAll(
                'a[href]:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
            )).filter(function (el) {
                return !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);
            });
        }

        function openMenu() {
            if (isOpen) return;
            isOpen = true;
            mobileNav.classList.add("open");
            mobileNav.removeAttribute("aria-hidden");
            if (backdrop) backdrop.classList.add("open");
            hamburger.setAttribute("aria-expanded", "true");
            hamburger.setAttribute("aria-label", "Fechar menu");
            document.body.classList.add("nav-open");
            // Focus first item (close button)
            var first = focusable(mobileNav)[0];
            if (first) setTimeout(function () { first.focus(); }, 30);
            document.addEventListener("keydown", onKey);
        }

        function closeMenu() {
            if (!isOpen) return;
            isOpen = false;
            mobileNav.classList.remove("open");
            mobileNav.setAttribute("aria-hidden", "true");
            if (backdrop) backdrop.classList.remove("open");
            hamburger.setAttribute("aria-expanded", "false");
            hamburger.setAttribute("aria-label", "Abrir menu");
            document.body.classList.remove("nav-open");
            hamburger.focus();
            document.removeEventListener("keydown", onKey);
        }

        function onKey(e) {
            if (e.key === "Escape") { closeMenu(); return; }
            if (e.key !== "Tab") return;
            var items = focusable(mobileNav);
            if (!items.length) return;
            var first = items[0], last = items[items.length - 1];
            if (e.shiftKey) {
                if (document.activeElement === first) { e.preventDefault(); last.focus(); }
            } else {
                if (document.activeElement === last) { e.preventDefault(); first.focus(); }
            }
        }

        hamburger.addEventListener("click", function () {
            if (isOpen) closeMenu(); else openMenu();
        });
        if (closeBtn) closeBtn.addEventListener("click", closeMenu);
        if (backdrop) backdrop.addEventListener("click", closeMenu);

        // Close on any anchor link click inside the menu (navigates or scrolls)
        mobileNav.addEventListener("click", function (e) {
            if (e.target.closest("a[href]")) closeMenu();
        });
    })();

    // --- Playable mini digital piano ---
    (function () {
        var ctx = null, master = null;

        function audioCtx() {
            if (ctx) return ctx;
            var AC = window.AudioContext || window.webkitAudioContext;
            if (!AC) return null;
            ctx = new AC();
            master = ctx.createGain();
            master.gain.value = 0.6;
            master.connect(ctx.destination);
            return ctx;
        }

        var SEMI = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };
        function noteToFreq(name) {
            var m = /^([A-G])(#?)(\d)$/.exec(name || "");
            if (!m) return null;
            var midi = (parseInt(m[3], 10) + 1) * 12 + SEMI[m[1]] + (m[2] === "#" ? 1 : 0);
            return 440 * Math.pow(2, (midi - 69) / 12);
        }

        function playFreq(f) {
            var c = audioCtx();
            if (!c) return;
            if (c.state === "suspended") { c.resume(); }
            var t = c.currentTime;
            var g = c.createGain();
            g.gain.setValueAtTime(0.0001, t);
            g.gain.linearRampToValueAtTime(0.6, t + 0.008);
            g.gain.exponentialRampToValueAtTime(0.0008, t + 1.5);
            var o1 = c.createOscillator(); o1.type = "triangle"; o1.frequency.value = f;
            var o2 = c.createOscillator(); o2.type = "sine"; o2.frequency.value = f * 2;
            var g2 = c.createGain(); g2.gain.value = 0.3; o2.connect(g2);
            var lp = c.createBiquadFilter(); lp.type = "lowpass"; lp.frequency.value = 2600;
            o1.connect(g); g2.connect(g); g.connect(lp); lp.connect(master);
            o1.start(t); o2.start(t); o1.stop(t + 1.6); o2.stop(t + 1.6);
        }

        function strike(keyEl) {
            var freq = noteToFreq(keyEl.getAttribute("data-note"));
            if (freq === null) return;
            playFreq(freq);
            keyEl.classList.add("press");
            setTimeout(function () { keyEl.classList.remove("press"); }, 150);
        }

        var kb = document.getElementById("keyboard");
        if (kb) {
            kb.classList.add("playable");
            kb.addEventListener("pointerdown", function (e) {
                var key = e.target.closest(".bkey") || e.target.closest(".wkey");
                if (!key || !kb.contains(key)) return;
                e.preventDefault();
                strike(key);
            });
        }
    })();

    // Helper: fill a [data-fill] progress bar to its percent width
    function fillBar(el) {
        var pct = el.getAttribute("data-fill") || "0";
        el.style.width = pct + "%";
    }
    function colTrackPx() {
        var c = document.getElementById("chart");
        return c ? Math.max(0, c.clientHeight - 26) : 130;
    }
    function colPx(el) {
        var h = parseFloat(el.getAttribute("data-h") || "0");
        return Math.round((h / 100) * colTrackPx());
    }
    function growCol(el) {
        el.style.height = colPx(el) + "px";
    }

    // No GSAP or reduced motion: show everything, fill bars, done.
    if (!hasGSAP || reduce) {
        document.querySelectorAll(".reveal").forEach(function (el) {
            el.style.opacity = 1; el.style.transform = "none";
        });
        document.querySelectorAll(".bar > i").forEach(fillBar);
        document.querySelectorAll(".chart .col i").forEach(growCol);
        var ring0 = document.getElementById("ring");
        if (ring0) ring0.style.strokeDashoffset = (239 * (1 - 0.78)).toString();
        return;
    }

    gsap.registerPlugin(ScrollTrigger);

    // NOTE: scroll reveals are handled by AOS (see _LandingLayout.cshtml).
    // GSAP here only drives continuous / data-driven motion: nav entrance,
    // particles, MIDI playhead, progress bars/chart that fill to a value.

    // --- Hero entrance: gentle stagger ---
    gsap.from("#nav .nav-inner > *", { opacity: 0, y: -12, duration: 0.7, stagger: 0.08, ease: "power2.out", delay: 0.1 });

    // --- Hero piano stage ---
    var stage = document.getElementById("pianoStage");
    if (stage) {
        // Fewer particles on limited devices
        var particleCount = lowCap ? 5 : 14;
        var pc = document.getElementById("particles");
        if (pc) {
            for (var pi = 0; pi < particleCount; pi++) {
                var p = document.createElement("span");
                p.className = "particle";
                var s = (2 + Math.random() * 3).toFixed(1);
                p.style.left = (Math.random() * 100).toFixed(2) + "%";
                p.style.top = (Math.random() * 100).toFixed(2) + "%";
                p.style.width = s + "px";
                p.style.height = s + "px";
                p.style.opacity = (0.2 + Math.random() * 0.5).toFixed(2);
                pc.appendChild(p);
                gsap.to(p, {
                    y: -(20 + Math.random() * 45), x: -10 + Math.random() * 20, opacity: 0,
                    duration: 5 + Math.random() * 5, ease: "sine.inOut",
                    repeat: -1, yoyo: true, delay: Math.random() * 4
                });
            }
        }

        // MIDI playhead sweeps and lights up notes as it passes
        var ph = document.getElementById("playhead");
        var mnotes = [].slice.call(document.querySelectorAll("#midiRoll .mnote"));
        if (ph) {
            var head = { x: 0 };
            gsap.to(head, {
                x: 100, duration: 6.5, ease: "none", repeat: -1,
                onUpdate: function () {
                    ph.style.left = head.x + "%";
                    for (var k = 0; k < mnotes.length; k++) {
                        var nl = parseFloat(mnotes[k].style.left);
                        var nw = parseFloat(mnotes[k].style.width);
                        if (head.x >= nl && head.x <= nl + nw + 2) mnotes[k].classList.add("lit");
                        else mnotes[k].classList.remove("lit");
                    }
                }
            });
        }

        // Pointer parallax — desktop only (costly on touch/low-cap devices)
        if (!lowCap && window.matchMedia("(pointer: fine)").matches) {
            var device = document.getElementById("pianoDevice");
            var glow = stage.querySelector(".stage-glow");
            stage.addEventListener("pointermove", function (e) {
                var r = stage.getBoundingClientRect();
                var dx = (e.clientX - r.left) / r.width - 0.5;
                var dy = (e.clientY - r.top) / r.height - 0.5;
                if (device) gsap.to(device, { x: dx * 10, y: dy * 8, rotateY: dx * 3, rotateX: -dy * 2, duration: 0.6, ease: "power2.out", overwrite: "auto" });
                if (glow) gsap.to(glow, { x: dx * -26, y: dy * -18, duration: 0.9, ease: "power2.out", overwrite: "auto" });
            });
            stage.addEventListener("pointerleave", function () {
                if (device) gsap.to(device, { x: 0, y: 0, rotateY: 0, rotateX: 0, duration: 0.9, ease: "power2.out" });
                if (glow) gsap.to(glow, { x: 0, y: 0, duration: 1.1, ease: "power2.out" });
            });
        }
    }

    // --- Hero animated BACKGROUND — skipped entirely on low-cap devices ---
    var bgTimeline = null;
    if (!lowCap) {
        (function () {
            var kb = document.getElementById("bgKeyboard");
            if (!kb) return;

            var bgp = document.getElementById("bgParticles");
            if (bgp) {
                for (var i = 0; i < 8; i++) {
                    var d = document.createElement("span");
                    d.className = "bgp";
                    var s = (2 + Math.random() * 3).toFixed(1);
                    d.style.left = (Math.random() * 100).toFixed(2) + "%";
                    d.style.top = (40 + Math.random() * 60).toFixed(2) + "%";
                    d.style.width = s + "px"; d.style.height = s + "px";
                    d.style.opacity = (0.15 + Math.random() * 0.4).toFixed(2);
                    bgp.appendChild(d);
                    gsap.to(d, {
                        y: -(40 + Math.random() * 70), x: -14 + Math.random() * 28, opacity: 0,
                        duration: 6 + Math.random() * 5, ease: "sine.inOut", repeat: -1, yoyo: true, delay: Math.random() * 5
                    });
                }
            }

            var host = document.getElementById("risingNotes");
            var glyphs = ["♪", "♫", "♩", "♬"];

            function spawnNote(k) {
                if (!host) return;
                var el = document.createElement("span");
                el.className = "rnote";
                el.textContent = glyphs[(Math.random() * glyphs.length) | 0];
                var hr = host.getBoundingClientRect();
                var kr = k.getBoundingClientRect();
                var x = kr.width > 2 ? (kr.left - hr.left + kr.width / 2) : Math.random() * hr.width;
                el.style.left = x + "px";
                el.style.top = (hr.height * 0.6) + "px";
                el.style.fontSize = (15 + Math.random() * 11) + "px";
                host.appendChild(el);
                gsap.fromTo(el,
                    { opacity: 0, y: 0, scale: 0.8 },
                    {
                        opacity: 0.7, y: -(110 + Math.random() * 80), x: "+=" + (-22 + Math.random() * 44),
                        scale: 1, rotation: -12 + Math.random() * 24, duration: 2.6 + Math.random() * 1.1,
                        ease: "power1.out", onComplete: function () { el.remove(); }
                    });
                gsap.to(el, { opacity: 0, duration: 0.9, delay: 1.7, ease: "power1.in" });
            }

            function pulseWaves() {
                gsap.fromTo(".hero-bg-waves", { opacity: 0.5 }, { opacity: 0.85, duration: 0.16, yoyo: true, repeat: 1, ease: "power2.out" });
            }

            var melody = ["C", "E", "G", "B", "A", "G", "E", "D"];
            var oct = 2;
            var tl = gsap.timeline({ repeat: -1, repeatDelay: 0.7 });
            bgTimeline = tl;
            melody.forEach(function (note, idx) {
                var key = kb.querySelector('.bgk-wkey[data-note="' + note + '"][data-oct="' + oct + '"]');
                if (!key) return;
                var light = key.querySelector(".klight");
                var at = idx * 0.46;
                tl.to(light, { opacity: 0.95, duration: 0.16, ease: "power2.out", onStart: function () { spawnNote(key); pulseWaves(); } }, at)
                  .to(light, { opacity: 0, duration: 0.52, ease: "power2.in" }, at + 0.16);
            });
        })();
    }

    // --- Pause heavy animations when tab is hidden ---
    if (typeof document.hidden !== "undefined") {
        document.addEventListener("visibilitychange", function () {
            if (document.hidden) {
                if (bgTimeline) bgTimeline.pause();
            } else {
                if (bgTimeline) bgTimeline.resume();
            }
        });
    }

    // --- Progress bars fill when scrolled into view ---
    gsap.utils.toArray(".bar > i").forEach(function (el) {
        var pct = parseFloat(el.getAttribute("data-fill") || "0");
        ScrollTrigger.create({
            trigger: el, start: "top 92%", once: true,
            onEnter: function () { gsap.to(el, { width: pct + "%", duration: 1.1, ease: "power2.out" }); }
        });
    });

    // --- Evolution chart columns grow ---
    gsap.utils.toArray(".chart .col i").forEach(function (el, i) {
        ScrollTrigger.create({
            trigger: "#chart", start: "top 85%", once: true,
            onEnter: function () { gsap.to(el, { height: colPx(el) + "px", duration: 0.9, delay: i * 0.06, ease: "power3.out" }); }
        });
    });

    // --- Progress ring sweeps to 78% ---
    var ring = document.getElementById("ring");
    if (ring) {
        var circ = 239, target = circ * (1 - 0.78);
        ScrollTrigger.create({
            trigger: ring, start: "top 92%", once: true,
            onEnter: function () { gsap.to(ring, { strokeDashoffset: target, duration: 1.4, ease: "power2.out" }); }
        });
    }
})();
