(() => {
    const sidebar = document.getElementById("sidebar");
    const toggle = document.getElementById("sidebar-toggle");
    const close = document.getElementById("sidebar-close");
    if (!sidebar || !toggle || !close) return;

    const setOpen = (open) => {
        sidebar.classList.toggle("is-open", open);
        document.body.classList.toggle("sidebar-open", open);
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
    };

    toggle.addEventListener("click", () => setOpen(true));
    close.addEventListener("click", () => setOpen(false));
    sidebar.querySelectorAll("a").forEach(link => link.addEventListener("click", () => setOpen(false)));
    window.addEventListener("resize", () => {
        if (window.innerWidth >= 768) setOpen(false);
    });
})();
