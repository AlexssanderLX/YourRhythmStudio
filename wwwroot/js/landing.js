/* YourRhythm Studio — landing interactions
   Capability tiers:
   - reduced  : prefers-reduced-motion → skip all decorative GSAP animation
   - standard : slow connection / very low memory → no heavy bg loops, but
                CSS bg waves and keyboard are still visible
   - enhanced : full experience — AOS + GSAP nav entrance + ambient piano bg

   Viewport width is NOT used as a proxy for device capability.
   A modern phone should receive enhanced or standard based on real signals. */
(function () {
    "use strict";

    var hasGSAP = typeof window.gsap !== "undefined";

    /** Returns "reduced" | "standard" | "enhanced" */
    var TIER = (function () {
        if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
            return "reduced";
        }
        try {
            var conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
            if (conn) {
                if (conn.saveData) return "standard";
                if (conn.effectiveType === "slow-2g" || conn.effectiveType === "2g") return "standard";
            }
            if (navigator.deviceMemory !== undefined && navigator.deviceMemory < 1) return "standard";
            if (navigator.hardwareConcurrency !== undefined && navigator.hardwareConcurrency < 2) return "standard";
        } catch (e) {}
        return "enhanced";
    })();

    // --- Nav: subtle border on scroll ---
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
        mobileNav.addEventListener("click", function (e) {
            if (e.target.closest("a[href]")) closeMenu();
        });
    })();

    // Without GSAP or with reduced motion: static layout is fine.
    if (!hasGSAP || TIER === "reduced") return;

    gsap.registerPlugin(ScrollTrigger);

    // Nav entrance (all non-reduced tiers)
    gsap.from("#nav .nav-inner > *", {
        opacity: 0, y: -10, duration: 0.6, stagger: 0.07,
        ease: "power2.out", delay: 0.05
    });

    // Ambient background piano — enhanced tier only
    if (TIER !== "enhanced") return;

    var bgTimeline = null;

    (function () {
        var kb = document.getElementById("bgKeyboard");
        if (!kb) return;

        // Background particles (spawned into DOM)
        var bgp = document.getElementById("bgParticles");
        if (bgp) {
            for (var i = 0; i < 8; i++) {
                var d = document.createElement("span");
                d.className = "bgp";
                var sz = (2 + Math.random() * 3).toFixed(1);
                d.style.left   = (Math.random() * 100).toFixed(2) + "%";
                d.style.top    = (40 + Math.random() * 60).toFixed(2) + "%";
                d.style.width  = sz + "px";
                d.style.height = sz + "px";
                d.style.opacity = (0.15 + Math.random() * 0.4).toFixed(2);
                bgp.appendChild(d);
                gsap.to(d, {
                    y: -(40 + Math.random() * 70), x: -14 + Math.random() * 28,
                    opacity: 0, duration: 6 + Math.random() * 5,
                    ease: "sine.inOut", repeat: -1, yoyo: true, delay: Math.random() * 5
                });
            }
        }

        var host   = document.getElementById("risingNotes");
        var glyphs = ["♪", "♫", "♩", "♬"];

        function spawnNote(k) {
            if (!host) return;
            var el = document.createElement("span");
            el.className = "rnote";
            el.textContent = glyphs[(Math.random() * glyphs.length) | 0];
            var hr = host.getBoundingClientRect();
            var kr = k.getBoundingClientRect();
            var x = kr.width > 2 ? (kr.left - hr.left + kr.width / 2) : Math.random() * hr.width;
            el.style.left     = x + "px";
            el.style.top      = (hr.height * 0.6) + "px";
            el.style.fontSize = (15 + Math.random() * 11) + "px";
            host.appendChild(el);
            gsap.fromTo(el,
                { opacity: 0, y: 0, scale: 0.8 },
                {
                    opacity: 0.7,
                    y: -(110 + Math.random() * 80),
                    x: "+=" + (-22 + Math.random() * 44),
                    scale: 1,
                    rotation: -12 + Math.random() * 24,
                    duration: 2.6 + Math.random() * 1.1,
                    ease: "power1.out",
                    onComplete: function () { el.remove(); }
                });
            gsap.to(el, { opacity: 0, duration: 0.9, delay: 1.7, ease: "power1.in" });
        }

        function pulseWaves() {
            gsap.fromTo(".hero-bg-waves", { opacity: 0.5 },
                { opacity: 0.85, duration: 0.16, yoyo: true, repeat: 1, ease: "power2.out" });
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
            tl.to(light, {
                opacity: 0.95, duration: 0.16, ease: "power2.out",
                onStart: function () { spawnNote(key); pulseWaves(); }
            }, at)
              .to(light, { opacity: 0, duration: 0.52, ease: "power2.in" }, at + 0.16);
        });
    })();

    // Pause the background melody when the tab is not visible
    if (typeof document.hidden !== "undefined") {
        document.addEventListener("visibilitychange", function () {
            if (document.hidden) {
                if (bgTimeline) bgTimeline.pause();
            } else {
                if (bgTimeline) bgTimeline.resume();
            }
        });
    }
})();
