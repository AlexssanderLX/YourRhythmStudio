/* YourRhythm Studio — landing interactions
   Capability tiers:
   - reduce  : prefers-reduced-motion → skip decorative animation
   - lowCap  : slow connection / low memory / small viewport → skip heavy effects
   - default : full experience (AOS + GSAP nav entrance) */
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

    // Without GSAP or with reduced motion: static state is fine, exit early.
    if (!hasGSAP || reduce) return;

    gsap.registerPlugin(ScrollTrigger);

    // Nav entrance — only runs once on page load
    if (!lowCap) {
        gsap.from("#nav .nav-inner > *", {
            opacity: 0, y: -10, duration: 0.6, stagger: 0.07,
            ease: "power2.out", delay: 0.05
        });
    }
})();
